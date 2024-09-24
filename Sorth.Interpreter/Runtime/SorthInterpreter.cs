
using System.Runtime.CompilerServices;
using Sorth.Interpreter.Language.Code;
using Sorth.Interpreter.Language.Source;
using Sorth.Interpreter.Runtime.DataStructures;



namespace Sorth.Interpreter.Runtime
{

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


    public class SorthInterpreter : ContextualData
    {
        private List<string> SearchPaths;

        private Dictionary Dictionary;

        public SortedDictionary<string, Word> Words
        {
            get
            {
                return Dictionary.CombinedWords;
            }
        }

        private ContextualList<WordHandlerInfo> Handlers;
        public ContextualList<Value> Variables { get; private set; }

        public Stack<Value> Stack { get; private set; }

        private Stack<Constructor> Constructors;

        public Location? CurrentLocation { get; private set; }

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

        public SorthInterpreter()
        {
            SearchPaths = new List<string>();

            Dictionary = new Dictionary();
            Handlers = new ContextualList<WordHandlerInfo>();
            Variables = new ContextualList<Value>();

            Stack = new Stack<Value>();

            Constructors = new Stack<Constructor>();
        }

        public void Push(Value value)
        {
            Stack.Push(value);
        }

        public Value Pop()
        {
            if (Stack.Count == 0)
            {
                ThrowError("Stack underflow.");
            }

            return Stack.Pop();
        }

        public void ThrowError(string message)
        {
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
                    handler_index = index
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

        public void ExecuteWord(long index)
        {
            var handler_info = Handlers[(int)index];

            if (!CurrentLocation.HasValue)
            {
                CurrentLocation = handler_info.location;
            }

            try
            {
                handler_info.handler(this);
                CurrentLocation = null;
            }
            catch
            {
                CurrentLocation = null;
                throw;
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

            Constructors.Push(constructor);

            try
            {
                Constructors.Peek().CompileTokenList(this);

                if (constructor.Top != null)
                {
                    var top_level = SorthILGenerator.GenerateHandler(this,
                                                                     name,
                                                                     constructor.Top.ByteCode);

                    top_level(this);
                }

                Constructors.Pop();
            }
            catch
            {
                Constructors.Pop();
                throw;
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
