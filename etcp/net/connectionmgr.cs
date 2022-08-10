using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Framework.ETcp
{
    //确认是否存在多线程问题
    public class Connectionmgr : IConnectionMgr
    {
        public IConnection Create(INet net, Socket socket, ISession session)
        {
            ++next_id;
            Connection conn = new Connection(next_id, net, socket, session);
            dict[next_id] = conn;
            return conn;
        }
        public void Remove(UInt64 id)
        {
            if (dict.ContainsKey(id))
            {
                dict.Remove(id);
            }
        }

        public int GetConnCount()
        {
            return dict.Count;
        }

        public void Update()
        { 
            foreach(KeyValuePair<UInt64, IConnection> kv in dict)
            {
                kv.Value.Update();
            }
        }

        private UInt64 next_id = 0;
        private Dictionary<UInt64, IConnection> dict = new Dictionary<ulong, IConnection>();
    }
}
