
using System.Collections.Generic;
using Sorth.Interpreter.Runtime;



namespace Sorth.Interpreter.Runtime.DataStructures
{

    public class ContextualList<value_type> : ContextualData
    {
        private class SubList
        {
            public List<value_type> items;
            public int start_index;

            public SubList(int start_index)
            {
                items = new List<value_type>();
                this.start_index = start_index;
            }
        }

        List<SubList> stack;

        public ContextualList()
        {
            stack = new List<SubList>();
            MarkContext();
        }

        public int Count()
        {
            return stack[stack.Count - 1].start_index + stack[stack.Count - 1].items.Count;
        }

        public int Insert(value_type value)
        {
            stack[stack.Count - 1].items.Add(value);
            return Count() - 1;
        }

        public value_type this[int index]
        {
            get
            {
                if (index <= Count() - 1)
                {
                    for (int i = stack.Count() - 1 ; i >= 0; --i)
                    {
                        if (index >= stack[i].start_index)
                        {
                            index -= stack[i].start_index;
                            return stack[i].items[index];
                        }
                    }
                }

                throw new ScriptError($"Index {index} not found.");
            }

            set
            {
                if (index <= Count() - 1)
                {
                    for (int i = stack.Count() - 1; i >= 0; --i)
                    {
                        if (index >= stack[i].start_index)
                        {
                            index -= stack[i].start_index;
                            stack[i].items[index] = value;
                            return;
                        }
                    }
                }

                throw new ScriptError($"Index {index} not found.");
            }
        }

        public void MarkContext()
        {
            int start_index = 0;

            if (stack.Count > 0)
            {
                start_index = Count();
            }

            stack.Add(new SubList(start_index));
        }

        public void ReleaseContext()
        {
            if (stack.Count > 0)
            {
                stack.RemoveAt(stack.Count - 1);
            }
            else
            {
                throw new ScriptError("No context to release.");
            }
        }
    }

}
