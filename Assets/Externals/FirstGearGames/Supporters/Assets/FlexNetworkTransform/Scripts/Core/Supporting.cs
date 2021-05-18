using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms
{
    /// <summary>
    /// Data received on server from clients when using Client Authoritative movement.
    /// </summary>
    public class ReceivedClientData
    {
        #region Types.
        public enum DataTypes
        {
            Interval = 0,
            Teleport = 1
        }
        #endregion
        public ReceivedClientData() { }
        public ReceivedClientData(DataTypes dataType, bool localSpace, ref TransformData data)
        {
            DataType = dataType;
            LocalSpace = localSpace;
            Data = data;
        }

        public DataTypes DataType;
        public bool LocalSpace;
        public TransformData Data;
    }

    /// <summary>
    /// Possible axes to snap.
    /// </summary>
    [System.Serializable, System.Flags]
    public enum Vector3Axes : int
    {
        X = 1,
        Y = 2,
        Z = 4
    }



    /// <summary>
    /// Transform properties which need to be synchronized.
    /// </summary>
    [System.Flags]
    public enum SyncProperties : byte
    {
        None = 0,
        //Position included.
        Position = 1,
        //Rotation included.
        Rotation = 2,
        //Scale included.
        Scale = 4,
        //Indicates transform did not move.
        Settled = 8,
        //Indicates transform is attached to a network object.
        Attached = 16,
        //Indicates to compress small values.
        CompressSmall = 32,
        //Indicates a compression level.
        Id1 = 64,
        //Indicates a compression level.
        Id2 = 128
    }

    /// <summary>
    /// Which loop to smooth within.
    /// </summary>
    public enum SmoothingLoops
    {
        FixedUpdate = 0,
        Update = 1,
        LateUpdate = 2
    }

    /// <summary>
    /// Using strongly typed for performance.
    /// </summary>
    public static class EnumContains
    {
        /// <summary>
        /// Returns if a CompressedAxes Whole contains Part.
        /// </summary>
        /// <param name="whole"></param>
        /// <param name="part"></param>
        /// <returns></returns>
        public static bool CompressedAxesContains(CompressedAxes whole, CompressedAxes part)
        {
            return (whole & part) == part;
        }
        /// <summary>
        /// Returns if a SyncProperties Whole contains Part.
        /// </summary>
        /// <param name="whole"></param>
        /// <param name="part"></param>
        /// <returns></returns>
        public static bool SyncPropertiesContains(SyncProperties whole, SyncProperties part)
        {
            return (whole & part) == part;
        }

        /// <summary>
        /// Returns if a Axess Whole contains Part.
        /// </summary>
        /// <param name="whole"></param>
        /// <param name="part"></param>
        /// <returns></returns>
        public static bool Vector3AxesContains(Vector3Axes whole, Vector3Axes part)
        {
            return (whole & part) == part;
        }
    }

    /// <summary>
    /// Data about what the transform is attached to.
    /// </summary>
    public struct AttachedData
    {
        /// <summary>
        /// NetworkId for the attached.
        /// </summary>
        public uint NetId;
        /// <summary>
        /// Index within FlexAttachTargets to use. Will be -1 if using root on Identity.
        /// </summary>
        public sbyte AttachedTargetIndex;
        /// <summary>
        /// True if set.
        /// </summary>
        public bool IsSet;
        /// <summary>
        /// Sets the ComponentIndex value.
        /// </summary>
        /// <param name="componentIndex"></param>
        public void SetData(uint netId, sbyte componentIndex)
        {
            NetId = netId;
            AttachedTargetIndex = componentIndex;
            IsSet = (NetId != 0);
        }

        /// <summary>
        /// Returns if an AttachedData matches another.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Matches(AttachedData a, AttachedData b)
        {
            //Different isSet, so cannot be a match.
            if (a.IsSet != b.IsSet)
                return false;

            return (a.NetId == b.NetId && a.AttachedTargetIndex == b.AttachedTargetIndex);
        }
    }

    /// <summary>
    /// Container holding latest transform values.
    /// </summary>
    [System.Serializable]
    public struct TransformData
    {
        public void UpdateValues(byte syncProperties, uint networkIdentity, byte componentIndex, Vector3 position, Quaternion rotation, Vector3 scale, AttachedData attached)
        {
            SyncProperties = syncProperties;
            NetworkIdentity = networkIdentity;
            ComponentIndex = componentIndex;
            Position = position;
            Rotation = rotation;
            Scale = scale;
            Attached = attached;
            IsSet = true;
        }

        public byte SyncProperties;
        public uint NetworkIdentity;
        public byte ComponentIndex;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public AttachedData Attached;
        [System.NonSerialized]
        public bool IsSet;
    }

}