using System;
using System.IO;
using System.Net;

namespace Framework.ETcp
{ 
    public class MsgHeader
    {        
        public UInt32 BodyLen = 0;
    }

    public class Coder : ICoder
    {
        public UInt32 GetHeaderLen()
        {
            return PackageHeaderLen;
        }

        public bool GetBodyLen(byte[] datas, out UInt32 body_len)
        {
            MemoryStream memstream = new MemoryStream(datas);
            BinaryReader reader = new BinaryReader(memstream);            
            byte[] body_len_bytes = reader.ReadBytes(sizeof(UInt32));
            Array.Reverse(body_len_bytes);
            msg_header.BodyLen = BitConverter.ToUInt32(body_len_bytes);
            body_len = msg_header.BodyLen;
            return true;
        }

        public bool PackMsg(UInt32 msgId, byte[] datas, out byte[] out_datas)
        {
            MemoryStream memstream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memstream);
            UInt32 bodyLen = 0;
            writer.Write(bodyLen);

            writer.Write(IPAddress.NetworkToHostOrder((int)msgId));

            if (datas != null)
            {
                writer.Write(datas);
            }

            bodyLen = (UInt32)memstream.Position - sizeof(UInt32);
            memstream.Position = 0;

            writer.Write(IPAddress.NetworkToHostOrder((int)bodyLen));

            out_datas = memstream.ToArray();
            return true;
        }

        public bool UnpackMsg(byte[] datas, out byte[] out_datas)
        {
            out_datas = datas;
            return true;
        }

        public void ProcessMsg(byte[] datas, ISession sess)
        {
            if (datas.Length < PackageMsgIDLen)
            {
                GlobalVar.ELog.Errorf("[Session] SesssionID=%v ProcessMsg Len Error", sess.GetSessID());
                return;
            }

            MemoryStream memstream = new MemoryStream(datas);
            BinaryReader reader = new BinaryReader(memstream);

            byte[] msg_id_bytes = reader.ReadBytes((int)PackageMsgIDLen);
            Array.Reverse(msg_id_bytes);
            UInt32 msg_id = BitConverter.ToUInt32(msg_id_bytes);

            sess.OnHandler(msg_id, reader.ReadBytes(datas.Length - (int)PackageMsgIDLen));
        }

        public UInt32 GetPackageMaxLen()
        {
            return (UInt32)NetDef.PACKAGE_DEFAULT_MAX_SIZE;
        }

        public ICoder Clone()
        {
            return new Coder();
        }

        private MsgHeader msg_header = new MsgHeader();
        private const UInt32 PackageHeaderLen = 4;      
        private const UInt32 PackageMsgIDLen = 4;
    }

}