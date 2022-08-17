using System;
using System.IO;
using System.Net;
using System.Text;
using System.Security.Cryptography;


namespace Framework.ETcp
{
    public class ClientCoderDef 
    {
        public static int ClientNonEncryptType = 0;
        public static int ClientXorEncryptType = 1;              
    };

    public class ClientMsgHeader
    {
        public UInt32 BodyLen = 0;
    }


    public class ClientCoder : ICoder
    {        
        public void SetEncryptInfo(int encrypt_type, byte[] keyBytes)
        {
            this.encrypt_type = encrypt_type;
            key_bytes = keyBytes;
        }

        public UInt32 GetHeaderLen()
        {
            return PackageHeaderLen;
        }

        public bool GetBodyLen(byte[] datas, int offset, out UInt32 body_len)
        {
            MemoryStream memstream = new MemoryStream(datas,offset,datas.Length -offset);
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

            if (datas != null &&  datas.Length > 0)
            {
                if (encrypt_type == ClientCoderDef.ClientXorEncryptType)
                {
                    XorEncrypt(ref datas,key_bytes);
                    writer.Write(datas);
                } else if (encrypt_type == ClientCoderDef.ClientNonEncryptType)
                {
                    writer.Write(datas);
                }
                else
                {
                    GlobalVar.ELog.Errorf("[Session] MsgId=%v PackMsg EncryptType Error", msgId);
                    out_datas = null;
                    return false;
                }
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

            byte[] pbBytes = reader.ReadBytes(datas.Length - (int)PackageMsgIDLen);

            if (encrypt_type == ClientCoderDef.ClientXorEncryptType)
            {
                if (pbBytes != null && pbBytes.Length > 0)
                {
                    XorDecrypst(ref pbBytes, key_bytes);
                }
                sess.OnHandler(msg_id, pbBytes);                
            }
            else if (encrypt_type == ClientCoderDef.ClientNonEncryptType)
            {
                sess.OnHandler(msg_id, pbBytes);
            }
            else
            {
                GlobalVar.ELog.Errorf("[Session] SesssionID=%v ProcessMsg EncryptType Error", sess.GetSessID());
            }
        }

        public UInt32 GetPackageMaxLen()
        {
            return (UInt32)NetDef.PACKAGE_DEFAULT_MAX_SIZE;
        }

        public ICoder Clone()
        {
            ClientCoder coder = new ClientCoder();
            coder.SetEncryptInfo(encrypt_type, key_bytes);
            return coder;
        }

        private int encrypt_type = ClientCoderDef.ClientNonEncryptType;
        private ClientMsgHeader msg_header = new ClientMsgHeader();
        private const UInt32 PackageHeaderLen = 4;
        private const UInt32 PackageMsgIDLen = 4;
        private byte[] key_bytes = null;

        public void XorEncrypt(ref byte[] datas,byte[] key)
        {
            int dataLen = datas.Length;
            int keyLen = key.Length;
            for(int i =0;i < dataLen; i++)
            {
                if (i < keyLen)
                {
                    datas[i] ^= key[i];                    
                }                                
            }            
        }

        public void XorDecrypst(ref byte[] datas, byte[] key)
        {
            int dataLen = datas.Length;
            int keyLen = key.Length;
            for (int i = 0; i < dataLen; i++)
            {
                if (i < keyLen)
                {
                    datas[i] ^= key[i];
                }                
            }
        }
    }

}