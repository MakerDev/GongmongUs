#if MIRROR
using Mirror;
#elif MIRAGE
using Mirage;
#endif
using FirstGearGames.Utilities.Networks;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms
{
    public static class Serialization
    {
        /// <summary>
        /// Serializes a TransformData into the returned writer.
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="index"></param>
        public static PooledNetworkWriter SerializeTransformData(List<TransformData> datas, int index)
        {
            using (PooledNetworkWriter writer = NetworkWriterPool.GetWriter())
            {
                SerializeTransformData(writer, datas[index]);
                return writer;
            }
        }

        /// <summary>
        /// Serializes a TransformData into the returned writer.
        /// </summary>
        /// <param name="datas"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SerializeTransformData(NetworkWriter writer, TransformData data)
        {
            //SyncProperties.
            SyncProperties sp = (SyncProperties)data.SyncProperties;
            writer.WriteByte(data.SyncProperties);

            //NetworkIdentity.
            //Get compression level for netIdentity.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Id1))
                writer.WriteByte((byte)data.NetworkIdentity);
            else if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Id2))
                writer.WriteUInt16((ushort)data.NetworkIdentity);
            else
                writer.WriteUInt32(data.NetworkIdentity);
            //ComponentIndex.
            writer.WriteByte(data.ComponentIndex);

            //Position.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Position))
            {
                if (EnumContains.SyncPropertiesContains(sp, SyncProperties.CompressSmall))
                    Compressions.WriteCompressedVector3(writer, data.Position);
                else
                    writer.WriteVector3(data.Position);
            }
            //Rotation.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Rotation))
                writer.WriteUInt32(Quaternions.Compress(data.Rotation));
            //Scale.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Scale))
            {
                if (EnumContains.SyncPropertiesContains(sp, SyncProperties.CompressSmall))
                    Compressions.WriteCompressedVector3(writer, data.Scale);
                else
                    writer.WriteVector3(data.Scale);
            }

            //If attached.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Attached))
                WriteAttached(writer, data.Attached);
        }

        /// <summary>
        /// Deserializes a TransformData from data.
        /// </summary>
        public static void DeserializeTransformData(ref int readPosition, ref ArraySegment<byte> packetData, ref TransformData transformData)
        {
            using (PooledNetworkReader reader = NetworkReaderPool.GetReader(packetData))
            {
                DeserializeTransformData(reader, ref readPosition, ref packetData, ref transformData);    
            }
        }

        /// <summary>
        /// Deserializes a TransformData from data.
        /// </summary>
        public static void DeserializeTransformData(NetworkReader reader, ref int readPosition, ref ArraySegment<byte> packetData, ref TransformData transformData)
        {
            reader.Position = readPosition;

            //Sync properties.
            SyncProperties sp = (SyncProperties)reader.ReadByte();
            transformData.SyncProperties = (byte)sp;

            //NetworkIdentity.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Id1))
                transformData.NetworkIdentity = reader.ReadByte();
            else if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Id2))
                transformData.NetworkIdentity = reader.ReadUInt16();
            else
                transformData.NetworkIdentity = reader.ReadUInt32();
            //ComponentIndex.
            transformData.ComponentIndex = reader.ReadByte();

            //Position.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Position))
            {
                if (EnumContains.SyncPropertiesContains(sp, SyncProperties.CompressSmall))
                    transformData.Position = Compressions.ReadCompressedVector3(reader);
                else
                    transformData.Position = reader.ReadVector3();
            }
            //Rotation.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Rotation))
                transformData.Rotation = Quaternions.Decompress(reader.ReadUInt32());
            //scale.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Scale))
            {
                if (EnumContains.SyncPropertiesContains(sp, SyncProperties.CompressSmall))
                    transformData.Scale = Compressions.ReadCompressedVector3(reader);
                else
                    transformData.Scale = reader.ReadVector3();
            }
            //If attached.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Attached))
                transformData.Attached = ReadAttached(reader);

            //Data should be marked as set given we just populated it.
            transformData.IsSet = true;

            //Set readPosition to new reader position.
            readPosition = reader.Position;
        }


        /// <summary>
        /// Writes an attached to writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="attached"></param>
        public static void WriteAttached(NetworkWriter writer, AttachedData attached)
        {
            uint netId;
            sbyte componentIndex;
            if (!attached.IsSet)
            {
                netId = 0;
                componentIndex = -1;
            }
            else
            {
                netId = attached.NetId;
                componentIndex = attached.AttachedTargetIndex;
            }

            writer.WriteUInt32(netId);
            writer.WriteSByte(componentIndex);
        }

        /// <summary>
        /// Returns an AttachedData.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static AttachedData ReadAttached(NetworkReader reader)
        {
            AttachedData ad = new AttachedData();
            ad.SetData(reader.ReadUInt32(), reader.ReadSByte());
            return ad;
        }

    }


}