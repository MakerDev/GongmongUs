using Mirror;
using FirstGearGames.Utilities.Networks;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms
{
    public static class Serialization
    {
        /// <summary>
        /// Various lengths for types. This is simply to ensure I don't mess up my lengths.
        /// </summary>
        public const byte VECTOR3_BYTES = 12;
        public const byte QUATERNION_BYTES = 4;
        public const byte INT_BYTES = 4;
        public const byte SINGLE_BYTES = 4;
        public const byte SHORT_BYTES = 2;
        public const byte BYTE_BYTES = 1;
        /// <summary>
        /// Maximum absolute value a float may be to be compressed.
        /// </summary>
        private const float MAX_FLOAT_COMPRESSION_VALUE = 654f;

        /// <summary>
        /// Serializes a TransformSyncData into OutBuffer.
        /// </summary>
        /// <param name="bufferWritePosition"></param>
        /// <param name="datas"></param>
        /// <param name="index"></param>
        public static void SerializeTransformSyncData(byte[] writerBuffer, ref int bufferWritePosition, List<TransformSyncData> datas, int index)
        {
            //NetworkWriter writer = new NetworkWriter();
            using (PooledNetworkWriter writer = NetworkWriterPool.GetWriter())
            {
                //SyncProperties.
                SyncProperties sp = (SyncProperties)datas[index].SyncProperties;
                writer.WriteByte(datas[index].SyncProperties);

                //NetworkIdentity.
                //Get compression level for netIdentity.
                if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Id1))
                    writer.WriteByte((byte)datas[index].NetworkIdentity);
                else if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Id2))
                    writer.WriteUInt16((ushort)datas[index].NetworkIdentity);
                else
                    writer.WriteUInt32(datas[index].NetworkIdentity);
                //ComponentIndex.
                writer.WriteByte(datas[index].ComponentIndex);

                //Position.
                if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Position))
                {
                    if (EnumContains.SyncPropertiesContains(sp, SyncProperties.CompressSmall))
                        WriteCompressedVector3(writer, datas[index].Position);
                    else
                        writer.WriteVector3(datas[index].Position);
                }
                //Rotation.
                if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Rotation))
                    writer.WriteUInt32(Quaternions.CompressQuaternion(datas[index].Rotation));
                //Scale.
                if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Scale))
                {
                    if (EnumContains.SyncPropertiesContains(sp, SyncProperties.CompressSmall))
                        WriteCompressedVector3(writer, datas[index].Scale);
                    else
                        writer.WriteVector3(datas[index].Scale);
                }

                //If attached.
                if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Attached))
                    WriteAttached(writer, datas[index].Attached);

                Array.Copy(writer.ToArraySegment().Array, 0, writerBuffer, bufferWritePosition, writer.Length);
                bufferWritePosition += writer.Length;
            }
        }

        /// <summary>
        /// Deserializes a TransformSyncData from data.
        /// </summary>
        /// <param name="readPosition"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static TransformSyncData DeserializeTransformSyncData(ref int readPosition, ArraySegment<byte> data)
        {
            using (PooledNetworkReader reader = NetworkReaderPool.GetReader(data))
            {
                reader.Position = readPosition;
                TransformSyncData syncData = new TransformSyncData();

                //Sync properties.
                SyncProperties sp = (SyncProperties)reader.ReadByte();
                syncData.SyncProperties = (byte)sp;
                readPosition += BYTE_BYTES;

                //NetworkIdentity.
                if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Id1))
                {
                    syncData.NetworkIdentity = reader.ReadByte();
                    readPosition += BYTE_BYTES;
                }
                else if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Id2))
                {
                    syncData.NetworkIdentity = reader.ReadUInt16();
                    readPosition += SHORT_BYTES;
                }
                else
                {
                    syncData.NetworkIdentity = reader.ReadUInt32();
                    readPosition += INT_BYTES;
                }
                //ComponentIndex.
                syncData.ComponentIndex = reader.ReadByte();
                readPosition += BYTE_BYTES;

                //Position.
                if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Position))
                {
                    if (EnumContains.SyncPropertiesContains(sp, SyncProperties.CompressSmall))
                    {
                        syncData.Position = ReadCompressedVector3(ref readPosition, reader);
                    }
                    else
                    {
                        syncData.Position = reader.ReadVector3();
                        readPosition += VECTOR3_BYTES;
                    }
                }
                //Rotation.
                if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Rotation))
                {
                    syncData.Rotation = Quaternions.DecompressQuaternion(reader.ReadUInt32());
                    readPosition += QUATERNION_BYTES;
                }
                //scale.
                if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Scale))
                {
                    if (EnumContains.SyncPropertiesContains(sp, SyncProperties.CompressSmall))
                    {
                        syncData.Scale = ReadCompressedVector3(ref readPosition, reader);
                    }
                    else
                    {
                        syncData.Scale = reader.ReadVector3();
                        readPosition += VECTOR3_BYTES;
                    }
                }

                //If attached.
                if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Attached))
                    syncData.Attached = ReadAttached(ref readPosition, reader);

                return syncData;

            }
        }


        /// <summary>
        /// Writes an attached to writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="attached"></param>
        public static void WriteAttached(NetworkWriter writer, AttachedData? attached)
        {
            uint netId;
            sbyte componentIndex;
            if (attached == null)
            {
                netId = 0;
                componentIndex = -1;
            }
            else
            {
                netId = attached.Value.NetId;
                componentIndex = attached.Value.ComponentIndex;
            }

            writer.WriteUInt32(netId);
            writer.WriteSByte(componentIndex);
        }

        /// <summary>
        /// Returns an AttachedData.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static AttachedData ReadAttached(ref int readPosition, NetworkReader reader)
        {
            AttachedData ad = new AttachedData()
            {
                NetId = reader.ReadUInt32(),
                ComponentIndex = reader.ReadSByte()
            };

            readPosition += (INT_BYTES + BYTE_BYTES);
            return ad;
        }

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
        public static Vector3 ReadCompressedVector3(ref int readPosition, NetworkReader reader)
        {
            CompressedAxes ca = (CompressedAxes)reader.ReadByte();
            readPosition += BYTE_BYTES;
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
            {
                x = (reader.ReadUInt16() / 100f) * sign;
                readPosition += SHORT_BYTES;
            }
            else
            {
                x = reader.ReadSingle();
                readPosition += SINGLE_BYTES;
            }

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
            {
                y = (reader.ReadUInt16() / 100f) * sign;
                readPosition += SHORT_BYTES;
            }
            else
            {
                y = reader.ReadSingle();
                readPosition += SINGLE_BYTES;
            }

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
            {
                z = (reader.ReadUInt16() / 100f) * sign;
                readPosition += SHORT_BYTES;
            }
            else
            {
                z = reader.ReadSingle();
                readPosition += SINGLE_BYTES;
            }

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