
namespace Framework.ETcp
{
    public interface ILog
    {
        void Debug(string content);
        void Info(string content);
        void Warn(string content);
        void Error(string content);

        void Debugf(string format, params object[] args);
        void Infof(string format, params object[] args);
        void Warnf(string format, params object[] args);
        void Errorf(string format, params object[] args);
    }
}

