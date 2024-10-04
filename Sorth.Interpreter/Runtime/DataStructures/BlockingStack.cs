


namespace Sorth.Interpreter.Runtime.DataStructures
{


    public class BlockingStack
    {
        private object ItemLock;
        private Stack<Value> ValueStack;
        private ManualResetEventSlim Condition;

        public BlockingStack()
        {
            ItemLock = new object();
            ValueStack = new Stack<Value>();
            Condition = new ManualResetEventSlim();
        }

        public int Count
        {
            get
            {
                lock (ItemLock)
                {
                    return ValueStack.Count;
                }
            }
        }

        public void Push(Value value)
        {
            lock (ItemLock)
            {
                ValueStack.Push(value);
                Condition.Set();
            }
        }

        public Value Pop()
        {
            while (true)
            {
                Condition.Wait();

                lock (ItemLock)
                {
                    if (ValueStack.Count > 0)
                    {
                        var item = ValueStack.Pop();

                        if (ValueStack.Count == 0)
                        {
                            Condition.Reset();
                        }

                        return item;
                    }
                }
            }

            /*lock (ItemLock)
            {
                while (ValueStack.Count == 0)
                {
                    Condition.Reset();
                    Monitor.Wait(ItemLock);
                }

                return ValueStack.Pop();
            }*/
        }
    }


}
