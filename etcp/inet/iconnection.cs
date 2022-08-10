using System;
using System.Net.Sockets;

namespace Framework.ETcp
{
    public interface IConnection
    {
        UInt64 GetConnID();

        ISession GetSession();

        void DoAsyncReceive();

        void AsyncSend(byte[] datas);

        void Terminate();

        void Update();
    }

    public interface IConnectionMgr
    {
        IConnection Create(INet net, Socket socket, ISession session);

        void Remove(UInt64 id);

        int GetConnCount();
    }
}
