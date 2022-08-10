using System;

namespace Framework.ETcp
{
    public class LoopBuffer
    {
        public LoopBuffer(int cap)
        {
            Capacity = cap;
            Buffer = new byte[Capacity];
            ReadIndex = 0;

            LastFlag = false;
            LastCapacity = Capacity * 2;
            LastBuffer = new byte[LastCapacity];
            LastSize = 0;
        }

        public void ResetBuffer()
        {
            Array.Clear(Buffer, 0, Capacity);
            ReadIndex = 0;
        }

        public void ResetLastBuffer()
        {
            Array.Clear(LastBuffer, 0, LastCapacity);
            LastFlag = false;
            LastSize = 0;
        }

        public void AddReadIndex(int count)
        {
            ReadIndex += count;
        }

        public void Dispose()
        {
            Buffer = null;
            LastBuffer = null;
        }

        public int ReadIndex;
        public int Capacity;
        public byte[] Buffer;


        public bool LastFlag;
        public int LastSize;
        public int LastCapacity;
        public byte[] LastBuffer;
    }
}
