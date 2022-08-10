using System;

namespace Framework.ETcp
{
    public interface INet
    {
        bool Init(ILog log);

        void PushEvent(IEvent evt);

        bool Connect(string host, UInt32 port, ISession session);      

        bool Run(int count);
    }
}


