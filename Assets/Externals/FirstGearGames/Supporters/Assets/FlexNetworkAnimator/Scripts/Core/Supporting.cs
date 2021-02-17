using Mirror;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkAnimators
{


    [System.Serializable]
    public struct AnimatorUpdate
    {
        public byte ComponentIndex;
        public uint NetworkIdentity;
        public byte[] Data;

        public AnimatorUpdate(byte componentIndex, uint networkIdentity, byte[] data)
        {
            ComponentIndex = componentIndex;
            NetworkIdentity = networkIdentity;
            Data = data;
        }
    }


    public static class FNASerializer
    {
        public static void WriteAnimatorUpdate(this NetworkWriter writer, AnimatorUpdate au)
        {
            //Component index.
            writer.WriteByte(au.ComponentIndex);

            //Write compressed network identity.
            //byte.
            if (au.NetworkIdentity <= byte.MaxValue)
            {
                writer.WriteByte(1);
                writer.WriteByte((byte)au.NetworkIdentity);
            }
            //ushort.
            else if (au.NetworkIdentity <= ushort.MaxValue)
            {
                writer.WriteByte(2);
                writer.WriteUInt16((ushort)au.NetworkIdentity);
            }
            //Full value.
            else
            {
                writer.WriteByte(4);
                writer.WriteUInt32(au.NetworkIdentity);
            }

            //Animation data.
            //Compress data length.
            if (au.Data.Length <= byte.MaxValue)
            {
                writer.WriteByte(1);
                writer.WriteByte((byte)au.Data.Length);
            }
            else if (au.Data.Length <= ushort.MaxValue)
            {
                writer.WriteByte(2);
                writer.WriteUInt16((ushort)au.Data.Length);
            }
            else
            {
                writer.WriteByte(4);
                writer.WriteInt32(au.Data.Length);
            }
            if (au.Data.Length > 0)
                writer.WriteBytes(au.Data, 0, au.Data.Length);
        }

        public static AnimatorUpdate ReadAnimatorUpdate(this NetworkReader reader)
        {
            AnimatorUpdate au = new AnimatorUpdate();

            //Component index.
            au.ComponentIndex = reader.ReadByte();

            //Network identity.
            byte netIdCompression = reader.ReadByte();
            if (netIdCompression == 1)
                au.NetworkIdentity = reader.ReadByte();
            else if (netIdCompression == 2)
                au.NetworkIdentity = reader.ReadUInt16();
            else
                au.NetworkIdentity = reader.ReadUInt32();

            //Animation data.
            byte dataLengthCompression = reader.ReadByte();
            int dataLength;
            if (dataLengthCompression == 1)
                dataLength = reader.ReadByte();
            else if (dataLengthCompression == 2)
                dataLength = reader.ReadUInt16();
            else
                dataLength = reader.ReadInt32();

            if (dataLength > 0)
                au.Data = reader.ReadBytes(dataLength);
            else
                au.Data = new byte[0];

            return au;
        }



    }


}