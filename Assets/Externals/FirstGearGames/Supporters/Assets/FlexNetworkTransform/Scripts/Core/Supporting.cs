using Mirror;
using FirstGearGames.Utilities.Networks;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

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
        public ReceivedClientData(DataTypes dataType, bool localSpace, ref TransformSyncData data)
        {
            DataType = dataType;
            LocalSpace = localSpace;
            Data = data;
        }

        public DataTypes DataType;
        public bool LocalSpace;
        public TransformSyncData Data;
    }

    /// <summary>
    /// Possible axes to snap.
    /// </summary>
    [System.Serializable, System.Flags]
    public enum SnappingAxes : int
    {
        X = 1,
        Y = 2,
        Z = 4
    }

    /// <summary>
    /// Indicates how each axes is compressed.
    /// </summary>
    [System.Flags]
    public enum CompressedAxes : byte
    {
        None = 0,
        XPositive = 1,
        XNegative = 2,
        YPositive = 4,
        YNegative = 8,
        ZPositive = 16,
        ZNegative = 32
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
        public static bool AxesContains(SnappingAxes whole, SnappingAxes part)
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
        /// ComponentIndex for the attached. Byte.MaxValue represents no ComponentIndex.
        /// </summary>
        public sbyte ComponentIndex;
        /// <summary>
        /// Sets the ComponentIndex value.
        /// </summary>
        /// <param name="componentIndex"></param>
        public void SetData(uint netId, sbyte componentIndex)
        {
            NetId = netId;
            ComponentIndex = componentIndex;
        }

        /// <summary>
        /// Returns if an AttachedData matches another.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Matches(ref AttachedData? a, ref AttachedData? b)
        {
            //Both are null, therefor equal.
            if (a == null && b == null)
                return true;
            //One is null, other is not.
            if ((a == null) != (b == null))
                return false;

            /* If here neither is null. */
            return (a.Value.NetId == b.Value.NetId && a.Value.ComponentIndex == b.Value.ComponentIndex);
        }
    }

    /// <summary>
    /// Container holding latest transform values.
    /// </summary>
    [System.Serializable]
    //[StructLayout(LayoutKind.Auto)]
    public struct TransformSyncData
    {
        public void UpdateValues(byte syncProperties, uint networkIdentity, byte componentIndex, Vector3 position, Quaternion rotation, Vector3 scale, AttachedData? attached)
        {
            SyncProperties = syncProperties;
            NetworkIdentity = networkIdentity;
            ComponentIndex = componentIndex;
            Position = position;
            Rotation = rotation;
            Scale = scale;
            Attached = attached;
            Set = true;
        }

        public byte SyncProperties;
        public uint NetworkIdentity;
        public byte ComponentIndex;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public AttachedData? Attached;
        [System.NonSerialized]
        public bool Set;
    }

    public static class Helpers
    {
        /// <summary>
        /// Returns the NetworkBehaviour for the specified NetworkIdentity and component index.
        /// </summary>
        /// <param name="componentIndex"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkBehaviour ReturnNetworkBehaviour(NetworkIdentity netIdentity, byte componentIndex)
        {
            if (netIdentity == null)
                return null;
            /* Networkbehaviours within the collection are the same order as compenent indexes.
            * I can save several iterations by simply grabbing the index from the networkbehaviours collection rather than iterating
            * it. */
            //A network behaviour was removed or added at runtime, component counts don't match up.
            if (componentIndex >= netIdentity.NetworkBehaviours.Length)
                return null;

            return netIdentity.NetworkBehaviours[componentIndex];
        }
    }


}