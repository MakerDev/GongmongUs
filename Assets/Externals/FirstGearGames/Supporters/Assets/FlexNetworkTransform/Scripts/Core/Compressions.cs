#if MIRROR
using Mirror;
#elif MIRAGE
using Mirage;
#endif
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms
{

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

    public class Compressions : MonoBehaviour
    {
        /// <summary>
        /// Maximum absolute value a float may be to be compressed.
        /// </summary>
        private const float MAX_FLOAT_COMPRESSION_VALUE = 654f;

        /// <summary>
        /// Writes a compressed Vector3 to the writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="ca"></param>
        /// <param name="v"></param>
        public static void WriteCompressedVector3(NetworkWriter writer, Vector3 v)
        {
            CompressedAxes ca = CompressedAxes.None;
            //If can compress X.
            float absX = Mathf.Abs(v.x);
            if (absX <= MAX_FLOAT_COMPRESSION_VALUE)
                ca |= (Mathf.Sign(v.x) > 0f) ? CompressedAxes.XPositive : CompressedAxes.XNegative;
            //If can compress Y.
            float absY = Mathf.Abs(v.y);
            if (absY <= MAX_FLOAT_COMPRESSION_VALUE)
                ca |= (Mathf.Sign(v.y) > 0f) ? CompressedAxes.YPositive : CompressedAxes.YNegative;
            //If can compress Z.
            float absZ = Mathf.Abs(v.z);
            if (absZ <= MAX_FLOAT_COMPRESSION_VALUE)
                ca |= (Mathf.Sign(v.z) > 0f) ? CompressedAxes.ZPositive : CompressedAxes.ZNegative;

            //Write compresed axes.
            writer.WriteByte((byte)ca);
            //X
            if (EnumContains.CompressedAxesContains(ca, CompressedAxes.XNegative) || EnumContains.CompressedAxesContains(ca, CompressedAxes.XPositive))
                writer.WriteUInt16((ushort)Mathf.Round(absX * 100f));
            else
                writer.WriteSingle(v.x);
            //Y
            if (EnumContains.CompressedAxesContains(ca, CompressedAxes.YNegative) || EnumContains.CompressedAxesContains(ca, CompressedAxes.YPositive))
                writer.WriteUInt16((ushort)Mathf.Round(absY * 100f));
            else
                writer.WriteSingle(v.y);
            //Z
            if (EnumContains.CompressedAxesContains(ca, CompressedAxes.ZNegative) || EnumContains.CompressedAxesContains(ca, CompressedAxes.ZPositive))
                writer.WriteUInt16((ushort)Mathf.Round(absZ * 100f));
            else
                writer.WriteSingle(v.z);
        }


        /// <summary>
        /// Reads a compressed Vector3.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="ca"></param>
        /// <param name="v"></param>
        public static Vector3 ReadCompressedVector3(NetworkReader reader)
        {
            CompressedAxes ca = (CompressedAxes)reader.ReadByte();
            //Sign of compressed axes. If 0f, no compression was used for the axes.
            float sign;

            //X
            float x;
            if (EnumContains.CompressedAxesContains(ca, CompressedAxes.XNegative))
                sign = -1f;
            else if (EnumContains.CompressedAxesContains(ca, CompressedAxes.XPositive))
                sign = 1f;
            else
                sign = 0f;
            //If there is compression.
            if (sign != 0f)
                x = (reader.ReadUInt16() / 100f) * sign;
            else
                x = reader.ReadSingle();

            //Y
            float y;
            if (EnumContains.CompressedAxesContains(ca, CompressedAxes.YNegative))
                sign = -1f;
            else if (EnumContains.CompressedAxesContains(ca, CompressedAxes.YPositive))
                sign = 1f;
            else
                sign = 0f;
            //If there is compression.
            if (sign != 0f)
                y = (reader.ReadUInt16() / 100f) * sign;
            else
                y = reader.ReadSingle();

            //Z
            float z;
            if (EnumContains.CompressedAxesContains(ca, CompressedAxes.ZNegative))
                sign = -1f;
            else if (EnumContains.CompressedAxesContains(ca, CompressedAxes.ZPositive))
                sign = 1f;
            else
                sign = 0f;
            //If there is compression.
            if (sign != 0f)
                z = (reader.ReadUInt16() / 100f) * sign;
            else
                z = reader.ReadSingle();

            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Returns if a Vector3 can be compressed.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static bool CanCompressVector3(ref Vector3 v)
        {
            return
                (v.x > -MAX_FLOAT_COMPRESSION_VALUE && v.x < MAX_FLOAT_COMPRESSION_VALUE) ||
                (v.y > -MAX_FLOAT_COMPRESSION_VALUE && v.y < MAX_FLOAT_COMPRESSION_VALUE) ||
                (v.z > -MAX_FLOAT_COMPRESSION_VALUE && v.z < MAX_FLOAT_COMPRESSION_VALUE);
        }
    }


}