using FirstGearGames.Utilities.Networks;
using FirstGearGames.Utilities.Editors;
using FirstGearGames.Utilities.Maths;
using FirstGearGames.Utilities.Objects;
using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
#if MIRRORNG || MirrorNg
using NetworkConnection = Mirror.INetworkConnection;
#endif

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms
{


    public abstract class FlexNetworkTransformBase : NetworkBehaviour
    {
        #region Types.
        /// <summary>
        /// Space types to use when getting or setting attached data.
        /// </summary>
        private enum AttachedSpaces
        {
            Disabled = 0,
            Local = 1,
            World = 2
        }
        /// <summary>
        /// Attached object data.
        /// </summary>
        public class AttachedSyncData
        {
            /// <summary>
            /// Attached object's Networkidentity.
            /// </summary>
            public NetworkIdentity Identity = null;
            /// <summary>
            /// ComponentIndex to attach to. Will be null if using NetworkIdentity object.
            /// </summary>
            public sbyte ComponentIndex = -1;
            /// <summary>
            /// For spectators this is the transform to move towards. For owner this is where their transform is in localspace to the attached object.
            /// </summary>
            public Transform Target = null;
        }
        /// <summary>
        /// Extrapolation for the most recent received data.
        /// </summary>
        protected class ExtrapolationData
        {
            public float Remaining;
            public Vector3 Position;

            public ExtrapolationData(Vector3 position, float remaining)
            {
                Remaining = remaining;
                Position = position;
            }

            /// <summary>
            /// Adds onto remaining.
            /// </summary>
            /// <param name="value"></param>
            public void AddRemaining(float value)
            {
                Remaining += value;
            }
        }
        /// <summary>
        /// Move rates for the most recent received data.
        /// </summary>
        protected struct MoveRateData
        {
            public float Position;
            public float Rotation;
            public float Scale;
        }

        /// <summary>
        /// Data used to manage moving towards a target.
        /// </summary>
        protected class TargetSyncData
        {
            public void UpdateValues(TransformSyncData goalData, MoveRateData moveRates, ExtrapolationData extrapolationData)
            {
                GoalData = goalData;
                MoveRates = moveRates;
                Extrapolation = extrapolationData;
            }

            /// <summary>
            /// Transform goal data for this update.
            /// </summary>
            public TransformSyncData GoalData;
            /// <summary>
            /// How quickly to move towards each transform property.
            /// </summary>
            public MoveRateData MoveRates;
            /// <summary>
            /// How much extrapolation time remains.
            /// </summary>
            public ExtrapolationData Extrapolation = null;
        }
        /// <summary>
        /// Ways to synchronize datas.
        /// </summary>
        [System.Serializable]
        private enum SynchronizeTypes : int
        {
            Normal = 0,
            NoSynchronization = 1
        }
        /// <summary>
        /// Interval types to determine when to synchronize data.
        /// </summary>
        [System.Serializable]
        private enum IntervalTypes : int
        {
            Timed = 0,
            FixedUpdate = 1
        }
        #endregion

        #region Public.
        /// <summary>
        /// Dispatched when server receives data from a client while using client authoritative.
        /// </summary>
        public event Action<ReceivedClientData> OnClientDataReceived;
        /// <summary>
        /// Transform to monitor and modify.
        /// </summary>
        public abstract Transform TargetTransform { get; }
        /// <summary>
        /// AttachedData for what this object is attached to.
        /// </summary>
        public AttachedSyncData Attached { get; private set; } = new AttachedSyncData();
        /// <summary>
        /// Sets which object this transform is attached to.
        /// </summary>
        /// <param name="attached"></param>
        protected void SetAttachedInternal(NetworkIdentity attached, sbyte componentIndex)
        {
            uint netId = (attached == null) ? 0 : attached.ReturnNetworkId();
            UpdateAttached(netId, componentIndex);
        }
        /// <summary>
        /// LastSequenceId received from the client for this FlexNetworkTransformBase.
        /// </summary>
        public ushort LastClientSequenceId { get; private set; }
        /// <summary>
        /// Sets the LastSequenceId value.
        /// </summary>
        /// <param name="value"></param>
        public void SetLastClientSequenceIdInternal(ushort value)
        {
            LastClientSequenceId = value;
        }
        #endregion

        #region Serialized.
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("True to synchronize using LocalSpace values. False to use WorldSpace.")]
        [SerializeField]
        private bool _useLocalSpace = true;
        /// <summary>
        /// True to synchronize using LocalSpace values. False to use WorldSpace.
        /// </summary>
        protected bool UseLocalSpace { get { return _useLocalSpace; } }
        /// <summary>
        /// How to operate synchronization timings. Timed will synchronized every specified interval while FixedUpdate will synchronize every FixedUpdate.
        /// </summary>
        [Tooltip("How to operate synchronization timings. Timed will synchronized every specified interval while FixedUpdate will synchronize every FixedUpdate.")]
        [SerializeField]
        private IntervalTypes _intervalType = IntervalTypes.Timed;
        /// <summary>
        /// How often to synchronize this transform.
        /// </summary>
        [Tooltip("How often to synchronize this transform.")]
        [Range(0.00f, 0.5f)]
        [SerializeField]
        private float _synchronizeInterval = 0.1f;
        /// <summary>
        /// True to synchronize using the reliable channel. False to synchronize using the unreliable channel. Your project must use 0 as reliable, and 1 as unreliable for this to function properly. This feature is not supported on TCP transports.
        /// </summary>
        [Tooltip("True to synchronize using the reliable channel. False to synchronize using the unreliable channel. Your project must use 0 as reliable, and 1 as unreliable for this to function properly.")]
        [SerializeField]
        private bool _reliable = true;
        /// <summary>
        /// True to resend transform data with every unreliable packet. At the cost of bandwidth this will ensure smoother movement on very unstable connections but generally is not needed.
        /// </summary>
        [Tooltip("True to resend transform data with every unreliable packet. At the cost of bandwidth this will ensure smoother movement on very unstable connections but generally is not needed.")]
        [SerializeField]
        private bool _resendUnreliable = false;
        /// <summary>
        /// How far in the past objects should be for interpolation. Higher values will result in smoother movement with network fluctuations but lower values will result in objects being closer to their actual position. Lower values can generally be used for longer synchronization intervalls.
        /// </summary>
        [Tooltip("How far in the past objects should be for interpolation. Higher values will result in smoother movement with network fluctuations but lower values will result in objects being closer to their actual position. Lower values can generally be used for longer synchronization intervals.")]
        [Range(0.00f, 0.5f)]
        [SerializeField]
        private float _interpolationFallbehind = 0.06f;
        /// <summary>
        /// How long to extrapolate when data is expected but does not arrive. Smaller values are best for fast synchronization intervals. For precision or fast reaction games you may want to use no extrapolation or only one or two synchronization intervals worth. Extrapolation is client-side only.
        /// </summary>
        [Tooltip("How long to extrapolate when data is expected but does not arrive. Smaller values are best for fast synchronization intervals. For precision or fast reaction games you may want to use no extrapolation or only one or two synchronization intervals worth. Extrapolation is client-side only.")]
        [Range(0f, 5f)]
        [SerializeField]
        private float _extrapolationSpan = 0f;
        /// <summary>
        /// Teleport the transform if the distance between received data exceeds this value. Use 0f to disable.
        /// </summary>
        [Tooltip("Teleport the transform if the distance between received data exceeds this value. Use 0f to disable.")]
        [SerializeField]
        private float _teleportThreshold = 0f;
        /// <summary>
        /// True if using client authoritative movement.
        /// </summary>
        [Tooltip("True if using client authoritative movement.")]
        [SerializeField]
        private bool _clientAuthoritative = true;
        /// <summary>
        /// True to synchronize server results back to owner. Typically used when you are sending inputs to the server and are relying on the server response to move the transform.
        /// </summary>
        [Tooltip("True to synchronize server results back to owner. Typically used when you are sending inputs to the server and are relying on the server response to move the transform.")]
        [SerializeField]
        private bool _synchronizeToOwner = true;
        /// <summary>
        /// True to compress small values on position and scale. Values will be rounded to the hundredth decimal place, eg: 102.12f.
        /// </summary>
        [Tooltip("True to compress small values on position and scale. Values will be rounded to the hundredth decimal place, eg: 102.12f.")]
        [SerializeField]
        private bool _compressSmall = true;
        /// <summary>
        /// Synchronize options for position.
        /// </summary>
        [Tooltip("Synchronize options for position.")]
        [SerializeField]
        private SynchronizeTypes _synchronizePosition = SynchronizeTypes.Normal;
        /// <summary>
        /// Euler axes on the position to snap into place rather than move towards over time.
        /// </summary>
        [Tooltip("Euler axes on the rotation to snap into place rather than move towards over time.")]
        [SerializeField]
        [BitMask(typeof(SnappingAxes))]
        private SnappingAxes _snapPosition = (SnappingAxes)0;
        /// <summary>
        /// Sets SnapPosition value. For internal use only. Must be public for editor script.
        /// </summary>
        /// <param name="value"></param>
        public void SetSnapPosition(SnappingAxes value) { _snapPosition = value; }
        /// <summary>
        /// Synchronize states for rotation.
        /// </summary>
        [Tooltip("Synchronize states for position.")]
        [SerializeField]
        private SynchronizeTypes _synchronizeRotation = SynchronizeTypes.Normal;
        /// <summary>
        /// Euler axes on the rotation to snap into place rather than move towards over time.
        /// </summary>
        [Tooltip("Euler axes on the rotation to snap into place rather than move towards over time.")]
        [SerializeField]
        [BitMask(typeof(SnappingAxes))]
        private SnappingAxes _snapRotation = (SnappingAxes)0;
        /// <summary>
        /// Sets SnapRotation value. For internal use only. Must be public for editor script.
        /// </summary>
        /// <param name="value"></param>
        public void SetSnapRotation(SnappingAxes value) { _snapRotation = value; }
        /// <summary>
        /// Synchronize states for scale.
        /// </summary>
        [Tooltip("Synchronize states for scale.")]
        [SerializeField]
        private SynchronizeTypes _synchronizeScale = SynchronizeTypes.Normal;
        /// <summary>
        /// Euler axes on the scale to snap into place rather than move towards over time.
        /// </summary>
        [Tooltip("Euler axes on the scale to snap into place rather than move towards over time.")]
        [SerializeField]
        [BitMask(typeof(SnappingAxes))]
        private SnappingAxes _snapScale = (SnappingAxes)0;
        /// <summary>
        /// Sets SnapScale value. For internal use only. Must be public for editor script.
        /// </summary>
        /// <param name="value"></param>
        public void SetSnapScale(SnappingAxes value) { _snapScale = value; }
        #endregion

        #region Private.
        /// <summary>
        /// Last SyncData sent by client.
        /// </summary>
        private TransformSyncData _clientSyncData;
        /// <summary>
        /// Last SyncData sent by server.
        /// </summary>
        private TransformSyncData _serverSyncData;
        /// <summary>
        /// TargetSyncData to move between.
        /// </summary>
        private TargetSyncData _targetData = null;
        /// <summary>
        /// Next time client may send data.
        /// </summary>
        private float _nextClientSendTime = 0f;
        /// <summary>
        /// Next time server may send data.
        /// </summary>
        private float _nextServerSendTime = 0f;
        /// <summary>
        /// When sending data from client, after the transform stops changing and when using unreliable this becomes true while a reliable packet is being sent.
        /// </summary>
        private bool _clientSettleSent = false;
        /// <summary>
        /// When sending data from server, after the transform stops changing and when using unreliable this becomes true while a reliable packet is being sent.
        /// </summary>
        private bool _serverSettleSent = false;
        /// <summary>
        /// TeleportThreshold value squared.
        /// </summary>
        private float _teleportThresholdSquared;
        /// <summary>
        /// Time in which the transform was detected as idle.
        /// </summary>
        private float _transformIdleStart = -1f;
        /// <summary>
        /// NetworkVisibility component on the root of this object.
        /// </summary>
        private NetworkVisibility _networkVisibility = null;
        /// <summary>
        /// FlexNetworkTransformManager reference.
        /// </summary>
        private FlexNetworkTransformManager _manager;
        /// <summary>
        /// Sets the FlexNetworkTransformManager reference.
        /// </summary>
        /// <param name="manager"></param>
        public void SetManagerInternal(FlexNetworkTransformManager manager) { _manager = manager; }
        /// <summary>
        /// Last authoritative client for this object.
        /// </summary>
        private NetworkConnection _lastAuthoritativeClient = null;
        /// <summary>
        /// 
        /// </summary>
        private byte? _cachedComponentIndex = null;
        /// <summary>
        /// Cached ComponentIndex for the NetworkBehaviour this FNT is on. This is because Mirror codes bad.
        /// </summary>
        public byte CachedComponentIndex
        {
            get
            {
                if (_cachedComponentIndex == null)
                {
                    //Exceeds value.
                    if (base.ComponentIndex > 255)
                    {
                        Debug.LogError("ComponentIndex is larger than supported type.");
                        _cachedComponentIndex = 0;
                    }
                    //Doesn't exceed value.
                    else
                    {
                        _cachedComponentIndex = (byte)Mathf.Abs(base.ComponentIndex);
                    }
                }

                return _cachedComponentIndex.Value;
            }
        }
        #endregion

        #region Initializers and Ticks
        protected virtual void Awake()
        {
            SetTeleportThresholdSquared();
#if MIRRORNG || MirrorNg
            base.NetIdentity.OnStartServer.AddListener(StartServer);
            base.NetIdentity.OnStartClient.AddListener(StartClient);
            base.NetIdentity.OnStartAuthority.AddListener(StartAuthority);
            base.NetIdentity.OnStopAuthority.AddListener(StopAuthority);
#endif
        }
        protected virtual void OnDestroy()
        {
            if (Attached.Target != null)
                Destroy(Attached.Target.gameObject);
#if MIRRORNG || MirrorNg
            base.NetIdentity.OnStopServer.RemoveListener(StartServer);
            base.NetIdentity.OnStartClient.RemoveListener(StartClient);
            base.NetIdentity.OnStartAuthority.RemoveListener(StartAuthority);
            base.NetIdentity.OnStopAuthority.RemoveListener(StopAuthority);
#endif            
        }

        protected virtual void OnEnable()
        {
            FlexNetworkTransformManager.AddToActive(this);
        }
        protected virtual void OnDisable()
        {
            FlexNetworkTransformManager.RemoveFromActive(this);
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                bool attachedValid = AttachedValid();
                writer.WriteBoolean(attachedValid);
                /* Set flags. */
                if (attachedValid)
                {
                    Serialization.WriteAttached(writer, CreateAttachedData().Value);
                    //Last Pos/rot for attached target.
                    writer.WriteVector3(Attached.Target.localPosition);
                    writer.WriteQuaternion(Attached.Target.localRotation);
                }
            }

            return base.OnSerialize(writer, initialState);
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                bool attachedValid = reader.ReadBoolean();
                //If attached.
                if (attachedValid)
                {
                    int unused = 0;
                    AttachedData ad = Serialization.ReadAttached(ref unused, reader);
                    UpdateAttached(ad);
                    //Attached pos/Rot.
                    Vector3 pos = reader.ReadVector3();
                    Quaternion rot = reader.ReadQuaternion();
                    Attached.Target.localPosition = pos;
                    Attached.Target.localRotation = rot;
                    /* If not authoritative client with client auth, and attached
                     * exist then make a new target data based on transforms offsets
                     * to attached with instant move rates. This is so the transform 
                     * sticks to the attached until new data comes in. */
                    if (!this.ReturnHasAuthority())
                    {
                        //If TargetData exist then re-use goal data from it to save on GC. Otherwise make a new goal data.
                        TransformSyncData goalData = (_targetData == null) ? new TransformSyncData() : _targetData.GoalData;
                        //Update goalData values. This is the last received sync data.
                        SetTransformSyncData(
                            ref goalData, (SyncProperties)goalData.SyncProperties,
                            pos, rot, TargetTransform.GetScale(),
                            CreateAttachedData()
                            );
                        //Update target data to move towards.
                        SetTargetSyncData(ref _targetData, goalData, SetInstantMoveRates(), null);
                        /* Force a move towards target. This is because server data can come often come in before
                         * Update runs, resulting in a new target data with improper move rates. */
                        MoveTowardsTargetSyncData();
                    }
                }
            }

            base.OnDeserialize(reader, initialState);
        }


#if MIRROR
        public override void OnStartServer()
        {
            base.OnStartServer();
            StartServer();
        }

#endif
        private void StartServer()
        {
            _networkVisibility = transform.root.GetComponent<NetworkVisibility>();
        }

#if MIRROR
        public override void OnStartClient()
        {
            base.OnStartClient();
            StartClient();
        }
#endif
        private void StartClient()
        {
            CheckCreateTransformTargetData();
        }


#if MIRROR
        public override void OnStartAuthority()
        {
            base.OnStartAuthority();
            StartAuthority();
        }
#endif
        private void StartAuthority()
        {
            /* If have authority and client authoritative then there is
            * no reason to have a targe data. */
            if (_clientAuthoritative)
                _targetData = null;
        }

#if MIRROR
        public override void OnStopAuthority()
        {
            base.OnStopAuthority();
            StopAuthority();
        }
#endif

        private void StopAuthority()
        {
            CheckCreateTransformTargetData();
        }

        public void ManualUpdate(bool fixedUpdate)
        {
            CheckResetSequenceIds();
            SnapToAttached();
            CheckSendToServer(fixedUpdate);
            CheckSendToClients(fixedUpdate);
            MoveTowardsTargetSyncData();
        }

        /// <summary>
        /// Sets TeleportThresholdSquared value.
        /// </summary>
        private void SetTeleportThresholdSquared()
        {
            if (_teleportThreshold < 0f)
                _teleportThreshold = 0f;

            _teleportThresholdSquared = (_teleportThreshold * _teleportThreshold);
        }

        /// <summary>
        /// Checks if conditions are met to call CreateTransformTargetData.
        /// </summary>
        private void CheckCreateTransformTargetData()
        {
            /* If a client starts without being allowed to move the object a target data
            * must be set using current transform values so that the client may not move
             * the object. */
            /* If target data has not already been received.
             * If not server, since server is boss and shouldn't be blocked.
             * If client does not have authority or 
             * have authority but not client authoritative. */
            if (_targetData == null && !this.ReturnIsServer() && (!this.ReturnHasAuthority() || (this.ReturnHasAuthority() && !_clientAuthoritative)))
                CreateTransformTargetData();
        }

        /// <summary>
        /// Creates target data according to where the transform is currently.
        /// </summary>
        protected void CreateTransformTargetData()
        {
            TransformSyncData tsd = new TransformSyncData();
            SetTransformSyncData(
                ref tsd, 0,
                TargetTransform.GetPosition(UseLocalSpace), TargetTransform.GetRotation(UseLocalSpace), TargetTransform.GetScale(),
                null
                );

            //Create new target data without extrpaolation.
            SetTargetSyncData(ref _targetData, tsd, SetInstantMoveRates(), null);
        }
        #endregion

        #region CheckSendTo
        /// <summary>
        /// Checks if client needs to send data to server.
        /// </summary>
        private void CheckSendToServer(bool fixedUpdate)
        {
            //Timed interval.
            if (_intervalType == IntervalTypes.Timed)
            {
                if (Time.time < _nextClientSendTime)
                    return;
            }
            //Fixed interval.
            else
            {
                if (!fixedUpdate)
                    return;
            }

            //Not using client auth movement.
            if (!_clientAuthoritative)
                return;
            //Only send to server if client.
            if (!this.ReturnIsClient())
                return;
            //Not authoritative client.
            if (!this.ReturnHasAuthority())
                return;

            bool attachedValid = AttachedValid();
            //If owner of target then move target to this transforms position.
            if (attachedValid && IsAttachedOwner())
            {

                Attached.Target.SetPosition(false, TargetTransform.GetPosition(false));
                Attached.Target.SetRotation(false, TargetTransform.GetRotation(false));
            }

            SyncProperties sp = ReturnDifferentTransformProperties(ref _clientSyncData, attachedValid);

            bool useReliable = _reliable;
            if (!CanSendProperties(ref sp, ref _clientSettleSent, ref useReliable))
                return;

            /* This only applies if using interval but
            * add anyway since the math operation is fast. 
            *
            * No reason to favor performance for a single client
            * as the performance differences will only be noticeable
            * from fent, since it will be potentially
            * updating a lot of clients vs one client updating server. */
            _nextClientSendTime = Time.time + _synchronizeInterval;
            //Add additional sync properties.
            ApplyRequiredSyncProperties(ref sp, false, attachedValid);

            AttachedSpaces attachedSpace = (attachedValid) ? AttachedSpaces.Local : AttachedSpaces.Disabled;
            Vector3 position = GetTransformPosition(attachedSpace);
            Quaternion rotation = GetTransformRotation(attachedSpace);
            Vector3 scale = TargetTransform.GetScale();
            SetUseCompression(ref sp, ref position, ref scale);
            SetTransformSyncData(
                ref _clientSyncData, sp,
                position, rotation, scale,
                CreateAttachedData()
                );

            //send to clients.
            if (useReliable)
                FlexNetworkTransformManager.SendToServer(ref _clientSyncData, true);
            else
                FlexNetworkTransformManager.SendToServer(ref _clientSyncData, false);
        }


        /// <summary>
        /// Checks if server needs to send data to clients.
        /// </summary>
        private void CheckSendToClients(bool fixedUpdate)
        {
            //Timed interval.
            if (_intervalType == IntervalTypes.Timed)
            {
                if (Time.time < _nextServerSendTime)
                    return;
            }
            //Fixed interval.
            else
            {
                if (!fixedUpdate)
                    return;
            }

            //Only send to clients if server.
            if (!this.ReturnIsServer())
                return;

            bool attachedValid = AttachedValid();
            //If owner of target then move target to this transforms position.
            if (attachedValid && IsAttachedOwner())
            {

                Attached.Target.SetPosition(false, TargetTransform.GetPosition(false));
                Attached.Target.SetRotation(false, TargetTransform.GetRotation(false));
            }


            /* TargetData is generated when there is a goal to move towards.
             * While null it's safe to assume the transform was snapped or is being controlled
             * as a client host, so data is up to date. So with that in mind,
             * if null we can use current transform values. */
            bool transformAtGoal = (_targetData == null);
            SyncProperties sp;
            /* If at goal then compare transform values
             * against the last sent values, _serverSyncData. */
            if (transformAtGoal)
                sp = ReturnDifferentTransformProperties(ref _serverSyncData, attachedValid);
            /* If not at goal then compare the last received
             * values, _targetData, against the last sent
             * values, _serverSyncData. */
            else
                sp = ReturnDifferentTargetDataProperties(ref _serverSyncData, _targetData);

            bool useReliable = _reliable;
            if (!CanSendProperties(ref sp, ref _serverSettleSent, ref useReliable))
            {
                //Slightly reset next send time to improve performance by reducing checks.
                _nextServerSendTime = Time.time + Mathf.Min((_synchronizeInterval / 2f), _interpolationFallbehind);
                return;
            }
            else
            {
                //Reset next send time since a send is going to occur.
                _nextServerSendTime = Time.time + _synchronizeInterval;
            }

            //Add additional sync properties.
            ApplyRequiredSyncProperties(ref sp, false, attachedValid);

            /* If not using server sync data then we are using
             * targetdata. This is when running as a client host, and
             * the data is what was received from a spectator. */
            if (!transformAtGoal)
            {
                SetUseCompression(ref sp, ref _targetData.GoalData.Position, ref _targetData.GoalData.Scale);
                /* Have to use just calculated sync properties because the sync properties
                 * from server to client can vary from what they were client to server when
                 * using client authority. */
                SetTransformSyncData(
                    ref _serverSyncData, sp,
                    _targetData.GoalData.Position, _targetData.GoalData.Rotation, _targetData.GoalData.Scale,
                   _targetData.GoalData.Attached
                   );
            }
            /* If using server data then transform was snapped into position
             * or has authority and is client host.
             * So there is no need to read from targetdata. */
            else
            {
                AttachedSpaces attachedSpace = (attachedValid) ? AttachedSpaces.Local : AttachedSpaces.Disabled;
                Vector3 position = GetTransformPosition(attachedSpace);
                Quaternion rotation = GetTransformRotation(attachedSpace);
                Vector3 scale = TargetTransform.GetScale();
                SetUseCompression(ref sp, ref position, ref scale);
                SetTransformSyncData(
                    ref _serverSyncData, sp,
                    position, rotation, scale,
                    CreateAttachedData()
                    );
            }

            if (_networkVisibility == null)
            {
                FlexNetworkTransformManager.SendToAll(ref _serverSyncData, useReliable);
            }
            else
            {
#if MIRROR
                foreach (NetworkConnection item in _networkVisibility.netIdentity.observers.Values)
#elif MIRRORNG
                foreach (INetworkConnection item in _networkVisibility.NetIdentity.observers)
#endif
                    FlexNetworkTransformManager.SendToObserver(item, ref _serverSyncData, useReliable);
            }
        }

        /// <summary>
        /// Creates attached data using current Attached.
        /// </summary>
        /// <returns></returns>
        private AttachedData? CreateAttachedData()
        {
            if (Attached.Identity == null)
                return null;
            else
                return new AttachedData() { NetId = Attached.Identity.ReturnNetworkId(), ComponentIndex = Attached.ComponentIndex };
        }
        #endregion

        /// <summary>
        /// Checks if sequenceIds need to be reset.
        /// </summary>
        private void CheckResetSequenceIds()
        {
            

            if (this.ReturnOwner() == _lastAuthoritativeClient)
                return;
            _lastAuthoritativeClient = this.ReturnOwner();

            LastClientSequenceId = 0;
        }

        /// <summary>
        /// Updates the attached cache and returns true if attached exist.
        /// </summary>
        private bool UpdateAttached(AttachedData? attached)
        {
            if (attached == null)
                return UpdateAttached(0, -1);
            else
                return UpdateAttached(attached.Value.NetId, attached.Value.ComponentIndex);
        }
        public static byte ATC = 0;
        /// <summary>
        /// Updates the attached cache and returns true if attached exist.
        /// </summary>
        private bool UpdateAttached(uint netId, sbyte componentIndex)
        {
            NetworkIdentity currentAttachedIdentity = Attached.Identity;
            //If net id is 0 then attached cannot be looked up.
            if (netId == 0)
            {
                Attached.Identity = null;
                if (Attached.Target != null)
                    Destroy(Attached.Target.gameObject);
            }
            else
            {
                /* True if the update should be blocked. This is to save performance.
                 * 
                 * True if current attached exist, and it's netId matches passed in Id. */
                bool blockUpdate = (Attached.Target != null && Attached.ComponentIndex == componentIndex && currentAttachedIdentity != null && currentAttachedIdentity.ReturnNetworkId() == netId);
                if (!blockUpdate)
                {
                    //Create Attached target if not present.
                    if (Attached.Target == null)
                    {
                        Attached.Target = new GameObject().transform;
                        NameAttachedTarget(Attached.Target);
                    }

                    if (Platforms.ReturnSpawned(_manager.CurrentNetworkManager).TryGetValue(netId, out NetworkIdentity ni))
                    {
                        Attached.Identity = ni;
                        Attached.ComponentIndex = componentIndex;
                    }
                    else
                    {
                        Attached.Identity = null;
                        Attached.ComponentIndex = -1;
                    }

                    //Child target to new attached object.
                    Transform attachTarget = null;
                    if (AttachedValid())
                    {
                        //If using a component to attach to.
                        if (Attached.ComponentIndex >= 0)
                        {
                            if (Attached.Identity.GetComponent<FlexAttachTargets>() is FlexAttachTargets foa)
                            {
                                GameObject go = foa.ReturnTarget(Attached.ComponentIndex);
                                if (go != null)
                                    attachTarget = go.transform;
                            }
                            //Object attacher not found.
                            else
                            {
                                Debug.LogWarning("FlexObjectAttacher is not found on identity " + Attached.Identity.gameObject + ".");
                            }
                        }
                        //Attaching to root.
                        else
                        {
                            attachTarget = Attached.Identity.transform;
                        }

                        Attached.Target.SetParent(attachTarget);
                        /* Set attached to transform position so that it doesn't interfer
                         * with smoothing between space change. */
                        Attached.Target.position = TargetTransform.GetPosition(false);
                        Attached.Target.rotation = TargetTransform.GetRotation(false);
                    }
                }
            }

            return AttachedValid();
        }

        /// <summary>
        /// Names the AttachedTarget object relevant to it's purpose. EG: if for local player, or spectator. Only needed for debugging.
        /// </summary>
        /// <param name="go"></param>
        private void NameAttachedTarget(Transform t)
        {
#if UNITY_EDITOR
            t.name = IsAttachedOwner() ?
                "OwnerTarget " + gameObject.name :
                "SpectatorTarget " + gameObject.name;
#endif
        }

        /// <summary>
        /// Returns true if owner to attached.
        /// </summary>
        /// <returns></returns>
        private bool IsAttachedOwner()
        {
            /* Can be attached owner under the following conditions:
             * IsServer, ClientAuthoritative and no owner.
             * IsClient, ClientAuthoritative and is owner. */
            if (_clientAuthoritative)
            {
                //Server with no owner.
                if (this.ReturnIsServer() && !this.ReturnHasOwner())
                    return true;
                //Client and is owner.
                if (this.ReturnIsClient())
                    return this.ReturnHasAuthority();
            }
            else
            {
                return this.ReturnIsServer();
            }

            //Fall through.
            return false;
        }
        /// <summary>
        /// Returns if Attached is valid.
        /// </summary>
        /// <returns></returns>
        private bool AttachedValid()
        {
            return (Attached.Identity != null && Attached.Identity.ReturnNetworkId() != 0);
        }

        /// <summary>
        /// Gets the position of the attached if one is used, otherwise uses transform values.
        /// </summary>
        private Vector3 GetTransformPosition(AttachedSpaces attachedSpace)
        {
            //Attached exist.
            if (attachedSpace != AttachedSpaces.Disabled)
                return (attachedSpace == AttachedSpaces.Local) ? Attached.Target.localPosition : Attached.Target.position;
            //No attached.
            else
                return TargetTransform.GetPosition(UseLocalSpace);
        }

        /// <summary>
        /// Gets the rotation of the attached if one is used, otherwise uses transform values.
        /// </summary>
        private Quaternion GetTransformRotation(AttachedSpaces attachedSpace)
        {
            //Attached exist.
            if (attachedSpace != AttachedSpaces.Disabled)
                return (attachedSpace == AttachedSpaces.Local) ? Attached.Target.localRotation : Attached.Target.rotation;
            //No attached.
            else
                return TargetTransform.GetRotation(UseLocalSpace);
        }

        /// <summary>
        /// Sets if can use compression for position and scale.
        /// </summary>
        /// <param name=""></param>
        /// <param name="pos"></param>
        /// <param name="scale"></param>
        private void SetUseCompression(ref SyncProperties sp, ref Vector3 pos, ref Vector3 scale)
        {
            if (!_compressSmall)
                return;

            //If position or scale can compress then add compress small.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Position) && Serialization.CanCompressVector3(ref pos) ||
                EnumContains.SyncPropertiesContains(sp, SyncProperties.Scale) && Serialization.CanCompressVector3(ref scale)
                )
                sp |= SyncProperties.CompressSmall;
        }
        /// <summary>
        /// Applies SyncProperties which are required based on settings.
        /// </summary>
        /// <param name="sp"></param>
        private void ApplyRequiredSyncProperties(ref SyncProperties sp, bool forceAll, bool attachedValid)
        {
            /* //FEATURE Attached is always sent when it exist.
             * In the future I'd like to only send if changed but this
             * won't be possible for UDP. */
            if (forceAll || attachedValid)
                sp |= SyncProperties.Attached;

            //If to force all.
            if (forceAll)
            {
                sp |= ReturnConfiguredSynchronizedProperties();
            }
            //If has settled then must include all transform values to ensure a perfect match.
            else if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Settled))
            {
                sp |= ReturnConfiguredSynchronizedProperties();
            }
            //If not reliable must send everything that is generally synchronized.
            else if (!_reliable)
            {
                if (_resendUnreliable)
                    sp |= ReturnConfiguredSynchronizedProperties();
            }

        }

        /// <summary>
        /// Returns properties which are configured to be synchronized. This does not mean all of these properties will send. These only send if using unreliable or if settled to force a synchronization.
        /// </summary>
        /// <returns></returns>
        private SyncProperties ReturnConfiguredSynchronizedProperties()
        {
            SyncProperties sp = SyncProperties.None;

            if (_synchronizePosition == SynchronizeTypes.Normal)
                sp |= SyncProperties.Position;
            if (_synchronizeRotation == SynchronizeTypes.Normal)
                sp |= SyncProperties.Rotation;
            if (_synchronizeScale == SynchronizeTypes.Normal)
                sp |= SyncProperties.Scale;

            return sp;
        }

        /// <summary>
        /// Returns if data updates should send based on SyncProperties, Reliable, and send history.
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        private bool CanSendProperties(ref SyncProperties sp, ref bool settleSent, ref bool useReliable)
        {
            //If nothing has changed.
            if (sp == SyncProperties.None)
            {
                /* If reliable is default and there's no extrapolation
                 * then there is no reason to send a settle packet.
                 * This is because extrapolation can overshoot while
                 * waiting for a new packet, but with extrapolation off
                 * the most recent reliable packet is always the latest
                 * data. */
                if (_reliable && _extrapolationSpan == 0f)
                    return false;

                //Settle has already been sent.
                if (settleSent)
                {
                    return false;
                }
                //Settle has not been sent yet.
                else
                {
                    //If transform has not been set as idle yet.
                    if (_transformIdleStart == -1f)
                    {
                        //Set idle start and return unable to send.
                        _transformIdleStart = Time.time;
                        return false;
                    }
                    //If transform has been set as idle already.
                    else
                    {
                        float idleRequirement;
                        //If no owner then allow to be idle for a quarter of the interpolation before sending a packet.
                        if (!this.ReturnHasOwner())
                            idleRequirement = _interpolationFallbehind * 0.25f;
                        else
                            //If has an owner allow to be idle for half of the interpolation.
                            idleRequirement = _interpolationFallbehind * 0.5f;

                        //If transform has been idle enough to send settled.
                        if ((Time.time - _transformIdleStart) >= idleRequirement)
                        {
                            settleSent = true;
                            useReliable = true;
                            sp |= SyncProperties.Settled;
                            return true;
                        }
                        //If not idle long enough, return unable to send.
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            //Properties need to be synchronized.
            else
            {
                //Unset settled.
                settleSent = false;
                //Unset that transform has been idle.
                _transformIdleStart = -1f;
                return true;
            }

        }

        /// <summary>
        /// Returns which properties need to be sent to maintain synchronization with the transforms current properties.
        /// </summary>
        /// <returns></returns>
        private SyncProperties ReturnDifferentTransformProperties(ref TransformSyncData data, bool attachedValid)
        {
            SyncProperties sp = SyncProperties.None;

            //Data is null, so it's definitely not a match.
            if (!data.Set)
            {
                ApplyRequiredSyncProperties(ref sp, true, attachedValid);
                return sp;
            }

            //Position.
            if (_synchronizePosition == SynchronizeTypes.Normal)
            {
                bool positionMatches = (attachedValid) ? AttachedPositionMatches(ref data) : TransformPositionMatches(ref data);
                if (!positionMatches)
                    sp |= SyncProperties.Position;
            }
            //Rotation.
            if (_synchronizeRotation == SynchronizeTypes.Normal)
            {
                bool rotationMatches = (attachedValid) ? AttachedRotationMatches(ref data) : TransformRotationMatches(ref data);
                if (!rotationMatches)
                    sp |= SyncProperties.Rotation;
            }
            //Scale.
            if (_synchronizeScale == SynchronizeTypes.Normal)
            {
                //Attached don't support scale, so if attached is valid then scale always matches.
                bool scaleMatches = (attachedValid) ? true : TransformScaleMatches(ref data);
                if (!scaleMatches)
                    sp |= SyncProperties.Scale;
            }

            return sp;
        }
        /// <summary>
        /// Returns which properties need to be sent to maintain synchronization with targetData properties.
        /// </summary>
        /// <param name="targetData">When specified data is compared against targetData. Otherwise, data is compared against the transform.</param>
        /// <returns></returns>
        private SyncProperties ReturnDifferentTargetDataProperties(ref TransformSyncData data, TargetSyncData targetData)
        {
            //Cannot compare if either data is null.
            if (!data.Set || targetData == null)
                return (SyncProperties.Position | SyncProperties.Rotation | SyncProperties.Scale | SyncProperties.Attached);

            SyncProperties sp = SyncProperties.None;
            //Position.
            if (_synchronizePosition == SynchronizeTypes.Normal)
            {
                if (!TargetDataPositionMatches(ref data, targetData))
                    sp |= SyncProperties.Position;
            }
            //Rotation.
            if (_synchronizeRotation == SynchronizeTypes.Normal)
            {
                if (!TargetDataRotationMatches(ref data, targetData))
                    sp |= SyncProperties.Rotation;
            }
            //Scale.
            if (_synchronizeScale == SynchronizeTypes.Normal)
            {
                if (!TargetDataScaleMatches(ref data, targetData))
                    sp |= SyncProperties.Scale;
            }

            return sp;
        }

        /// <summary>
        /// Snaps transform to attached Target if not attached owner.
        /// </summary>
        private void SnapToAttached()
        {
            ///* Only used to keep transform on the server when
            // * as server only. This ensures proper position updates
            // * will be sent to clients as needed, and that the object
            // * will not slide around on the server. */
            //if (!NetworkIsServerOnly())
            //    return;
            //if (!AttachedValid()) //todo recomment
            //    return;
            if (!AttachedValid())
                return;
            if (IsAttachedOwner())
                return;

            if (_synchronizePosition != SynchronizeTypes.NoSynchronization)
                TargetTransform.SetPosition(false, Attached.Target.position);
            if (_synchronizeRotation != SynchronizeTypes.NoSynchronization)
                TargetTransform.SetRotation(false, Attached.Target.rotation);
            //Attached support doesn't require scale syncing.
        }


        /// <summary>
        /// Moves towards TargetSyncData.
        /// </summary>
        private void MoveTowardsTargetSyncData()
        {
            //No SyncData to check against.
            if (_targetData == null)
                return;
            /* Client authority but there is no owner.
             * Can happen when client authority is ticked but
            * the server takes away authority. */
            if (this.ReturnIsServer() && _clientAuthoritative && !this.ReturnHasOwner() && _targetData != null)
            {
                /* Remove sync data so server no longer tries to sync up to last data received from client.
                 * Object may be moved around on server at this point. */
                _targetData = null;
                return;
            }
            //Client authority, don't need to synchronize with self.
            if (this.ReturnHasAuthority() && _clientAuthoritative)
                return;
            //Not client authority but also not synchronize to owner.
            if (this.ReturnHasAuthority() && !_clientAuthoritative && !_synchronizeToOwner)
                return;

            bool attachedValid = AttachedValid();
            bool extrapolate = (_targetData.Extrapolation != null && _targetData.Extrapolation.Remaining > 0f);

            /* Move attached target towards goal. */
            if (attachedValid)
                TryMoveAttachedTarget();

            /* Check if transform is at goal.
             * If Transform is at goal and there
             * is no extrapolation left then exit method. */
            if (TransformAtSyncData(ref _targetData.GoalData, attachedValid) && !extrapolate)
                return;
            /* Only move using localspace if configured to local space
             * and if not using a attached. Attached offsets arrive in local space
             * but the transform must move in world space to the attached
             * target. */
            bool useLocalSpace = (UseLocalSpace && !attachedValid);

            //Position
            if (_synchronizePosition != SynchronizeTypes.NoSynchronization)
            {
                Vector3 positionGoal = (attachedValid) ? Attached.Target.position : _targetData.GoalData.Position;

                /* If attached is valid then use instant move rates. This
                 * is because the attached target is already smooth so
                 * we can snap the transform. */
                float moveRate = (attachedValid) ? -1f : _targetData.MoveRates.Position;
                //Instantly.
                if (moveRate == -1f)
                {
                    TargetTransform.SetPosition(useLocalSpace, positionGoal);
                }
                //Over time.
                else
                {
                    //If to extrapolate then overwrite position goal with extrapolation.
                    if (extrapolate)
                        positionGoal = _targetData.Extrapolation.Position;

                    TargetTransform.SetPosition(useLocalSpace,
                        Vector3.MoveTowards(TargetTransform.GetPosition(useLocalSpace), positionGoal, moveRate * Time.deltaTime)
                        );
                }
            }
            //Rotation.
            if (_synchronizeRotation != SynchronizeTypes.NoSynchronization)
            {
                Quaternion rotationGoal = (attachedValid) ? Attached.Target.rotation : _targetData.GoalData.Rotation;
                /* If attached is valid then use instant move rates. This
                * is because the attached target is already smooth so
                * we can snap the transform. */
                float moveRate = (attachedValid) ? -1f : _targetData.MoveRates.Rotation;
                //Instantly.
                if (moveRate == -1f)
                {
                    TargetTransform.SetRotation(useLocalSpace, rotationGoal);
                }
                //Over time.
                else
                {
                    TargetTransform.SetRotation(UseLocalSpace,
                        Quaternion.RotateTowards(TargetTransform.GetRotation(useLocalSpace), rotationGoal, moveRate * Time.deltaTime)
                        );
                }
            }
            //Scale.
            if (_synchronizeScale != SynchronizeTypes.NoSynchronization)
            {
                Vector3 scaleGoal = _targetData.GoalData.Scale;
                //Instantly.
                if (_targetData.MoveRates.Scale == -1f)
                {
                    TargetTransform.SetScale(scaleGoal);
                }
                //Over time.
                else
                {
                    TargetTransform.SetScale(
                        Vector3.MoveTowards(TargetTransform.GetScale(), scaleGoal, _targetData.MoveRates.Scale * Time.deltaTime)
                        );
                }
            }

            //Remove from remaining extrapolation time.
            if (extrapolate)
                _targetData.Extrapolation.AddRemaining(-Time.deltaTime);
        }

        /// <summary>
        /// Tries to move the attached target to it's goal position. This method assumes a attached is valid.
        /// </summary>
        private void TryMoveAttachedTarget()
        {
            //Always use local space when moving the attached.
            bool useLocalSpace = true;
            //Position
            if (_synchronizePosition != SynchronizeTypes.NoSynchronization)
            {
                //Instant.
                if (_targetData.MoveRates.Position == -1f)
                {
                    Attached.Target.SetPosition(useLocalSpace, _targetData.GoalData.Position);
                }
                //Over time.
                else
                {
                    //Move target to goal.
                    Attached.Target.SetPosition(useLocalSpace,
                        Vector3.MoveTowards(Attached.Target.GetPosition(useLocalSpace), _targetData.GoalData.Position, _targetData.MoveRates.Position * Time.deltaTime)
                        );
                }
            }
            //Rotation.
            if (_synchronizeRotation != SynchronizeTypes.NoSynchronization)
            {
                //Instant.
                if (_targetData.MoveRates.Rotation == -1f)
                {
                    Attached.Target.SetRotation(useLocalSpace, _targetData.GoalData.Rotation);
                }
                //Over time.
                else
                {
                    //Move target to goal.
                    Attached.Target.SetRotation(useLocalSpace,
                        Quaternion.RotateTowards(Attached.Target.GetRotation(useLocalSpace), _targetData.GoalData.Rotation, _targetData.MoveRates.Rotation * Time.deltaTime)
                        );
                }
            }

            //Attached target ignores scale.
        }

        /// <summary>
        /// Returns true if the passed in axes contains all axes.
        /// </summary>
        /// <param name="axes"></param>
        /// <returns></returns>
        private bool SnapAll(SnappingAxes axes)
        {
            return (axes == (SnappingAxes.X | SnappingAxes.Y | SnappingAxes.Z));
        }

        /// <summary>
        /// Returns true if the TargetTransform is aligned with data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool TransformAtSyncData(ref TransformSyncData data, bool attachedValid)
        {
            if (!data.Set)
                return false;

            bool positionMatches = (attachedValid) ? TransformPositionMatchesAttached() : TransformPositionMatches(ref data);
            bool rotationMatches = (attachedValid) ? TransformRotationMatchesAttached() : TransformRotationMatches(ref data);
            bool scaleMatches = TransformScaleMatches(ref data);
            return (positionMatches && rotationMatches && scaleMatches);
        }

        #region Position Matches.
        /// <summary>
        /// Returns if the transform position matches attached position.
        /// </summary>
        /// <param name="precise"></param>
        /// <returns></returns>
        private bool TransformPositionMatchesAttached()
        {
            return Attached.Target.position == TargetTransform.GetPosition(false);
        }
        /// <summary>
        /// Returns if this Attached.Target position matches data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool AttachedPositionMatches(ref TransformSyncData data)
        {
            if (!data.Set)
                return false;

            return Attached.Target.localPosition == data.Position;
        }

        /// <summary>
        /// Returns if TargetTransform position matches data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool TransformPositionMatches(ref TransformSyncData data)
        {
            if (!data.Set)
                return false;

            return TargetTransform.GetPosition(UseLocalSpace) == data.Position;
        }
        /// <summary>
        /// Returns if this TargetData position matches data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool TargetDataPositionMatches(ref TransformSyncData data, TargetSyncData targetData)
        {
            if (!data.Set || targetData == null)
                return false;

            return targetData.GoalData.Position == data.Position;
        }
        #endregion

        #region Rotation Matches.
        /// <summary>
        /// Returns if this transform rotation matches Attached.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool TransformRotationMatchesAttached()
        {
            return Attached.Target.rotation == TargetTransform.GetRotation(false);
        }
        /// <summary>
        /// Returns if this transform rotation matches data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool AttachedRotationMatches(ref TransformSyncData data)
        {
            if (!data.Set)
                return false;

            return Attached.Target.localRotation == data.Rotation;
        }
        /// <summary>
        /// Returns if this transform rotation matches data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool TransformRotationMatches(ref TransformSyncData data)
        {
            if (!data.Set)
                return false;

            return TargetTransform.GetRotation(UseLocalSpace) == data.Rotation;
        }
        /// <summary>
        /// Returns if this transform rotation matches data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool TargetDataRotationMatches(ref TransformSyncData data, TargetSyncData targetData)
        {
            if (!data.Set || targetData == null)
                return false;

            return targetData.GoalData.Rotation == data.Rotation;
        }
        #endregion

        #region Scale Matches.
        /// <summary>
        /// Returns if this transform scale matches data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool TransformScaleMatches(ref TransformSyncData data)
        {
            if (!data.Set)
                return false;

            return TargetTransform.GetScale() == data.Scale;
        }
        /// <summary>
        /// Returns if this transform scale matches data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool TargetDataScaleMatches(ref TransformSyncData data, TargetSyncData targetData)
        {
            if (!data.Set || targetData == null)
                return false;

            return targetData.GoalData.Scale == data.Scale;
        }
        #endregion

        #region Data Received.
        /// <summary>
        /// Called on clients when server data is received.
        /// </summary>
        /// <param name="data"></param>
        public void ServerDataReceived(TransformSyncData data)
        {
            //If client host exit method.
            if (this.ReturnIsServer())
                return;

            //If owner of object.
            if (this.ReturnHasAuthority())
            {
                //Client authoritative, already in sync.
                if (_clientAuthoritative)
                    return;
                //Not client authoritative, but also not sync to owner.
                if (!_clientAuthoritative && !_synchronizeToOwner)
                    return;
            }

            //Fill in missing data for properties that werent included in send.
            FillMissingData(ref data, _targetData);

            /* If attached is valid then set the target transform
            * to values received from the client. */
            bool attachedValid = UpdateAttached(data.Attached);

            ExtrapolationData extrapolation = null;
            MoveRateData moveRates;

            //If teleporting set move rates to be instantaneous.
            if (ShouldTeleport(ref data, attachedValid))
            {
                moveRates = SetInstantMoveRates();
            }
            //If not teleporting calculate extrapolation and move rates.
            else
            {
                extrapolation = SetExtrapolation(ref data, _targetData, attachedValid);
                moveRates = SetMoveRates(ref data, attachedValid);
            }

            ApplyTransformSnapping(ref data, false, attachedValid);
            SetTargetSyncData(ref _targetData, data, moveRates, extrapolation);
        }


        /// <summary>
        /// Called on clients when server data is received.
        /// </summary>
        /// <param name="data"></param>
        public void ClientDataReceived(TransformSyncData data)
        {
            //Sent to self.
            if (this.ReturnHasAuthority())
                return;

            //Fill in missing data for properties that werent included in send.
            FillMissingData(ref data, _targetData);

            /* If attached is valid then set the target transform
            * to values received from the client. */
            bool attachedValid = UpdateAttached(data.Attached);

            //Only build for event if there are listeners.
            if (OnClientDataReceived != null)
            {
                ReceivedClientData rcd = new ReceivedClientData(ReceivedClientData.DataTypes.Interval, UseLocalSpace, ref data);
                OnClientDataReceived.Invoke(rcd);

                //If data was nullified then do nothing.
                if (!rcd.Data.Set || !data.Set)
                    return;
            }

            /* If server only then snap to target position. 
             * Should I ever add extrapolation on server only
             * then I would need to move smoothly instead and
             * perform extrapolation
             * calculations. */
            if (this.ReturnIsServerOnly())
            {
                ApplyTransformSnapping(ref data, true, attachedValid);
                /* If there is an attached then target data must
                 * be set to keep object on attached. Otherwise
                 * targetdata can be nullified. */
                if (attachedValid)
                    SetTargetSyncData(ref _targetData, data, SetInstantMoveRates(), null);
                else
                    _targetData = null;
            }
            /* If not server only, so if client host, then set data
             * normally for smoothing. */
            else
            {
                ExtrapolationData extrapolation = null;
                MoveRateData moveRates;
                //If teleporting set move rates to be instantaneous.
                if (ShouldTeleport(ref data, attachedValid))
                {
                    moveRates = SetInstantMoveRates();
                    ApplyTransformSnapping(ref data, true, attachedValid);
                }
                //If not teleporting calculate extrapolation and move rates.
                else
                {
                    extrapolation = SetExtrapolation(ref data, _targetData, attachedValid);
                    moveRates = SetMoveRates(ref data, attachedValid);
                    ApplyTransformSnapping(ref data, false, attachedValid);
                }

                SetTargetSyncData(ref _targetData, data, moveRates, extrapolation);
            }
        }
        #endregion

        #region Misc.
        /// <summary>
        /// Sets or updates a TransformSyncData.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="sp"></param>
        /// <param name="lastSequenceId"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        /// <param name="attachedNetId"></param>
        private void SetTransformSyncData(ref TransformSyncData data, SyncProperties sp, Vector3 position, Quaternion rotation, Vector3 scale, AttachedData? attached)
        {
            //NetworkIdentity is always included so may as well put it in here.
            //Compress network identity.
            uint networkidentity = this.ReturnNetId();
            if (networkidentity <= 255)
                sp |= SyncProperties.Id1;
            else if (networkidentity <= 65535)
                sp |= SyncProperties.Id2;

            //Mirror stores the component index as an int but they serialize it as a byte.
            data.UpdateValues((byte)sp, networkidentity, CachedComponentIndex,
                position, rotation, scale,
                attached
                );
        }

        /// <summary>
        /// Sets or updates a TargetSyncData.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="goalData"></param>
        /// <param name="moveRates"></param>
        /// <param name="extrapolationData"></param>
        private void SetTargetSyncData(ref TargetSyncData data, TransformSyncData goalData, MoveRateData moveRates, ExtrapolationData extrapolationData)
        {
            if (data == null)
                data = new TargetSyncData();

            data.UpdateValues(goalData, moveRates, extrapolationData);
        }

        /// <summary>
        /// Returns synchronization interval used.
        /// </summary>
        /// <returns></returns>
        private float ReturnSyncInterval()
        {
            return (_intervalType == IntervalTypes.FixedUpdate) ? Time.fixedDeltaTime : _synchronizeInterval;
        }

        /// <summary>
        /// Returns if the transform should teleport.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool ShouldTeleport(ref TransformSyncData data, bool attachedValid)
        {
            if (_teleportThresholdSquared <= 0f)
                return false;

            Vector3 position = (attachedValid) ? GetTransformPosition(AttachedSpaces.Local) : GetTransformPosition(AttachedSpaces.Disabled);

            float dist = Vectors.FastSqrMagnitude(position - data.Position);
            return dist >= _teleportThresholdSquared;
        }


        /// <summary>
        /// Sets MoveRates to move instantly.
        /// </summary>
        /// <returns></returns>
        private MoveRateData SetInstantMoveRates()
        {
            return new MoveRateData()
            {
                Position = -1f,
                Rotation = -1f,
                Scale = -1f
            };
        }

        /// <summary>
        /// Sets MoveRates based on data, transform position, and synchronization interval.
        /// </summary>
        /// <param name="data"></param>
        private MoveRateData SetMoveRates(ref TransformSyncData data, bool attachedValid)
        {
            float past = ReturnSyncInterval() + _interpolationFallbehind;
            AttachedSpaces attachedSpace = (attachedValid) ? AttachedSpaces.Local : AttachedSpaces.Disabled;

            MoveRateData moveRates = new MoveRateData();
            float distance;
            /* Position. */
            Vector3 position = GetTransformPosition(attachedSpace);
            distance = Vector3.Distance(position, data.Position);
            moveRates.Position = distance / past;
            /* Rotation. */
            Quaternion rotation = GetTransformRotation(attachedSpace);
            distance = Quaternion.Angle(rotation, data.Rotation);
            moveRates.Rotation = distance / past;
            /* Scale. */
            distance = Vector3.Distance(TargetTransform.GetScale(), data.Scale);
            moveRates.Scale = distance / past;

            return moveRates;
        }


        /// <summary>
        /// Sets ExtrapolationExtra using TransformSyncData.
        /// </summary>
        private ExtrapolationData SetExtrapolation(ref TransformSyncData data, TargetSyncData previousTargetSyncData, bool attachedValid)
        {
            //Feature: cannot extrapolate on attached currently.
            if (attachedValid)
                return null;
            /* If attached Id changed. 
             * Cannot extrapolate when attached Ids change because
             * the space used is changed. */
            if (previousTargetSyncData != null && !AttachedData.Matches(ref previousTargetSyncData.GoalData.Attached, ref data.Attached))
                return null;
            //No extrapolation.
            if (_extrapolationSpan == 0f || previousTargetSyncData == null)
                return null;
            //Settled packet.
            if (EnumContains.SyncPropertiesContains((SyncProperties)data.SyncProperties, SyncProperties.Settled))
                return null;

            Vector3 positionDirection = (data.Position - previousTargetSyncData.GoalData.Position);
            //Feature: need to use proper attached spaces if supporting extrapolation on attached.
            Vector3 position = GetTransformPosition(AttachedSpaces.Disabled);
            Vector3 goalDirectionNormalzied = (data.Position - position).normalized;
            /* If direction to goal is different from extrapolation direction
             * then do not extrapolate. This can occur when the extrapolation
             * overshoots. If the extrapolation was to continue like this then
             * it would likely overshoot more and more, becoming extremely
             * offset. */
            if (goalDirectionNormalzied != positionDirection.normalized)
                return null;

            float multiplier = _extrapolationSpan / ReturnSyncInterval();

            return new ExtrapolationData(
                data.Position + (positionDirection * multiplier),
                _extrapolationSpan + ReturnSyncInterval()
            );
        }

        /// <summary>
        /// Snaps transforms to data where snapping is applicable.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="snapAll"></param>
        private void ApplyTransformSnapping(ref TransformSyncData data, bool snapAll, bool attachedValid)
        {
            //Snap attached, then snap transform to attached.
            if (attachedValid)
            {
                ApplyTransformSnapping(Attached.Target, ref data, snapAll, true);
                ApplyTransformSnapping(TargetTransform, (SyncProperties)data.SyncProperties, Attached.Target.position, Attached.Target.rotation, data.Scale, snapAll, false);
            }
            //Just snap transform to data.
            else
            {
                ApplyTransformSnapping(TargetTransform, ref data, snapAll, attachedValid);
            }
        }
        /// <summary>
        /// Snaps the transform to data where snapping is applicable.
        /// </summary>
        /// /// <param name="t"></param>
        /// <param name="data"></param>
        /// <param name="snapAll"></param>
        private void ApplyTransformSnapping(Transform t, ref TransformSyncData data, bool snapAll, bool snappingAttached)
        {
            ApplyTransformSnapping(t, (SyncProperties)data.SyncProperties, data.Position, data.Rotation, data.Scale, snapAll, snappingAttached);
        }
        /// <summary>
        /// Snaps the transform to data where snapping is applicable.
        /// </summary>
        /// <param name="targetData">Data to snap from.</param>
        private void ApplyTransformSnapping(Transform t, SyncProperties sp, Vector3 position, Quaternion rotation, Vector3 scale, bool snapAll, bool snappingAttached)
        {
            if (t == null)
                return;

            //If using local space or attached space is specified.
            bool useLocalSpace = (UseLocalSpace || snappingAttached);
            if (snapAll || EnumContains.SyncPropertiesContains(sp, SyncProperties.Position))
            {
                //If to snap all.
                if (snapAll || SnapAll(_snapPosition))
                {
                    t.SetPosition(useLocalSpace, position);
                }
                //Snap some or none.
                else
                {
                    //Snap X.
                    if (EnumContains.AxesContains(_snapPosition, SnappingAxes.X))
                        t.SetPosition(useLocalSpace, new Vector3(position.x, t.GetPosition(useLocalSpace).y, t.GetPosition(useLocalSpace).z));
                    //Snap Y.
                    if (EnumContains.AxesContains(_snapPosition, SnappingAxes.Y))
                        t.SetPosition(useLocalSpace, new Vector3(t.GetPosition(useLocalSpace).x, position.y, t.GetPosition(useLocalSpace).z));
                    //Snap Z.
                    if (EnumContains.AxesContains(_snapPosition, SnappingAxes.Z))
                        t.SetPosition(useLocalSpace, new Vector3(t.GetPosition(useLocalSpace).x, t.GetPosition(useLocalSpace).y, position.z));
                }
            }

            /* Rotation. */
            if (snapAll || EnumContains.SyncPropertiesContains(sp, SyncProperties.Rotation))
            {
                //If to snap all.
                if (snapAll || SnapAll(_snapRotation))
                {
                    t.SetRotation(useLocalSpace, rotation);
                }
                //Snap some or none.
                else
                {
                    /* Only perform snap checks if snapping at least one
                     * to avoid extra cost of calculations. */
                    if ((int)_snapRotation != 0)
                    {
                        /* Convert to eulers since that is what is shown
                         * in the inspector. */
                        Vector3 startEuler = t.GetRotation(UseLocalSpace).eulerAngles;
                        Vector3 targetEuler = rotation.eulerAngles;
                        //Snap X.
                        if (EnumContains.AxesContains(_snapRotation, SnappingAxes.X))
                            startEuler.x = targetEuler.x;
                        //Snap Y.
                        if (EnumContains.AxesContains(_snapRotation, SnappingAxes.Y))
                            startEuler.y = targetEuler.y;
                        //Snap Z.
                        if (EnumContains.AxesContains(_snapRotation, SnappingAxes.Z))
                            startEuler.z = targetEuler.z;

                        //Rebuild into quaternion.
                        t.SetRotation(useLocalSpace, Quaternion.Euler(startEuler));
                    }
                }
            }

            /* Scale.
             * Only snap scale if not Attached Target
             * as Attached Target doesn't need scale. */
            if (t != Attached.Target && snapAll || EnumContains.SyncPropertiesContains(sp, SyncProperties.Scale))
            {
                //If to snap all.
                if (snapAll || SnapAll(_snapScale))
                {
                    t.SetScale(scale);
                }
                //Snap some or none.
                else
                {
                    //Snap X.
                    if (EnumContains.AxesContains(_snapScale, SnappingAxes.X))
                        t.SetScale(new Vector3(scale.x, t.GetScale().y, t.GetScale().z));
                    //Snap Y.
                    if (EnumContains.AxesContains(_snapScale, SnappingAxes.Y))
                        t.SetPosition(UseLocalSpace, new Vector3(t.GetScale().x, scale.y, t.GetScale().z));
                    //Snap Z.
                    if (EnumContains.AxesContains(_snapScale, SnappingAxes.Z))
                        t.SetPosition(UseLocalSpace, new Vector3(t.GetScale().x, t.GetScale().y, scale.z));
                }
            }
        }

        /// <summary>
        /// Modifies values within goalData based on what data was included in the packet.
        /// For example, if rotation was not included in the packet then the last datas rotation will be used, or transforms current rotation if there is no previous packet.
        /// </summary>
        private void FillMissingData(ref TransformSyncData data, TargetSyncData targetSyncData)
        {
            SyncProperties sp = (SyncProperties)data.SyncProperties;
            /* Begin by setting goal data using what has been serialized
            * via the writer. */
            //Position wasn't included.
            if (!EnumContains.SyncPropertiesContains(sp, SyncProperties.Position))
            {
                if (targetSyncData == null)
                    data.Position = TargetTransform.GetPosition(UseLocalSpace);
                else
                    data.Position = targetSyncData.GoalData.Position;
            }
            //Rotation wasn't included.
            if (!EnumContains.SyncPropertiesContains(sp, SyncProperties.Rotation))
            {
                if (targetSyncData == null)
                    data.Rotation = TargetTransform.GetRotation(UseLocalSpace);
                else
                    data.Rotation = targetSyncData.GoalData.Rotation;
            }
            //Scale wasn't included.
            if (!EnumContains.SyncPropertiesContains(sp, SyncProperties.Scale))
            {
                if (targetSyncData == null)
                    data.Scale = TargetTransform.GetScale();
                else
                    data.Scale = targetSyncData.GoalData.Scale;
            }

            /* Attached data will always be included every packet
             * if an attached is present. */
        }
        #endregion    

        #region Editor.
        private void OnValidate()
        {
            SetTeleportThresholdSquared();
        }
        #endregion
    }
}

