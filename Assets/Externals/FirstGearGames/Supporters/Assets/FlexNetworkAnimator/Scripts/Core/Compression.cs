using Mirror;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkAnimators
{

    public static class Compression
    {

        /// <summary>
        /// Compression levels for data.
        /// </summary>
        public enum CompressionLevels : byte
        {
            //No compression.
            None = 0,
            //Data can fit into a byte.
            Level1Positive = 1,
            Level1Negative = 2,
            //Data can fit into a short.
            Level2Positive = 3,
            Level2Negative = 4
        }

        #region WriteCompressed.
        /// <summary>
        /// Writes a compressed uint to a writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        public static void WriteCompressedUInt(this PooledNetworkWriter writer, uint value)
        {
            //Fits in a byte.
            if (value <= byte.MaxValue)
            {
                writer.WriteByte((byte)CompressionLevels.Level1Positive);
                writer.WriteByte((byte)value);
            }
            //Fits in a ushort
            else if (value <= ushort.MaxValue)
            {
                writer.WriteByte((byte)CompressionLevels.Level2Positive);
                writer.WriteUInt16((ushort)value);
            }
            //Cannot compress.
            else
            {
                writer.WriteByte((byte)CompressionLevels.None);
                writer.WriteUInt32(value);
            }
        }
        /// <summary>
        /// Writes a compressed int to a writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        public static void WriteCompressedInt(this PooledNetworkWriter writer, int value)
        {
            int absolute = Mathf.Abs(value);
            bool positive = (value >= 0);
            //Fits in a byte.
            if (absolute <= byte.MaxValue)
            {
                if (positive)
                    writer.WriteByte((byte)CompressionLevels.Level1Positive);
                else
                    writer.WriteByte((byte)CompressionLevels.Level1Negative);

                writer.WriteByte((byte)absolute);
            }
            //Fits in a ushort
            else if (value <= ushort.MaxValue)
            {
                if (positive)
                    writer.WriteByte((byte)CompressionLevels.Level2Positive);
                else
                    writer.WriteByte((byte)CompressionLevels.Level2Negative);

                writer.WriteUInt16((ushort)absolute);
            }
            //Cannot compress.
            else
            {
                writer.WriteByte((byte)CompressionLevels.None);
                writer.WriteInt32(value);
            }
        }
        /// <summary>
        /// Writes a compressed float to a writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        public static void WriteCompressedFloat(this PooledNetworkWriter writer, float value)
        {
            int absolute = Mathf.Abs(Mathf.RoundToInt(value * 100f));
            bool positive = (value >= 0);
            //Fits in a byte.
            if (absolute <= byte.MaxValue)
            {
                if (positive)
                    writer.WriteByte((byte)CompressionLevels.Level1Positive);
                else
                    writer.WriteByte((byte)CompressionLevels.Level1Negative);

                writer.WriteByte((byte)absolute);
            }
            //Fits in a ushort
            else if (value <= ushort.MaxValue)
            {
                if (positive)
                    writer.WriteByte((byte)CompressionLevels.Level2Positive);
                else
                    writer.WriteByte((byte)CompressionLevels.Level2Negative);

                writer.WriteUInt16((ushort)absolute);
            }
            //Cannot compress.
            else
            {
                writer.WriteByte((byte)CompressionLevels.None);
                writer.WriteSingle(value);
            }
        }
        #endregion


        #region ReadCompressed.
        /// <summary>
        /// Writes a compressed uint to a writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        public static uint ReadCompressedUInt(this PooledNetworkReader reader)
        {
            CompressionLevels cl = (CompressionLevels)reader.ReadByte();

            //Compressed into byte.
            if (cl == CompressionLevels.Level1Positive)
                return reader.ReadByte();
            //Compressed into ushort.
            else if (cl == CompressionLevels.Level2Positive)
                return reader.ReadUInt16();
            //Not compressed.
            else
                return reader.ReadUInt32();
        }
        /// <summary>
        /// Writes a compressed int to a writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        public static int ReadCompressedInt(this PooledNetworkReader reader)
        {
            CompressionLevels cl = (CompressionLevels)reader.ReadByte();

            //Compressed into positive byte.
            if (cl == CompressionLevels.Level1Positive)
                return reader.ReadByte();
            //Compressed into negative byte.
            else if (cl == CompressionLevels.Level1Negative)
                return -reader.ReadByte();
            //Compressed into positive short.
            if (cl == CompressionLevels.Level2Positive)
                return reader.ReadUInt16();
            //Compressed into negative short.
            else if (cl == CompressionLevels.Level2Negative)
                return -reader.ReadUInt16();
            //Not compressed.
            else
                return reader.ReadInt32();
        }

        /// <summary>
        /// Writes a compressed float to a writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        public static float ReadCompressedFloat(this PooledNetworkReader reader)
        {
            CompressionLevels cl = (CompressionLevels)reader.ReadByte();
            float divisor = 100f;

            //Compressed into positive byte.
            if (cl == CompressionLevels.Level1Positive)
                return reader.ReadByte() / divisor;
            //Compressed into negative byte.
            else if (cl == CompressionLevels.Level1Negative)
                return -reader.ReadByte() / divisor;
            //Compressed into positive short.
            if (cl == CompressionLevels.Level2Positive)
                return reader.ReadUInt16() / divisor;
            //Compressed into negative short.
            else if (cl == CompressionLevels.Level2Negative)
                return -reader.ReadUInt16() / divisor;
            //Not compressed.
            else
                return reader.ReadSingle();
        }
        #endregion


    }

}