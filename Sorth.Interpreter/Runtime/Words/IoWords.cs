
using System.Text;
using Sorth.Interpreter.Runtime.DataStructures;



namespace Sorth.Interpreter.Runtime.Words
{

    public class IoWords
    {
        private static long FileIndex = 4;
        private static Dictionary<long, FileStream> HandleMap = new Dictionary<long, FileStream>();


        private static FileStream PopFileSteam(SorthInterpreter interpreter)
        {
            var handle = interpreter.Pop().AsInteger(interpreter);
            FileStream? result = null;

            if (HandleMap.TryGetValue(handle, out var file_stream))
            {
                result = file_stream; 
            }
            else
            {
                interpreter.ThrowError($"Handle {handle} is not an open file.");
            }

            return result ?? throw new Exception("Internal error.");
        }


        private static void WordFileOpen(SorthInterpreter interpreter)
        {
            var flags = (FileAccess)interpreter.Pop().AsInteger(interpreter);
            var path = interpreter.Pop().AsString(interpreter);

            var file_stream = new FileStream(path, FileMode.OpenOrCreate, flags);

            HandleMap[FileIndex] = file_stream;
            interpreter.Push(Value.From(FileIndex));

            ++FileIndex;
        }

        private static void WordFileCreate(SorthInterpreter interpreter)
        {
            var flags = (FileAccess)interpreter.Pop().AsInteger(interpreter);
            var path = interpreter.Pop().AsString(interpreter);

            var file_stream = new FileStream(path, FileMode.Create, flags);

            HandleMap[FileIndex] = file_stream;
            interpreter.Push(Value.From(FileIndex));

            ++FileIndex;
        }

        private static void WordFileCreateTempFile(SorthInterpreter interpreter)
        {
            var flags = (FileAccess)interpreter.Pop().AsInteger(interpreter);

            var path = Path.GetTempFileName();
            var file_stream = new FileStream(path, FileMode.Create, flags);

            HandleMap[FileIndex] = file_stream;
            interpreter.Push(Value.From(FileIndex));

            ++FileIndex;
        }

        private static void WordFileClose(SorthInterpreter interpreter)
        {
            var handle = interpreter.Pop().AsInteger(interpreter);
            
            if (HandleMap.TryGetValue(handle, out var file_stream))
            {
                file_stream.Close();
                HandleMap.Remove(handle);
            }
            else
            {
                interpreter.ThrowError($"Handle {handle} is not an open file.");
            }
        }

        private static void WordFileDelete(SorthInterpreter interpreter)
        {
            var path = interpreter.Pop().AsString(interpreter);
            File.Delete(path);
        }

        private static void WordSocketConnect(SorthInterpreter interpreter)
        {
        }

        private static void WordFileSizeRead(SorthInterpreter interpreter)
        {
            var file_stream = PopFileSteam(interpreter);

            interpreter.Push(Value.From(file_stream.Length));
        }

        private static void WordFileExists(SorthInterpreter interpreter)
        {
            var path = interpreter.Pop().AsString(interpreter);

            interpreter.Push(Value.From(Path.Exists(path)));
        }

        private static void WordFileIsOpen(SorthInterpreter interpreter)
        {
            var handle = interpreter.Pop().AsInteger(interpreter);
            var result = false;

            if (HandleMap.TryGetValue(handle, out _))
            {
                result = true;
            }

            interpreter.Push(Value.From(result));
        }

        private static void WordFileIsEof(SorthInterpreter interpreter)
        {
            var file_stream = PopFileSteam(interpreter);
            var result = false;

            result = file_stream.Position == file_stream.Length;
            interpreter.Push(Value.From(result));
        }

        private static void WordFileRead(SorthInterpreter interpreter)
        {
        }

        private static void WordFileReadCharacter(SorthInterpreter interpreter)
        {
            var file_stream = PopFileSteam(interpreter);
            var buffer = new byte[1];

            file_stream.Read(buffer, 0, 1);
            interpreter.Push(Value.From(BitConverter.ToString(buffer, 0, 1)));
        }

        private static void WordFileReadString(SorthInterpreter interpreter)
        {
            var file_stream = PopFileSteam(interpreter);
            var size = (int)interpreter.Pop().AsInteger(interpreter);
            var buffer = new byte[size];

            file_stream.Read(buffer, 0, size);

            var string_value = Encoding.UTF8.GetString(buffer, 0, size);

            interpreter.Push(Value.From(string_value));
        }

        private static void WordFileWrite(SorthInterpreter interpreter)
        {
            var file_stream = PopFileSteam(interpreter);
            var string_value = interpreter.Pop().ToString() ?? "";

            var bytes = Encoding.UTF8.GetBytes(string_value);

            file_stream.Write(bytes, 0, bytes.Length);
        }

        private static void WordFileLineRead(SorthInterpreter interpreter)
        {
            var file_stream = PopFileSteam(interpreter);

            using (StreamReader reader = new StreamReader(file_stream))
            {
                var line = reader.ReadLine() ?? "";
                interpreter.Push(Value.From(line));
            }
        }

        private static void WordFileLineWrite(SorthInterpreter interpreter)
        {
            var file_stream = PopFileSteam(interpreter);
            var string_value = interpreter.Pop().AsString(interpreter) + "\n";

            var bytes = Encoding.UTF8.GetBytes(string_value);

            file_stream.Write(bytes, 0, bytes.Length);
        }


        public static void Register(SorthInterpreter interpreter)
        {
            interpreter.AddWord("file.open", WordFileOpen,
                        "Open an existing file and return a fd.",
                        "path flags -- fd");

            interpreter.AddWord("file.create", WordFileCreate,
                            "Create/open a file and return a fd.",
                            "path flags -- fd");

            interpreter.AddWord("file.create.tempfile", WordFileCreateTempFile,
                            "Create/open an unique temporary file and return it's fd.",
                            "flags -- path fd");

            interpreter.AddWord("file.close", WordFileClose,
                            "Take a fd and close it.",
                            "fd -- ");

            interpreter.AddWord("file.delete", WordFileDelete,
                            "Delete the specified file.",
                            "file_path -- ");


            interpreter.AddWord("socket.connect", WordSocketConnect,
                            "Connect to Unix domain socket at the given path.",
                            "path -- fd");


            interpreter.AddWord("file.size@", WordFileSizeRead,
                            "Return the size of a file represented by a fd.",
                            "fd -- size");


            interpreter.AddWord("file.exists?", WordFileExists,
                            "Does the file at the given path exist?",
                            "path -- bool");

            interpreter.AddWord("file.is_open?", WordFileIsOpen,
                            "Is the fd currently valid?",
                            "fd -- bool");

            interpreter.AddWord("file.is_eof?", WordFileIsEof,
                            "Is the file pointer at the end of the file?",
                            "fd -- bool");


            interpreter.AddWord("file.@", WordFileRead,
                            "Read from a given file.  (Unimplemented.)",
                            " -- ");

            interpreter.AddWord("file.char@", WordFileReadCharacter,
                            "Read a character from a given file.",
                            "fd -- character");

            interpreter.AddWord("file.string@", WordFileReadString,
                            "Read a a string of a specified length from a given file.",
                            "size fd -- string");

            interpreter.AddWord("file.!", WordFileWrite,
                            "Write a value as text to a file, unless it's a ByteBuffer.",
                            "value fd -- ");


            interpreter.AddWord("file.line@", WordFileLineRead,
                            "Read a full line from a file.",
                            "fd -- string");

            interpreter.AddWord("file.line!", WordFileLineWrite,
                            "Write a string as a line to the file.",
                            "string fd -- ");


            interpreter.AddWord("file.r/o",
                (interpreter) =>
                {
                    interpreter.Push(Value.From((long)FileAccess.Read));
                },
                "Constant for opening a file as read only.",
                " -- flag");

            interpreter.AddWord("file.w/o",
                (interpreter) =>
                {
                    interpreter.Push(Value.From((long)FileAccess.Write));
                },
                "Constant for opening a file as write only.",
                " -- flag");

            interpreter.AddWord("file.r/w",
                (interpreter) =>
                {
                    interpreter.Push(Value.From((long)FileAccess.ReadWrite));
                },
                "Constant for opening a file for both reading and writing.",
                " -- flag");
        }
    }

}
