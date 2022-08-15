using System; 
using Framework.ETcp;
using System.Threading;
using Pb;
using static System.Console;

public class ELog : ILog
{
    public void Debug(string content)
    {
        WriteLine(content);
    }
    
    public void Info(string content)
    {
        WriteLine(content);
    }
    public void Warn(string content)
    {
        WriteLine(content);
    }
    public void Error(string content)
    {
        WriteLine(content);
    }

    public void Debugf(string format, params object[] args)
    {
        WriteLine(string.Format(format, args));
    }
    public void Infof(string format, params object[] args)
    {
        WriteLine(string.Format(format, args));
    }
    public void Warnf(string format, params object[] args)
    {
        WriteLine(string.Format(format, args));
    }

    public void Errorf(string format, params object[] args)
    {
        WriteLine(string.Format(format, args));
    }
}

public delegate bool GatewaySessionFunc(ClientSession sess, byte[] datas);

public class GatewaySession : IClientSessionHandler
{    
    bool IClientSessionHandler.Init()
    {
        iddealer.Register((UInt32)C2SLogicMsgId.S2CHeartbeatResId, OnHandlerS2CHeartbeatRes);
        return true;
    }

    void IClientSessionHandler.OnBeatHeartError(ClientSession sess)
    {
        GlobalVar.ELog.Error("BeatHeartError");
    }

    void IClientSessionHandler.OnConnect(ClientSession sess)
    {
        GlobalVar.ELog.Info("OnEstablish");
        c2s_heartbeat_req req = new c2s_heartbeat_req();        
        sess.SendProtoMsg((uint)C2SLogicMsgId.C2SHeartbeatReqId, req);
    }

    void IClientSessionHandler.OnHandlerMsg(ClientSession sess, uint msg_id, byte[] datas)
    {
        var dealer = iddealer.Find(msg_id);
        if (dealer == null)
        {
            GlobalVar.ELog.Warnf("SDServerSession OnHandlerMsg Can Not Find MsgID = {0}", msg_id);
            return;
        }

        dealer(sess, datas);
    }

    void IClientSessionHandler.OnDisconnect(ClientSession sess)
    {
        GlobalVar.ELog.Info("OnDisconnect");
    }

    private IDDealer<GatewaySessionFunc> iddealer = new IDDealer<GatewaySessionFunc>((UInt32)C2SLogicMsgId.C2SBaseMinLogicMsgId, (UInt32)C2SLogicMsgId.C2SBaseMaxLogicMsgId);


    bool OnHandlerS2CHeartbeatRes(ClientSession sess, byte[] datas)
    {
        s2c_heartbeat_res ack = s2c_heartbeat_res.Parser.ParseFrom(datas);        

        c2s_heartbeat_req req = new c2s_heartbeat_req();        
        sess.SendProtoMsg((uint)C2SLogicMsgId.C2SHeartbeatReqId, req);

        return true;
    }
}


class Program
{
    static void Main(string[] args)
    {
        ILog log = new ELog();

        if (!Net.Instance.Init(log))
        {
            return;
        }

        ClientSessionMgr clientSessMgr = new ClientSessionMgr();
        string host = "192.168.97.134";
        UInt32 port = 1299;
        GatewaySession gateway = new GatewaySession();
        ClientCoder coder = new ClientCoder();
        byte[] xorkey = new byte[] { 0x0C, 0xF0, 0x2D, 0x7B, 0x39, 0x08, 0xFE, 0x21, 0xBB, 0x41, 0x58 };
        coder.SetEncryptInfo(ClientCoderDef.ClientXorEncryptType, xorkey);
        UInt64 gatewaySessionID = clientSessMgr.Connect(host, port, gateway, coder);

        bool busy = false;

        for (; ; )
        {
            if(!clientSessMgr.IsExistSession(gatewaySessionID))
            {
                if (!clientSessMgr.IsInConnectCache(gatewaySessionID))
                {
                    gatewaySessionID = 0;
                }
            }

            if (gatewaySessionID == 0)
            {
                GatewaySession newGateway = new GatewaySession();
                Coder newCoder = new Coder();
                gatewaySessionID = clientSessMgr.Connect(host, port, newGateway, newCoder);
                GlobalVar.ELog.Info("Reconnect");
            }

            clientSessMgr.Update();

            busy = Net.Instance.Run(100);            
            if (!busy)
            {
                Thread.Sleep(1);
            }            
        }                
    }
}

