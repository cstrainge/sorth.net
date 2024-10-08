


namespace Sorth.Interpreter.Runtime.DataStructures
{


    public class BlockingQueue
    {
        private object ItemLock;
        private Queue<Value> ValueQueue;

        public BlockingQueue()
        {
            ItemLock = new object();
            ValueQueue = new Queue<Value>();
        }

        public int Count
        {
            get
            {
                lock (ItemLock)
                {
                    return ValueQueue.Count;
                }
            }
        }

        public void Push(Value value)
        {
            lock (ItemLock)
            {
                ValueQueue.Enqueue(value);
                Monitor.PulseAll(ItemLock);
            }
        }

        public Value Pop()
        {
            lock (ItemLock)
            {
                while (ValueQueue.Count == 0)
                {
                    Monitor.Wait(ItemLock);
                }
                return ValueQueue.Dequeue();
            }
        }
    }


}
