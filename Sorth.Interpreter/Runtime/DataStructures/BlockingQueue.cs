


namespace Sorth.Interpreter.Runtime.DataStructures
{


    public class BlockingQueue
    {
        private object ItemLock;
        private Queue<Value> ValueQueue;
        //private ManualResetEventSlim Condition;

        public BlockingQueue()
        {
            ItemLock = new object();
            ValueQueue = new Queue<Value>();
            //Condition = new ManualResetEventSlim();
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
                //Condition.Set();
                Monitor.PulseAll(ItemLock);
            }
        }

        public Value Pop()
        {
            /*while (true)
            {
                Condition.Wait();

                lock (ItemLock)
                {
                    if (ValueQueue.Count > 0)
                    {
                        var item = ValueQueue.Dequeue();

                        if (ValueQueue.Count == 0)
                        {
                            Condition.Reset();
                        }

                        return item;
                    }
                }
            }*/
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
