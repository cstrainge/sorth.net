
using Sorth.Interpreter.Language.Source;



namespace Sorth.Interpreter.Runtime.DataStructures
{

    public struct Word
    {
        public bool is_immediate;
        public bool is_scripted;
        public bool is_hidden;

        public string description;
        public string signature;

        public int handler_index;

        public Location? location;

        public Word()
        {
            is_immediate = false;
            is_scripted = false;
            is_hidden = false;

            description = "";
            signature = "";

            handler_index = -1;

            location = null;
        }

        public Word(Location location)
        : this()
        {
            this.location = location;
        }
    }

    public class Dictionary : ContextualData
    {
        List<Dictionary<string, Word>> stack;

        public Dictionary()
        {
            stack = new List<Dictionary<string, Word>>();
            MarkContext();
        }

        public Dictionary(Dictionary original)
        {
            stack = new List<Dictionary<string, Word>>();
            MarkContext();

            foreach (var sub_dictionary in original.stack)
            {
                foreach (var entry in sub_dictionary)
                {
                    stack[0][entry.Key] = entry.Value;
                }
            }
        }

        public void Insert(string name, Word word)
        {
            var top = stack[stack.Count - 1];
            top[name] = word;
        }

        public ( bool, Word? ) Find(string name)
        {
            for (var i = stack.Count - 1; i >= 0; --i)
            {
                Word word;

                if (stack[i].TryGetValue(name, out word))
                {
                    return ( true, word );
                }
            }

            return ( false, null );
        }

        public SortedDictionary<string, Word> CombinedWords
        {
            get
            {
                var words = new SortedDictionary<string, Word>();

                foreach (var dictionary in stack)
                {
                    foreach (var entry in dictionary)
                    {
                        words.TryAdd(entry.Key, entry.Value);
                    }
                }

                return words;
            }
        }

        public void MarkContext()
        {
            stack.Add(new Dictionary<string, Word>());
        }

        public void ReleaseContext()
        {
            if (stack.Count == 0)
            {
                throw new ScriptError("Releasing empty context.");
            }

            stack.RemoveAt(stack.Count - 1);
        }
    }

}
