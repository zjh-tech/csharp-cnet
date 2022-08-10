namespace Framework.ETcp
{
    public class Event : IEvent
    {
        private EventType evt_type;
        private IConnection conn;
        private object datas;

        public Event(EventType _evt_type, IConnection _conn, object _datas)
        {
            evt_type = _evt_type;
            conn = _conn;
            datas = _datas;
        }

        public EventType GetEventType()
        {
            return evt_type;
        }

        public IConnection GetConn()
        {
            return conn;
        }

        public object GetDatas()
        {
            return datas;
        }

        public bool ProcessMsg()
        {
            if (conn == null)
            {
                GlobalVar.ELog.Error("[Net] Run Conn Is Nil");
                return false;
            }

            ISession session = conn.GetSession();
            if (session == null)
            {
                GlobalVar.ELog.Error("[Net] Run Session Is Nil");
                return false;
            }

            if (evt_type == EventType.ConnEstablishType) {
                //session.SetConnection(t.conn)
                session.OnEstablish();
             }
            else if (evt_type == EventType.ConnRecvMsgType) {
                session.GetCoder().ProcessMsg((byte[])datas, session);     
             }
             else if (evt_type == EventType.ConnCloseType) {
                session.SetConnection(null);
                GlobalVar.GConnectionMgr.Remove(conn.GetConnID());
                session.OnTerminate();      
            }
            return true;
        }
    }
}
