using System;
using Google.Protobuf;

namespace Framework.ETcp
{
    public abstract class Session : ISession
    {
        public void SetConnection(IConnection _conn)
        {
            conn = _conn;
        }

        public UInt64 GetSessID()
        {
            return session_id;
        }

        public void SetSessID(UInt64 _session_id)
        {
            session_id = _session_id;
        }

        public ICoder GetCoder()
        {
            return coder;
        }

        public void SetCoder(ICoder _coder)
        {
            coder = _coder;
        }

        public bool IsListenType()
        {
            return (session_type == SessionType.LISTEN) ? true : false;
        }

        public bool IsConnectType()
        {
            return (session_type == SessionType.CONNECT) ? true : false;
        }

        public void SetConnectType()
        {
            session_type = SessionType.CONNECT;
        }

        public void SetListenType()
        {
            session_type = SessionType.LISTEN;
        }

        public void SetSessionFactory(ISessionfactory factory)
        {
            session_factory = factory;
        }

        public ISessionfactory GetSessionFactory()
        {
            return session_factory;
        }

        public bool AsyncSendMsg(UInt32 msg_id, byte[] datas)
        {
            byte[] all_datas;
            if (coder.PackMsg(msg_id, datas, out all_datas) == false)
            {
                GlobalVar.ELog.Errorf("[Session] SesssionID={0} SendMsg MsgId={1} PackMsg Error", GetSessID(), msg_id);
                return false;
            }

            if (all_datas.Length >= coder.GetPackageMaxLen())
            {
                GlobalVar.ELog.Errorf("[Session] SesssionID={0} SendMsg MsgId={1} Out Range PackMsg Max Len", GetSessID(), msg_id);                
                return false;
            }
            GlobalVar.ELog.Debugf("[Net][Session] SendMsg MsgId={0},Datas={1}", msg_id, all_datas);
            conn.AsyncSend(all_datas);
            return true;
        }

        public bool AsyncSendProtoMsg(UInt32 msg_id, IMessage message)
        {
            if (message != null)
            {
                //需优化ToByteArray性能   
                //https://www.yht7.com/news/36972
                byte[] datas = message.ToByteArray();
                AsyncSendMsg(msg_id, datas);
            }
            else
            {
                AsyncSendMsg(msg_id, null);
            }
            return true;
        }

        public abstract void OnHandler(UInt32 msg_id, byte[] datas);

        public void Terminate()
        {
            terminate_flag = true;
            if (conn != null)
            {
                conn.Terminate();
            }
        }

        public bool GetTerminate()
        {
            return terminate_flag;
        }

        public abstract void OnEstablish();

        public abstract void OnTerminate();

        private UInt64 session_id = 0;
        private IConnection conn = null;
        private ICoder coder = null;
        private SessionType session_type = SessionType.LISTEN;
        private ISessionfactory session_factory = null;
        private bool terminate_flag = false;
    }
}