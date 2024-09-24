
using Sorth.Interpreter.Runtime;
using Sorth.Interpreter.Runtime.DataStructures;
using Sorth.Interpreter.Runtime.Words;



static class SorthMain
{
    private static string GetStdLibPath()
    {
        return    Environment.GetEnvironmentVariable("SORTH_NET_LIB_PATH")
               ?? AppDomain.CurrentDomain.BaseDirectory;
    }

    public static void Main(string[] args)
    {
        try
        {
            var interpreter = new SorthInterpreter();

            interpreter.AddSearchPath(GetStdLibPath());

            BaseWords.Register(interpreter);
            TerminalWords.Register(interpreter);
            IoWords.Register(interpreter);
            UserWords.Register(interpreter);

            interpreter.ProcessSourceFile("std.f");

            interpreter.MarkContext();

            interpreter.AddSearchPath(Directory.GetCurrentDirectory());

            if (args.Length >= 1)
            {
                List<Value> script_args = new List<Value>();

                for (int i = 1; i < args.Length; ++i)
                {
                    script_args.Add(Value.From(args[i]));
                }

                interpreter.AddWord("args",
                    (interpreter) =>
                    {
                        interpreter.Push(Value.From(script_args));
                    },
                    "List of command line arguments passed to the script.",
                    " -- arguments");

                interpreter.ProcessSourceFile(args[0]);
            }
            else
            {
                interpreter.ExecuteWord("repl");
            }
        }
        catch (ScriptError error)
        {
            Console.WriteLine("Runtime error:");
            Console.WriteLine(error.Message);
        }
    }
}
