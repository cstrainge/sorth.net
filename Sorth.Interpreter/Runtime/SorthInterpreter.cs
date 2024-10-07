
using System.Runtime.CompilerServices;
using Sorth.Interpreter.Language.Code;
using Sorth.Interpreter.Language.Source;
using Sorth.Interpreter.Runtime.DataStructures;



namespace Sorth.Interpreter.Runtime
{


    using CallItem = ( string Name, Location Location );


    public delegate void WordHandler(SorthInterpreter interpreter);


    public readonly struct WordHandlerInfo
    {
        public readonly string name;
        public readonly WordHandler handler;
        public readonly Location location;

        public WordHandlerInfo(string new_name, WordHandler new_handler, Location new_location)
        {
            name = new_name;
            handler = new_handler;
            location = new_location;
        }
    }


    public struct SubThreadInfo
    {
        public Word Word;
        public Thread WordThread;

        public BlockingQueue Inputs;
        public BlockingQueue Outputs;

        public SubThreadInfo(Word word, Thread word_thread)
        {
            Word = word;
            WordThread = word_thread;
            Inputs = new BlockingQueue();
            Outputs = new BlockingQueue();
        }
    }


    public class SorthInterpreter : ContextualData
    {
        private SorthInterpreter? ParentInterpreter;

        private List<string> SearchPaths;

        private Dictionary Dictionary;

        public SortedDictionary<string, Word> Words
        {
            get
            {
                return Dictionary.CombinedWords;
            }
        }

        private Dictionary<int, SubThreadInfo> SubThreads;
        private object SubThreadLock;

        public ContextualList<WordHandlerInfo> Handlers { get; private set; }
        public ContextualList<Value> Variables { get; private set; }

        public Stack<Value> Stack { get; private set; }
        public int MaxDepth { get; private set; }

        private Stack<CallItem> CallStack;

        private Stack<Constructor> Constructors;

        public Location? CurrentLocation { get; set; }

        public Constructor Constructor
        {
            get
            {
                if (Constructors.Count == 0)
                {
                    ThrowError("No code constructor available.");
                }

                return Constructors.Peek();
            }
        }

        public List<SubThreadInfo> Threads
        {
            get
            {
                if (ParentInterpreter != null)
                {
                    return ParentInterpreter.Threads;
                }

                lock (SubThreadLock)
                {
                    var info_list = new List<SubThreadInfo>(SubThreads.Count);

                    foreach (var info in SubThreads)
                    {
                        info_list.Add(info.Value);
                    }

                    return info_list;
                }
            }
        }

        public SorthInterpreter()
        {
            ParentInterpreter = null;

            SearchPaths = new List<string>();

            Dictionary = new Dictionary();
            Handlers = new ContextualList<WordHandlerInfo>();
            Variables = new ContextualList<Value>();

            SubThreads = new Dictionary<int, SubThreadInfo>();
            SubThreadLock = new object();

            Stack = new Stack<Value>(50);
            MaxDepth = 0;

            CallStack = new Stack<CallItem>(100);

            Constructors = new Stack<Constructor>();
        }

        public SorthInterpreter(SorthInterpreter parent)
        {
            ParentInterpreter = parent;

            SearchPaths = new List<string>(parent.SearchPaths);

            Dictionary = new Dictionary(parent.Dictionary);
            Handlers = new ContextualList<WordHandlerInfo>(parent.Handlers);
            Variables = new ContextualList<Value>(parent.Variables);

            SubThreads = new Dictionary<int, SubThreadInfo>();
            SubThreadLock = new object();

            Stack = new Stack<Value>(50);
            MaxDepth = 0;

            CallStack = new Stack<CallItem>(100);

            Constructors = new Stack<Constructor>();

            MarkContext();
        }

        public void Push(Value value)
        {
            Stack.Push(value);

            if (Stack.Count > MaxDepth)
            {
                MaxDepth = Stack.Count;
            }
        }

        public Value Pop()
        {
            if (Stack.Count == 0)
            {
                ThrowError("Stack underflow.");
            }

            return Stack.Pop();
        }

        public int ThreadInputCount(int id)
        {
            if (ParentInterpreter != null)
            {
                return ParentInterpreter.ThreadInputCount(id);
            }

            return GetThreadInfo(id).Inputs.Count;
        }

        public void ThreadPushInput(int id, Value value)
        {
            if (ParentInterpreter != null)
            {
                ParentInterpreter.ThreadPushInput(id, value);
            }
            else
            {
                GetThreadInfo(id).Inputs.Push(value);
            }
        }

        public Value ThreadPopInput(int id)
        {
            if (ParentInterpreter != null)
            {
                return ParentInterpreter.ThreadPopInput(id);
            }

            return GetThreadInfo(id).Inputs.Pop();
        }

        public int ThreadOutputCount(int id)
        {
            if (ParentInterpreter != null)
            {
                return ParentInterpreter.ThreadOutputCount(id);
            }

            return GetThreadInfo(id).Outputs.Count;
        }

        public void ThreadPushOutput(int id, Value value)
        {
            if (ParentInterpreter != null)
            {
                ParentInterpreter.ThreadPushOutput(id, value);
            }
            else
            {
                GetThreadInfo(id).Outputs.Push(value);
            }
        }

        public Value ThreadPopOutput(int id)
        {
            if (ParentInterpreter != null)
            {
                return ParentInterpreter.ThreadPopOutput(id);
            }

            var info = GetThreadInfo(id);
            var value = info.Outputs.Pop();

            if (   (!info.WordThread.IsAlive)
                && (info.Outputs.Count == 0))
            {
                info.WordThread.Join();

                lock (SubThreadLock)
                {
                    if (SubThreads.ContainsKey(id))
                    {
                        SubThreads.Remove(id);
                    }
                }
            }

            return value;
        }

        public Value Pick(int index)
        {
            var temp_list = new List<Value>(Stack);

            var item = temp_list[index];
            temp_list.RemoveAt(index);

            Stack = new Stack<Value>(temp_list.AsEnumerable().Reverse());

            return item;
        }

        public void PushTo(int index)
        {
            var value = Stack.Pop();
            var temp_list = new List<Value>(Stack);

            temp_list.Insert(index, value);

            Stack = new Stack<Value>(temp_list.AsEnumerable().Reverse());
        }

        public void ThrowError(string message)
        {
            if (CallStack.Count > 0)
            {
                message += "\n\nCall stack:\n";

                foreach (var item in CallStack)
                {
                    message += $"  {item.Location} -- {item.Name}\n";
                }
            }

            if (CurrentLocation.HasValue)
            {
                throw new ScriptError(CurrentLocation.Value, message);
            }
            else
            {
                throw new ScriptError(message);
            }
        }

        public void AddWord(string word_name, WordHandler handler, Location location,
                            string description = "",
                            string signature = "",
                            bool is_immediate = false,
                            bool is_hidden = false,
                            bool is_scripted = false)
        {
            var handler_info = new WordHandlerInfo(word_name, handler, location);
            var index = Handlers.Insert(handler_info);
            var new_word = new Word
                {
                    is_immediate = is_immediate,
                    is_hidden = is_hidden,
                    is_scripted = is_scripted,
                    description = description,
                    signature = signature,
                    handler_index = index,
                    location = location
                };

            Dictionary.Insert(word_name, new_word);
        }

        public void AddWord(string word_name, WordHandler handler,
                            string description = "",
                            string signature = "",
                            bool is_immediate = false,
                            bool is_hidden = false,
                            bool is_scripted = false,
                            [CallerFilePath] string file_path = "",
                            [CallerLineNumber] int line_number = 0)
        {
            var location = new Location(file_path, line_number, 1);

            AddWord(word_name, handler, location, description, signature, is_immediate,
                    is_hidden, is_scripted);
        }

        public ( bool, Word? ) FindWord(string word)
        {
            return Dictionary.Find(word);
        }

        public ( bool, Word?, string ) FindWord(long index)
        {
            if ((int)index >= Handlers.Count())
            {
                return ( false, null, "" );
            }

            var handler_info = Handlers[(int)index];
            var ( found, word ) = FindWord(handler_info.name);

            return ( found, word, handler_info.name );
        }

        private void AppendNewThread(SubThreadInfo info)
        {
            if (ParentInterpreter != null)
            {
                ParentInterpreter.AppendNewThread(info);
            }
            else
            {
                lock (SubThreadLock)
                {
                    SubThreads.Add(info.WordThread.ManagedThreadId, info);
                }
            }
        }

        private void RemoveThread(int id)
        {
            if (ParentInterpreter != null)
            {
                ParentInterpreter.RemoveThread(id);
            }
            else
            {
                // Get a copy of the info struct.
                var info = GetThreadInfo(id);

                // Only clean up the thread structure if it's output queue is empty.  This way we
                // can make sure receiving threads get their messages.
                if (info.Outputs.Count == 0)
                {
                    info.WordThread.Join();

                    lock (SubThreadLock)
                    {
                        if (SubThreads.ContainsKey(id))
                        {
                            SubThreads.Remove(id);
                        }
                    }
                }
            }
        }

        private SubThreadInfo GetThreadInfo(int id)
        {
            if (ParentInterpreter != null)
            {
                return ParentInterpreter.GetThreadInfo(id);
            }

            lock (SubThreadLock)
            {
                if (!SubThreads.ContainsKey(id))
                {
                    ThrowError($"Thread id, {id}, not found.");
                }

                return SubThreads[id];
            }
        }

        public int ExecuteWordThreaded(Word word)
        {
            // If this interpreter has a parent, request it to spawn the thread so that they are
            // all tracked in the same place.
            if (ParentInterpreter != null)
            {
                return ParentInterpreter.ExecuteWordThreaded(word);
            }

            // Clone the interpreter to run in the thread.
            var child = new SorthInterpreter(this);

            var word_thread = new Thread(() =>
                {
                    try
                    {
                        // Execute the requested word.  Then on return clean up after ourselves.
                        child.ExecuteWord(word);
                        child.RemoveThread(Thread.CurrentThread.ManagedThreadId);
                    }
                    catch
                    {
                        // TODO: Report this in the thread info?
                    }
                });

            // Start and register the thread.
            word_thread.Start();

            AppendNewThread(new SubThreadInfo(word, word_thread));

            // Finally return the new id to the caller.
            return word_thread.ManagedThreadId;
        }

        public void ExecuteWord(long index)
        {
            var handler_info = Handlers[(int)index];

            if (!CurrentLocation.HasValue)
            {
                CurrentLocation = handler_info.location;
            }

            bool pushed = false;

            try
            {
                CallStack.Push(( handler_info.name, handler_info.location ));
                pushed = true;

                handler_info.handler(this);
            }
            finally
            {
                if (pushed)
                {
                    CallStack.Pop();
                }

                CurrentLocation = null;
            }
        }

        public void ExecuteWord(string word_name)
        {
            var ( found, word_info ) = FindWord(word_name);

            if (found && (word_info != null))
            {
                ExecuteWord(word_info.Value.handler_index);
            }
            else
            {
                ThrowError($"Word, {word_name}, not found.");
            }
        }

        public void ExecuteWord(Word word)
        {
            ExecuteWord(word.handler_index);
        }

        public void ExecuteWord(Location location, Word word)
        {
            CurrentLocation = location;
            ExecuteWord(word.handler_index);
        }

        public void AddSearchPath(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
            }

            if (Directory.Exists(path))
            {
                SearchPaths.Add(path);
            }
        }

        public string FindFile(string path)
        {
            if (   Path.IsPathRooted(path)
                && Path.Exists(path))
            {
                return path;
            }
            else
            {
                for (int i = SearchPaths.Count - 1; i >= 0; --i)
                {
                    string full_path = Path.GetFullPath(Path.Combine(SearchPaths[i], path));

                    if (File.Exists(full_path))
                    {
                        return full_path;
                    }
                }
            }

            ThrowError($"File, {path}, not found.");

            return "";
        }

        public void ProcessSourceFile(string path)
        {
            var buffer = new SourceBuffer(FindFile(path));
            ProcessSource(Path.GetFileNameWithoutExtension(path), buffer);
        }

        public void ProcessSource(string source)
        {
            var buffer = new SourceBuffer("<repl>", source);
            ProcessSource("Repl", buffer);
        }

        private void ProcessSource(string name, SourceBuffer buffer)
        {
            var tokens = Tokenizer.Tokenize(buffer);
            var constructor = new Constructor(tokens);

            try
            {
                Constructors.Push(constructor);
                Constructors.Peek().CompileTokenList(this);

                if (constructor.Top != null)
                {
                    var top_level = SorthILGenerator.GenerateHandler(this,
                                                                     name,
                                                                     constructor.Top.ByteCode);

                    top_level(this);
                }
            }
            finally
            {
                Constructors.Pop();
            }
        }

        public void Reset()
        {
            ReleaseContext();
            Stack.Clear();

            MarkContext();
        }

        public void MarkContext()
        {
            Dictionary.MarkContext();
            Handlers.MarkContext();
            Variables.MarkContext();
        }

        public void ReleaseContext()
        {
            Dictionary.ReleaseContext();
            Handlers.ReleaseContext();
            Variables.ReleaseContext();
        }
    }

}
