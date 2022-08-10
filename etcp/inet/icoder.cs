using System;

namespace Framework.ETcp
{
    public interface ICoder
    {
        UInt32 GetHeaderLen();

        bool GetBodyLen(byte[] datas, out UInt32 body_len);

        bool PackMsg(UInt32 msgId, byte[] datas, out byte[] out_datas);

        bool UnpackMsg(byte[] datas, out byte[] out_datas);

        void ProcessMsg(byte[] datas, ISession sess);

        UInt32 GetPackageMaxLen();
    }
}
