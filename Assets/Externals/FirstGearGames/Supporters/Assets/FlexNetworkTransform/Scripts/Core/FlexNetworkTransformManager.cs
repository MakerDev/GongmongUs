using FirstGearGames.Utilities.Networks;
using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
#if MIRRORNG || MirrorNg
using NetworkConnection = Mirror.INetworkConnection;
#endif


namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms
{
    [System.Serializable]
#if MIRROR
    public struct TransformSyncDataMessage : NetworkMessage
#elif MIRRORNG
    public struct TransformSyncDataMessage
#endif
    {
        public ushort SequenceId;
        public ArraySegment<byte> Data;
    }

    public class FlexNetworkTransformManager : MonoBehaviour
    {

        #region Serialized
        /// <summary>
        /// 
        /// </summary>
#if MIRRORNG || MirrorNg
        [Tooltip("Current NetworkManager.")]
        [SerializeField]
#endif
        private NetworkManager _networkManager = null;
        /// <summary>
        /// Current NetworkManager.
        /// </summary>
        public NetworkManager CurrentNetworkManager { get { return _networkManager; } }
        /// <summary>
        /// True to make this gameObject dont destroy on load. True is recommended if your NetworkManager is also dont destroy on load.
        /// </summary>
#if MIRRORNG || MirrorNg
        [Tooltip("True to make this gameObject dont destroy on load. True is recommended if your NetworkManager is also dont destroy on load.")]
        [SerializeField]
#endif
        private bool _dontDestroyOnLoad = true;
        #endregion

        #region Private.
        /// <summary>
        /// Active FlexNetworkTransform components.
        /// </summary>
        private static List<FlexNetworkTransformBase> _activeFlexNetworkTransforms = new List<FlexNetworkTransformBase>();
        /// <summary>
        /// Unreliable SyncDatas to send to all.
        /// </summary>
        private static List<TransformSyncData> _toAllUnreliableSyncData = new List<TransformSyncData>();
        /// <summary>
        /// Reliable SyncDatas to send to all.
        /// </summary>
        private static List<TransformSyncData> _toAllReliableSyncData = new List<TransformSyncData>();
        /// <summary>
        /// Unreliable SyncDatas to send to server.
        /// </summary>
        private static List<TransformSyncData> _toServerUnreliableSyncData = new List<TransformSyncData>();
        /// <summary>
        /// Reliable SyncDatas to send send to server.
        /// </summary>
        private static List<TransformSyncData> _toServerReliableSyncData = new List<TransformSyncData>();
        /// <summary>
        /// Unreliable SyncDatas sent to specific observers.
        /// </summary>
        private static Dictionary<NetworkConnection, List<TransformSyncData>> _observerUnreliableSyncData = new Dictionary<NetworkConnection, List<TransformSyncData>>();
        /// <summary>
        /// Reliable SyncDatas sent to specific observers.
        /// </summary>
        private static Dictionary<NetworkConnection, List<TransformSyncData>> _observerReliableSyncData = new Dictionary<NetworkConnection, List<TransformSyncData>>();
        /// <summary>
        /// True if a fixed frame.
        /// </summary>
        private bool _fixedFrame = false;
        /// <summary>
        /// Last fixed frame.
        /// </summary>
        private int _lastFixedFrame = -1;
        /// <summary>
        /// Last sequenceId sent by server.
        /// </summary>
        private ushort _lastServerSentSequenceId = 0;
        /// <summary>
        /// Last sequenceId received from server.
        /// </summary>
        private ushort _lastServerReceivedSequenceId = 0;
        /// <summary>
        /// Last sequenceId sent by this client.
        /// </summary>
        private ushort _lastClientSentSequenceId = 0;
        /// <summary>
        /// Last NetworkClient.active state.
        /// </summary>
        private bool _lastClientActive = false;
        /// <summary>
        /// Last NetworkServer.active state.
        /// </summary>
        private bool _lastServerActive = false;
        /// <summary>
        /// How much data can be bundled per reliable message.
        /// </summary>
        private int _reliableMTU = -1;
        /// <summary>
        /// How much data can be bundled per unreliable message.
        /// </summary>
        private int _unreliableMTU = -1;
        /// <summary>
        /// Buffer to send outgoing data. Segments will always be 1200 or less.
        /// </summary>
        private byte[] _writerBuffer = new byte[1200];
        /// <summary>
        /// Used to prevent GC with GetComponents.
        /// </summary>
        private List<FlexNetworkTransformBase> _getComponents = new List<FlexNetworkTransformBase>();
        /// <summary>
        /// Singleton of this script. Used to ensure script is not loaded more than once. This will change for NG once custom message subscriptions are supported.
        /// </summary>
        private static FlexNetworkTransformManager _instance;
        #endregion

        #region Const.
        /// <summary>
        /// Maximum packet size by default. This is used when packet size is unknown.
        /// </summary>
        private const int MAXIMUM_PACKET_SIZE = 1200;
        /// <summary>
        /// Guestimated amount of how much MTU will be needed to send one transform on any transport. This will likely never be a problem but just incase.
        /// </summary>
        private const int MINIMUM_MTU_REQUIREMENT = 150;
        #endregion

#if MIRROR
        /// <summary>
        /// Automatically initializes script. Only works for Mirror.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void MirrorFirstInitialize()
        {
            GameObject go = new GameObject();
            go.name = "FlexNetworkTransformManager";
            go.AddComponent<FlexNetworkTransformManager>();
        }
#endif

        private void Awake()
        {
            FirstInitialize();
        }

        /// <summary>
        /// Initializes script. Only works for MirrorNG.
        /// </summary>
        private void FirstInitialize()
        {
            if (_instance != null)
            {
                Debug.LogError("Multiple FlexNetworkTransformManager instances found. This new instance will be destroyed.");
                Destroy(this);
                return;
            }

            _instance = this;
            if (_dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }

        private void FixedUpdate()
        {
            /* Don't send if the same frame. Since
            * physics aren't actually involved there is
            * no reason to run logic twice on the
            * same frame; that will only hurt performance
            * and the network more. */
            if (Time.frameCount == _lastFixedFrame)
                return;
            _lastFixedFrame = Time.frameCount;

            _fixedFrame = true;
        }

        private void Update()
        {
            CheckRegisterHandlers();

            //Run updates on FlexNetworkTransforms.
            for (int i = 0; i < _activeFlexNetworkTransforms.Count; i++)
                _activeFlexNetworkTransforms[i].ManualUpdate(_fixedFrame);

            _fixedFrame = false;
            //Send any queued messages.
            SendMessages();
        }


        /// <summary>
        /// Registers handlers for the client.
        /// </summary>
        private void CheckRegisterHandlers()
        {
            bool ncActive = Platforms.ReturnClientActive(CurrentNetworkManager);
            bool nsActive = Platforms.ReturnServerActive(CurrentNetworkManager);
            bool changed = (_lastClientActive != ncActive || _lastServerActive != nsActive);
            //If wasn't active previously but is now then get handlers again.
            if (changed && ncActive)
                NetworkReplaceHandlers(true);
            if (changed && nsActive)
                NetworkReplaceHandlers(false);

            _lastClientActive = ncActive;
            _lastServerActive = nsActive;
        }

        /// <summary>
        /// Adds to ActiveFlexNetworkTransforms.
        /// </summary>
        /// <param name="fntBase"></param>
        public static void AddToActive(FlexNetworkTransformBase fntBase)
        {
            _activeFlexNetworkTransforms.Add(fntBase);
            fntBase.SetManagerInternal(_instance);
        }
        /// <summary>
        /// Removes from ActiveFlexNetworkTransforms.
        /// </summary>
        /// <param name="fntBase"></param>
        public static void RemoveFromActive(FlexNetworkTransformBase fntBase)
        {
            _activeFlexNetworkTransforms.Remove(fntBase);
        }

        /// <summary>
        /// Sends data to server.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="reliable"></param>
        public static void SendToServer(ref TransformSyncData data, bool reliable)
        {
            /* Do not send as reference because a copy needs to be made to ensure
             * data is not replaced in the collection. */
            List<TransformSyncData> list = (reliable) ? _toServerReliableSyncData : _toServerUnreliableSyncData;
            list.Add(data);
        }

        /// <summary>
        /// Sends data to all.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="reliable"></param>
        public static void SendToAll(ref TransformSyncData data, bool reliable)
        {
            /* Do not send as reference because a copy needs to be made to ensure
            * data is not replaced in the collection. */
            List<TransformSyncData> list = (reliable) ? _toAllReliableSyncData : _toAllUnreliableSyncData;
            list.Add(data);
        }

        /// <summary>
        /// Sends data to observers.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="data"></param>
        /// <param name="reliable"></param>
        public static void SendToObserver(NetworkConnection conn, ref TransformSyncData data, bool reliable)
        {
            /* Do not send as reference because a copy needs to be made to ensure
            * data is not replaced in the collection. */
            /* Actually, it should be okay to use ref because each sync data is going to be the most recent.
             * And since I'm not ACTUALLY using delta, this shouldn't cause any harm. */
            Dictionary<NetworkConnection, List<TransformSyncData>> dict = (reliable) ? _observerReliableSyncData : _observerUnreliableSyncData;

            List<TransformSyncData> datas;
            //If doesn't have datas for connection yet then make new datas.
            if (!dict.TryGetValue(conn, out datas))
            {
                datas = new List<TransformSyncData>();
                dict[conn] = datas;
            }

            datas.Add(data);
        }

        /// <summary>
        /// Sends queued messages.
        /// </summary>
        private void SendMessages()
        {
            //If MTUs haven't been set yet.
            if (_reliableMTU == -1 || _unreliableMTU == -1)
                Platforms.SetMTU(ref _reliableMTU, ref _unreliableMTU, MAXIMUM_PACKET_SIZE);

            //Server.
            if (Platforms.ReturnServerActive(CurrentNetworkManager))
            {
                _lastServerSentSequenceId++;
                if (_lastServerSentSequenceId == ushort.MaxValue)
                    _lastServerSentSequenceId = 0;

                //Reliable to all.
                SendTransformSyncDatas(_lastServerSentSequenceId, false, null, _toAllReliableSyncData, true);
                //Unreliable to all.
                SendTransformSyncDatas(_lastServerSentSequenceId, false, null, _toAllUnreliableSyncData, false);
                //Reliable to observers.
                foreach (KeyValuePair<NetworkConnection, List<TransformSyncData>> item in _observerReliableSyncData)
                {
                    //Null or unready network connection.
                    if (item.Key == null || !item.Key.IsReady())
                        continue;

                    SendTransformSyncDatas(_lastServerSentSequenceId, false, item.Key, item.Value, true);
                }
                //Unreliable to observers.
                foreach (KeyValuePair<NetworkConnection, List<TransformSyncData>> item in _observerUnreliableSyncData)
                {
                    //Null or unready network connection.
                    if (item.Key == null || !item.Key.IsReady())
                        continue;

                    SendTransformSyncDatas(_lastServerSentSequenceId, false, item.Key, item.Value, false);
                }
            }
            //Client.
            if (Platforms.ReturnClientActive(CurrentNetworkManager))
            {
                _lastClientSentSequenceId++;
                if (_lastClientSentSequenceId == ushort.MaxValue)
                    _lastClientSentSequenceId = 0;

                //Reliable to all.
                SendTransformSyncDatas(_lastClientSentSequenceId, true, null, _toServerReliableSyncData, true);
                //Unreliable to all.
                SendTransformSyncDatas(_lastClientSentSequenceId, true, null, _toServerUnreliableSyncData, false);
            }

            _toServerReliableSyncData.Clear();
            _toServerUnreliableSyncData.Clear();
            _toAllReliableSyncData.Clear();
            _toAllUnreliableSyncData.Clear();
            _observerReliableSyncData.Clear();
            _observerUnreliableSyncData.Clear();
        }

        /// <summary>
        /// Sends data to all or specified connection.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="datas"></param>
        /// <param name="reliable"></param>
        /// <param name="maxCollectionSize"></param>
        private void SendTransformSyncDatas(ushort sequenceId, bool toServer, NetworkConnection conn, List<TransformSyncData> datas, bool reliable)
        {
            int index = 0;
            int channel = (reliable) ? 0 : 1;
            int mtu = (reliable) ? _reliableMTU : _unreliableMTU;
            //Subtract a set amount from mtu to account for headers and misc data.
            mtu -= 100;
#if UNITY_EDITOR
            if (mtu < MINIMUM_MTU_REQUIREMENT)
                Debug.LogWarning("MTU is dangerously low on channel " + channel + ". Data may not send properly.");
#endif

            while (index < datas.Count)
            {
                int writerPosition = 0;
                //Write until buffer is full or all indexes have been written.
                while (writerPosition < mtu && index < datas.Count)
                {
                    Serialization.SerializeTransformSyncData(_writerBuffer, ref writerPosition, datas, index);
                    index++;
                }

                TransformSyncDataMessage msg = new TransformSyncDataMessage()
                {
                    SequenceId = sequenceId,
                    Data = new ArraySegment<byte>(_writerBuffer, 0, writerPosition)
                };

                if (toServer)
                {
                    Platforms.ClientSend(CurrentNetworkManager, msg, channel);
                }
                else
                {
                    //If no connection then send to all.
                    if (conn == null)
                        Platforms.ServerSendToAll(CurrentNetworkManager, msg, channel);
                    //Otherwise send to connection.
                    else
                        conn.Send(msg, channel);
                }
            }
        }

        /// <summary>
        /// Received on clients when server sends data.
        /// </summary>
        /// <param name="msg"></param>
        private void OnServerTransformSyncData(TransformSyncDataMessage msg)
        {
            //Old packet.
            if (IsOldPacket(_lastServerReceivedSequenceId, msg.SequenceId))
                return;

            _lastServerReceivedSequenceId = msg.SequenceId;
            int readPosition = 0;
            while (readPosition < msg.Data.Count)
            {
                TransformSyncData tsd = Serialization.DeserializeTransformSyncData(ref readPosition, msg.Data);
                /* Initially I tried caching the getcomponent calls but the performance difference
                 * couldn't be registered. At this time it's not worth creating the extra complexity
                 * for what might be a 1% fps difference. */
                if (Platforms.ReturnSpawned(CurrentNetworkManager).TryGetValue(tsd.NetworkIdentity, out NetworkIdentity ni))
                {
                    FlexNetworkTransformBase fntBase = ReturnFNTBaseOnNetworkIdentity(ni, tsd.ComponentIndex);
                    if (fntBase != null)
                        fntBase.ServerDataReceived(tsd);
                }
            }

        }

        /// <summary>
        /// Received on server when client sends data.
        /// </summary>
        /// <param name="msg"></param>
        private void OnClientTransformSyncData(TransformSyncDataMessage msg)
        {
            //Have to check sequence id against the FNT sending.

            int readPosition = 0;
            while (readPosition < msg.Data.Count)
            {
                TransformSyncData tsd = Serialization.DeserializeTransformSyncData(ref readPosition, msg.Data);

                /* Initially I tried caching the getcomponent calls but the performance difference
                * couldn't be registered. At this time it's not worth creating the extra complexity
                * for what might be a 1% fps difference. */
                if (Platforms.ReturnSpawned(CurrentNetworkManager).TryGetValue(tsd.NetworkIdentity, out NetworkIdentity ni))
                {
                    FlexNetworkTransformBase fntBase = ReturnFNTBaseOnNetworkIdentity(ni, tsd.ComponentIndex);
                    if (fntBase != null)
                    {
                        //Skip if old packet.
                        if (IsOldPacket(fntBase.LastClientSequenceId, msg.SequenceId))
                            continue;

                        /* SequenceId is set per FNT because clients will be sending
                         * different sequenceIds each. */
                        fntBase.SetLastClientSequenceIdInternal(msg.SequenceId);
                        fntBase.ClientDataReceived(tsd);
                    }
                }
            }
        }

        /// <summary>
        /// Returns a FlexNetworkTransformBase on a networkIdentity using a componentIndex.
        /// </summary>
        /// <param name="componentIndex"></param>
        /// <returns></returns>
        private FlexNetworkTransformBase ReturnFNTBaseOnNetworkIdentity(NetworkIdentity ni, byte componentIndex)
        {
            NetworkBehaviour nb = Helpers.ReturnNetworkBehaviour(ni, componentIndex);
            if (nb == null)
                return null;

            nb.GetComponents<FlexNetworkTransformBase>(_getComponents);
            /* Now find the FNTBase which matches the component index. There is probably only one FNT
             * but if the user were using FNT + FNT Child there could be more so it's important to get all FNT
             * on the object. */
            for (int i = 0; i < _getComponents.Count; i++)
            {
                //Match found.
                if (_getComponents[i].CachedComponentIndex == componentIndex)
                    return _getComponents[i];
            }

            /* If here then the component index was found but the fnt with the component index
             * was not. This should never happen. */
            Debug.LogWarning("ComponentIndex found but FlexNetworkTransformBase was not.");
            return null;
        }


        /// <summary>
        /// Returns if a packet is old or out of order.
        /// </summary>
        /// <param name="lastSequenceId"></param>
        /// <param name="sequenceId"></param>
        /// <returns></returns>
        private bool IsOldPacket(ushort lastSequenceId, ushort sequenceId, ushort resetRange = 256)
        {
            /* New Id is equal or higher. Allow equal because
             * the same sequenceId will be used for when bundling
             * hundreds of FNTs over multiple sends. */
            if (sequenceId >= lastSequenceId)
            {
                return false;
            }
            //New sequenceId isn't higher, check if perhaps the sequenceId reset to 0.
            else
            {
                ushort difference = (ushort)Mathf.Abs(lastSequenceId - sequenceId);
                /* Return old packet if difference isnt beyond
                 * the reset range. Difference should be extreme if a reset occurred. */
                return (difference < resetRange);
            }
        }

        #region Platform specific Support.
        /// <summary>
        /// Replaces handlers.
        /// </summary>
        /// <param name="client">True to replace for client.</param>
        private void NetworkReplaceHandlers(bool client)
        {
            if (client)
            {
#if MIRROR
                NetworkClient.ReplaceHandler<TransformSyncDataMessage>(OnServerTransformSyncData);
#elif MIRRORNG
                CurrentNetworkManager.Client.Connection?.RegisterHandler<TransformSyncDataMessage>(OnServerTransformSyncData);
#endif
            }
            else
            {
#if MIRROR
                NetworkServer.ReplaceHandler<TransformSyncDataMessage>(OnClientTransformSyncData);
#elif MIRRORNG
                CurrentNetworkManager.Server.Connected.AddListener(delegate (INetworkConnection conn)
                {
                    conn.RegisterHandler<TransformSyncDataMessage>(OnClientTransformSyncData);
                });
#endif
            }
        }
        #endregion
    }


}