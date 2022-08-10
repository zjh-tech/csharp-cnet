using System;

namespace Framework.ETcp
{
    public class IDDealer<T> where T : class
    {
        public IDDealer(UInt32 _min_msg_id, UInt32 _max_msg_id)
        {
            min_msg_id = _min_msg_id;
            max_msg_id = _max_msg_id;
            dealers = new T[max_msg_id - min_msg_id];
            for (int i = 0; i < (max_msg_id - min_msg_id); ++i)
            {
                dealers[i] = null;
            }
        }

        public bool Register(UInt32 msg_id, T t)
        {
            if (msg_id < min_msg_id)
            {
                return false;
            }

            if (msg_id > max_msg_id)
            {
                return false;
            }

            if (dealers[msg_id] != null)
            {
                return false;
            }

            dealers[msg_id] = t;
            return true;
        }

        public void UnRegister(UInt32 msg_id)
        {
            if (msg_id >= min_msg_id && min_msg_id < max_msg_id)
            {
                dealers[msg_id] = null;
            }
        }

        public T Find(UInt32 msg_id)
        {
            if (msg_id >= min_msg_id && min_msg_id < max_msg_id)
            {
                return dealers[msg_id];
            }

            return null;
        }

        private UInt32 min_msg_id;
        private UInt32 max_msg_id;
        private T[] dealers;
    }
}