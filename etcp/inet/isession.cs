using System;

namespace Framework.ETcp
{
    public enum SessionType
    {
        CONNECT = 1,
        LISTEN = 2,
    }

    public interface ISession
    {
        void SetConnection(IConnection conn);

        UInt64 GetSessID();

        void SetSessID(UInt64 session_id);

        void OnEstablish();

        void OnTerminate();        

        ICoder GetCoder();

        void OnHandler(UInt32 msg_id, byte[] datas);

        void SetCoder(ICoder coder);

        bool IsListenType();

        bool IsConnectType();

        void SetConnectType();

        void SetListenType();

        void SetSessionFactory(ISessionfactory factory);

        ISessionfactory GetSessionFactory();

        bool AsyncSendMsg(UInt32 msg_id, byte[] datas);
        
        void Terminate();
    }

    public interface ISessionfactory
    {
        ISession CreateSession();
        void AddSession(ISession sess);
        void RemoveSession(UInt64 session_id);
    }
}
