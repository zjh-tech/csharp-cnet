using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace Framework.ETcp
{
    public class ConnectParas
    {
        public ConnectParas(Socket _socket, string _host, UInt32 _port, IConnection _conn)
        {
            ClientSocket = _socket;
            Host = _host;
            Port = _port;
            Conn = _conn;
        }

        public Socket ClientSocket;
        public string Host;
        public UInt32 Port;
        public IConnection Conn;
    }

    public class Net : INet
    {
        public bool Init(ILog log)
        {
            GlobalVar.ELog = log;
            event_queue = new ConcurrentQueue<IEvent>();            
            return true;
        }

        public bool Run(int count)
        {
            GlobalVar.GConnectionMgr.Update();            

            if (event_queue.Count == 0)
            {
                return false;
            }

            bool busy = false;
            if (event_queue.Count < count)
            {
                count = event_queue.Count;
                busy = true; 
            }

            for (int i = 0; i < count; ++i)
            {
                IEvent evt = null;                
                if (event_queue.TryDequeue(out evt))
                {
                    continue;
                }
                if (evt == null)
                {
                    continue;
                }
                evt.ProcessMsg();               
            }

            return busy;
        }

        public bool Connect(string host, UInt32 port, ISession session)
        {            
            if (session == null)
            {
                GlobalVar.ELog.Errorf("[Net] Host={0},Port={1} Connect Session Error", host, port);
                return false;
            }         

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if(socket == null)
            {
                GlobalVar.ELog.Errorf("[Net] Host={0},Port={1} Connect Socket Error", host, port);
                return false; 
            }

            IConnection conn = GlobalVar.GConnectionMgr.Create(this, socket, session);
            if (conn == null)
            {
                GlobalVar.ELog.Errorf("[Net] Host={0},Port={1} Connect Connection Error", host, port);
                return false;
            }

            IPEndPoint connect_endpoint = new IPEndPoint(IPAddress.Parse(host), (int)port);
            if(connect_endpoint == null)
            {
                GlobalVar.ELog.Errorf("[Net] Host={0},Port={1} Connect IPEndPoint Error", host, port);
                return false; 
            }

            ConnectParas conn_paras = new ConnectParas(socket, host, port, conn);
            if(conn_paras == null)
            {
                GlobalVar.ELog.Errorf("[Net] Host={0},Port={1} Connect ConnectParas Error", host, port);
                return false; 
            }
                        
            try
            {
                socket.BeginConnect(connect_endpoint, on_begin_connect, conn_paras);
            }
            catch (System.Exception ex)
            {
                GlobalVar.ELog.Errorf("[Net] Host={0},Port={1} BeginConnect Error={2}", host, port, ex.ToString());
                return false;
            }            
            return true;
        }
        
        private void on_begin_connect(IAsyncResult result)
        {            
            ConnectParas conn_paras = (ConnectParas)result.AsyncState;
            if (conn_paras == null)
            {
                GlobalVar.ELog.Error("[Net] on_begin_connect ConnectParas = null");
                return;
            }

            if (conn_paras.ClientSocket == null)
            {
                GlobalVar.ELog.Error("[Net] on_begin_connect ConnectParas.ClientSocket = null");
                return;
            }
            
            try
            {
                conn_paras.ClientSocket.EndConnect(result);
            }
            catch (System.Exception ex)
            {
                GlobalVar.ELog.Errorf("[Net] Host={0},Port={1} EndConnect Error={2}", conn_paras.Host, conn_paras.Port, ex.ToString());
                return;
            }            

            conn_paras.Conn.DoAsyncReceive();
        }

        public void PushEvent(IEvent evt)
        {
            event_queue.Enqueue(evt);
        }
        
        private ConcurrentQueue<IEvent> event_queue = null;

        public static Net Instance = new Net();
    }
}
