namespace Framework.ETcp
{
    public enum EventType
    {
        ConnEstablishType = 1,
        ConnRecvMsgType = 2,
        ConnCloseType = 3,
    }

    public interface IEvent
    {
        EventType GetEventType();
        IConnection GetConn();
        bool ProcessMsg(); 
    }
}
