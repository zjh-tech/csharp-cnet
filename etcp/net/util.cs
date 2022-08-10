using System;

namespace Framework.ETcp
{
    public class Util
    {
        public static long GetMillSecond()
        {
            return DateTime.Now.Ticks / 10000;
        }
    }
}
