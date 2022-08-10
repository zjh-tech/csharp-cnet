using System;
using System.Collections.Generic;
using Google.Protobuf;

namespace Framework.ETcp
{
    public interface IClientSessionHandler
    {
        bool Init();

        void OnConnect(ClientSession sess);

        void OnDisconnect(ClientSession sess);

        void OnHandlerMsg(ClientSession sess, uint msg_id, byte[] datas);

        void OnBeatHeartError(ClientSession sess);
    }

    public class ClientSession : Session
    {
        public override void OnEstablish()
        {
            last_beat_heart_time = Util.GetMillSecond();
            var factory = GetSessionFactory();
            factory.AddSession(this);
            last_beat_heart_time = Util.GetMillSecond();
            handler.OnConnect(this);
        }

        public override void OnHandler(uint msg_id, byte[] datas)
        {
            last_beat_heart_time = Util.GetMillSecond();
            handler.OnHandlerMsg(this, msg_id, datas);
        }

        public override void OnTerminate()
        {
            var factory = GetSessionFactory();
            factory.RemoveSession(GetSessID());
            GlobalVar.ELog.Infof("[ClientSession] OnTerminate SessionID={0}", GetSessID());
            handler.OnDisconnect(this);
        }


        public void Update()
        {
            if (GetTerminate())
            {
                return;
            }

            long now = Util.GetMillSecond();
            if ((last_beat_heart_time + beat_heart_max_time) < now)
            {
                GlobalVar.ELog.Errorf("[ClientSession] SessionID={0}  BeatHeart Exception", GetSessID());
                handler.OnBeatHeartError(this);
                Terminate();
            }
        }

        public void SetHandler(IClientSessionHandler _handler)
        {
            handler = _handler;
        }

        public void SendBytes(UInt32 msgID, byte[] datas)
        {
            AsyncSendMsg(msgID, datas);
        }

        public void SendProtoMsg(UInt32 msgID, IMessage message)
        {
            AsyncSendProtoMsg(msgID, message);
        }

        private IClientSessionHandler handler = null;
        private long last_beat_heart_time = 0;
        private long beat_heart_max_time = 1000 * 60 * 3;
    }


    public class ClientSessionMgr : ISessionfactory
    {
        public UInt64 Connect(string _host, UInt32 _port, IClientSessionHandler _handler, ICoder _coder)
        {
            _handler.Init();

            ClientSession session = (ClientSession)CreateSession();
            session.SetHandler(_handler);
            session.SetCoder(_coder);
            session.SetConnectType();

            ConnectCache cache = new ConnectCache(session.GetSessID(), _host, _port, Util.GetMillSecond() + mgr_beat_heart_max_time);
            connect_cache_dict[session.GetSessID()] = cache;
            GlobalVar.ELog.Infof("[ClientSessionMgr] ConnectCache Add SessionID={0},Host={1} Port={2}", session.GetSessID(), _host, _port);

            Net.Instance.Connect(_host, _port, session);
            return session.GetSessID();
        }

        public bool IsInConnectCache(UInt64 session_id)
        {
            return connect_cache_dict.ContainsKey(session_id);
        }

        public bool IsExistSession(UInt64 session_id)
        {
            return session_dict.ContainsKey(session_id);
        }
        
        public void AddSession(ISession sess)
        {
            UInt64 session_id = sess.GetSessID();
            session_dict[session_id] = (ClientSession)sess;

            GlobalVar.ELog.Infof("[ClientSessionMgr] AddSession SessionID={0}", session_id);

            if (connect_cache_dict.ContainsKey(session_id))
            {
                GlobalVar.ELog.Infof("[ClientSessionMgr] AddSession Triggle ConnectCache Del SessionID={0}", session_id);
                connect_cache_dict.Remove(session_id);
            }
        }

        public ISession CreateSession()
        {
            ++session_id;
            ClientSession session = new ClientSession();
            session.SetSessID(session_id);
            session.SetCoder(coder);
            session.SetHandler(handler);
            session.SetSessionFactory(this);
            GlobalVar.ELog.Infof("[ClientSessionMgr] CreateSession={0}", session.GetSessID());
            return session;
        }

        public void RemoveSession(UInt64 session_id)
        {
            if (session_dict.ContainsKey(session_id))
            {
                GlobalVar.ELog.Infof("[ClientSessionMgr] RemoveSession SessionID={0} ", session_id);
                session_dict.Remove(session_id);
            }
        }

        public void Update()
        {
            long now = Util.GetMillSecond();
            List<UInt64> remove_list = new List<UInt64>();
            foreach (KeyValuePair<UInt64, ConnectCache> kv in connect_cache_dict)
            {
                if (kv.Value.ConnectTick < now)
                {
                    remove_list.Add(kv.Key);
                }
            }

            if (remove_list.Count != 0)
            {
                int remove_count = remove_list.Count;
                for (int i = 0; i < remove_count; ++i)
                {
                    GlobalVar.ELog.Infof("[ClientSessionMgr] Timeout Triggle ConnectCache Del SessionID={0}", remove_list[i]);
                    connect_cache_dict.Remove(remove_list[i]);
                }
            }

            foreach (KeyValuePair<UInt64, ClientSession> kv in session_dict)
            {
                if (kv.Value != null)
                {
                    kv.Value.Update();
                }
            }
        }

        private UInt64 session_id = 0;
        private Dictionary<UInt64, ClientSession> session_dict = new Dictionary<UInt64, ClientSession>();
        private IClientSessionHandler handler = null;
        private ICoder coder = null;
        private Dictionary<UInt64, ConnectCache> connect_cache_dict = new Dictionary<UInt64, ConnectCache>();
        private long mgr_beat_heart_max_time = 1000 * 10;

        private class ConnectCache
        {
            public ConnectCache(UInt64 session_id, string host, UInt32 port, Int64 connect_tick)
            {
                SessionID = session_id;
                Host = host;
                Port = port;
                ConnectTick = connect_tick;
            }

            public UInt64 SessionID;
            public string Host;
            public UInt32 Port;
            public Int64 ConnectTick;
        }
    }
}