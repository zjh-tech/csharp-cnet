using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace Framework.ETcp
{
    public class Connection : IConnection
    {
        public Connection(UInt64 _conn_id, INet _net, Socket _socket, ISession _sess)
        {
            conn_id = _conn_id;
            net = _net;
            socket = _socket;
            session = _sess;
        }

        public UInt64 GetConnID()
        {
            return conn_id;
        }
        public ISession GetSession()
        {
            return session;
        }

        private void set_conn_state(ConnectionState _state)
        {
            int new_state = (int)_state;
            System.Threading.Interlocked.Exchange(ref state, new_state);
        }

        private bool is_conn_state(ConnectionState _state)
        {
            long tempState = System.Threading.Interlocked.Read(ref state);
            return (tempState == (long)_state) ? true : false;
        }

        private bool get_and_set_state(ConnectionState _old_state, ConnectionState _new_state)
        {
            long old_state = (long)_old_state;
            long new_state = (long)_new_state;
            if (System.Threading.Interlocked.CompareExchange(ref state, old_state, new_state) == old_state)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #region Send
        public void AsyncSend(byte[] datas)
        {
            if (datas == null)
            {
                return;
            }

            if (socket == null)
            {
                return;
            }

            if (is_conn_state(ConnectionState.ESTABLISHED) == false)
            {
                GlobalVar.ELog.Errorf("[Net] [Connection] ConnID={0} AsyncSend Not Is Established State", conn_id);
                return;
            }

            lock (send_queue)
            {
                bool freeflag = (send_queue.Count == 0) ? true : false;
                send_queue.Enqueue(datas);
                if (freeflag)
                {
                    send_size = 0;

                    try
                    {
                        socket.BeginSend(datas, 0, datas.Length, SocketFlags.None, OnSend, null);
                    }
                    catch (System.Exception ex)
                    {
                        GlobalVar.ELog.Errorf("[Net] [Connection] ConnID={0} BeginSend First Error {1}", conn_id, ex.ToString());
                        close(false);
                    }
                }
            }
        }

        private void OnSend(IAsyncResult result)
        {
            if (result == null || socket == null)
            {
                return;
            }

            int bytes = 0;

            try
            {
                bytes = socket.EndSend(result);
            }
            catch (System.Exception ex)
            {
                GlobalVar.ELog.Errorf("[Net] [Connection] ConnID={0} EndSend Error {1}", conn_id, ex.ToString());
                close(false);
                return;
            }

            lock (send_queue)
            {
                if (send_queue.Count == 0)
                {
                    return;
                }

                byte[] cur = send_queue.Peek();
                if (cur == null)
                {
                    return;
                }
                send_size += bytes;
                if (send_size == cur.Length)
                {
                    cur = null;
                    cur = send_queue.Dequeue();
                    if (cur != null)
                    {
                        cur = null;
                    }

                    byte[] next = (send_queue.Count == 0) ? null : send_queue.Peek();
                    if (next == null)
                    {
                        return;
                    }

                    send_size = 0;
                    try
                    {
                        socket.BeginSend(next, 0, next.Length, SocketFlags.None, OnSend, null);
                    }
                    catch (System.Exception ex)
                    {
                        GlobalVar.ELog.Errorf("[Net] [Connection] ConnID={0} BeginSend Next Error {1}", conn_id, ex.ToString());
                        close(false);
                        return;
                    }
                }
                else if (send_size < cur.Length)
                {
                    GlobalVar.ELog.Errorf("[Net] [Connection] ConnID={0} BeginSend Current Package", conn_id);
                    int sendpos = cur.Length - send_size;

                    try
                    {
                        socket.BeginSend(cur, send_size, sendpos, SocketFlags.None, OnSend, null);
                    }
                    catch (System.Exception ex)
                    {
                        GlobalVar.ELog.Errorf("[Net] [Connection] ConnID={0} BeginSend  Continue Current Package Error {1}", conn_id, ex.ToString());
                        close(false);
                        return;
                    }
                }
                else
                {
                    GlobalVar.ELog.Errorf("[Net] [Connection] ConnID={0} EndSend Package Error", conn_id);
                }
            }
        }
        #endregion

        #region Recv
        public void DoAsyncReceive()
        {            
            if (socket == null)
            {
                GlobalVar.ELog.Errorf("[Net] [Connection] ConnID={0} StartRecvThread Socket Null", conn_id);
                return;
            }

            session.SetConnection(this);
            set_conn_state(ConnectionState.ESTABLISHED);

            Event evt = new Event(EventType.ConnEstablishType, this, null);
            net.PushEvent(evt);

            socket.SendBufferSize = NetDef.MSG_SEND_BUFF_SIZE;
            socket.ReceiveBufferSize = NetDef.MSG_RECV_BUFF_SIZE;
            loop_buffer.ResetBuffer();
            loop_buffer.ResetLastBuffer();

            try
            {
                socket.BeginReceive(loop_buffer.Buffer, 0, loop_buffer.Capacity, SocketFlags.None, on_begin_receive, null);
            }
            catch (System.Exception ex)
            {
                GlobalVar.ELog.Errorf("[Net] [Connection] ConnID={0} BeginReceive First  Error {1}", conn_id, ex.ToString());
                close(false);
                return;
            }

            GlobalVar.ELog.Infof("[Net] [Connection] ConnID={0} StartRecvThread Socket Success", conn_id);
        }

        private void on_begin_receive(IAsyncResult result)
        {
            if (socket == null)
            {
                return;
            }

            int bytes = 0;
            try
            {
                bytes = socket.EndReceive(result);
            }
            catch (System.Exception ex)
            {
                GlobalVar.ELog.Errorf("[Net] [Connection] ConnID={0} EndReceive Error {1}", conn_id, ex.ToString());
                close(false);
                return;
            }

            if (is_conn_state(ConnectionState.ESTABLISHED) == false)
            {
                GlobalVar.ELog.Warnf("[Net] [Connection] ConnID={0} EndReceive Not Established State", conn_id);
                return;
            }

            if (bytes == 0)
            {
                GlobalVar.ELog.Infof("[Net] [Connection] ConnID={0} Close Socket", conn_id);
                close(false);
                return;
            }
            else if (bytes > 0)
            {
                process_receive_buffer(bytes);
                loop_buffer.ResetBuffer();
                try
                {
                    socket.BeginReceive(loop_buffer.Buffer, 0, loop_buffer.Capacity, SocketFlags.None, on_begin_receive, null);
                }
                catch (System.Exception ex)
                {
                    GlobalVar.ELog.Errorf("[Net] [Connection] ConnID={0} BeginReceive Next  Error {1}", conn_id, ex.ToString());
                    close(false);
                    return;
                }
            }
        }

        private void process_one_packet(byte[] bytes, int offset, UInt32 body_len)
        {
            ICoder coder = session.GetCoder();

            byte[] bodyBytes = new byte[body_len];
            Array.Copy(bytes, offset, bodyBytes, 0, body_len);
                       
            byte[] out_bodyBytes;
            coder.UnpackMsg(bodyBytes,out out_bodyBytes);

            Event evt = new Event(EventType.ConnRecvMsgType, this, out_bodyBytes);
            net.PushEvent(evt);
        }

        private void process_receive_buffer(int bytes)
        {
            for (int offset = 0; offset < bytes;)
            {
                ICoder coder = session.GetCoder();
                UInt32 header_len = coder.GetHeaderLen();
                UInt32 body_len = 0;
                UInt32 total_len = 0;

                if (loop_buffer.LastFlag)
                {
                    //优化: 先拷贝包头,解析出包体长度后再拷包体
                    //LastBuffer Socket缓冲区
                    Array.Copy(loop_buffer.Buffer, 0, loop_buffer.LastBuffer, loop_buffer.LastSize, bytes);
                    int old_lastsize = loop_buffer.LastSize;
                    loop_buffer.LastSize += bytes;

                    if (loop_buffer.LastSize < header_len)
                    {
                        GlobalVar.ELog.Debugf("[Net] [Connection] ConnID={0} ProcessReceiveBuffer Lastbuffer Not Enough Header Length", conn_id);
                        return;
                    }

                    bool error_flag = coder.GetBodyLen(loop_buffer.LastBuffer,0, out body_len);
                    if (error_flag == false)
                    {
                        GlobalVar.ELog.Errorf("[Net] [Connection] ConnID={0} ProcessReceiveBuffer GetBodyLen Error", conn_id);
                        return;
                    }

                    total_len = header_len + body_len;
                    if (total_len > NetDef.PACKAGE_DEFAULT_MAX_SIZE)
                    {
                        GlobalVar.ELog.Errorf("[Net] [Connection] ConnID={0} ProcessReceiveBuffer Lastbuffer Greater Than Package Length", conn_id);
                        return;
                    }

                    if (total_len > loop_buffer.LastBuffer.Length)
                    {
                        GlobalVar.ELog.Debugf("[Net] [Connection] ConnID={0} ProcessReceiveBuffer Lastbuffer Not Enough One Package", conn_id);
                        return;
                    }

                    process_one_packet(loop_buffer.LastBuffer, (int)header_len, body_len);

                    int diff_len = (int)(total_len - old_lastsize);
                    offset += diff_len;
                    loop_buffer.AddReadIndex(diff_len);
                    loop_buffer.ResetLastBuffer();

                    continue;
                }

                if ((offset + header_len) > bytes)
                {
                    GlobalVar.ELog.Info("[Net] [Connection] ConnID={0} ProcessReceiveBuffer Buffer Last Package Not Enough Header");
                    loop_buffer.LastFlag = true;
                    loop_buffer.LastSize = bytes - offset;
                    Array.Copy(loop_buffer.Buffer, offset, loop_buffer.LastBuffer, 0, loop_buffer.LastSize);
                    return;
                }

                bool body_error_flag = coder.GetBodyLen(loop_buffer.Buffer,offset, out body_len);
                if (body_error_flag == false)
                {
                    GlobalVar.ELog.Errorf("[Net] [Connection] ConnID={0} ProcessReceiveBuffer GetBodyLen Error", conn_id);
                    return;
                }

                total_len = header_len + body_len;

                if (total_len == (bytes - offset))
                {
                    process_one_packet(loop_buffer.Buffer, offset + (int)header_len, body_len);
                    return;
                }
                else if (total_len > (bytes - offset))
                {
                    GlobalVar.ELog.Debugf("[Net] [Connection] ConnID={0} ProcessReceiveBuffer Buffer Last Package Not Enough Package", conn_id);
                    loop_buffer.LastFlag = true;
                    loop_buffer.LastSize = bytes - offset;
                    Array.Copy(loop_buffer.Buffer, offset, loop_buffer.LastBuffer, 0, loop_buffer.LastSize);
                    return;
                }
                else if (total_len < (bytes - offset))
                {
                    process_one_packet(loop_buffer.Buffer, offset + (int)header_len, body_len);
                    offset += (int)total_len;
                    loop_buffer.AddReadIndex((int)total_len);
                }
            }
        }

        #endregion

        #region Close
        public void Terminate()
        {
            close(true);
        }
        private void close(bool terminate)
        {
            if (get_and_set_state(ConnectionState.CLOSED, ConnectionState.CLOSED))
            {
                return;
            }

            Event evt = new Event(EventType.ConnCloseType, this, null);
            net.PushEvent(evt);

            if (terminate)
            {
                check_send_finish_time = Util.GetMillSecond();
                check_send_interval_time = 1000;
                check_send_timeout = 1000 * 60;               
            }

            terminate_flag = terminate;
        }

        public void Update()
        { 
            if (check_send_timeout == 0 || check_send_finish_time == 0 || check_send_interval_time == 0)
            {
                return;
            }

            long now = Util.GetMillSecond();
            if (now >= check_send_timeout)
            {
                on_close(terminate_flag);
            }
            else
            {
                bool flag = false;
                lock (send_queue)
                {
                    if (send_queue.Count == 0)
                    {
                        flag = true;
                    }
                }
                
                if (flag)
                {
                    on_close(terminate_flag);
                }
            }         
        }

        private void on_close(bool terminate)
        {
            if (terminate)
            {
                GlobalVar.ELog.Infof("Net [Connection] ConnID={0} Active Closed", conn_id);
            }
            else
            {
                GlobalVar.ELog.Infof("Net [Connection] ConnID={0} Passive Closed", conn_id);
            }

            if (socket != null)
            {
                socket.Close();
                socket = null;
            }
        }
        #endregion

        private Socket socket = null;
        private Queue<byte[]> send_queue = new Queue<byte[]>();
        private int send_size = 0;
        private Queue<byte[]> recv_queue = new Queue<byte[]>();
        private LoopBuffer loop_buffer = new LoopBuffer(NetDef.PACKAGE_DEFAULT_MAX_SIZE);


        private UInt64 conn_id = 0;
        private INet net = null;
        private ISession session = null;
        private long state = (long)ConnectionState.CLOSED;

        private bool terminate_flag = false; 
        private long check_send_finish_time = 0;
        private long check_send_interval_time = 0;
        private long check_send_timeout = 0;       
    }
}
