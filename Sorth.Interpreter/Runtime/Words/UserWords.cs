
using Sorth.Interpreter.Runtime.DataStructures;



namespace Sorth.Interpreter.Runtime.Words
{

    public static class UserWords
    {
        private static void WordUserEnvRead(SorthInterpreter interpreter)
        {
            var name = interpreter.Pop().AsString(interpreter);
            var value = Environment.GetEnvironmentVariable(name) ?? "";

            interpreter.Push(Value.From(value));
        }

        private static void WordUserOsRead(SorthInterpreter interpreter)
        {
            string result = "";

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    result = "Windows";
                    break;

                case PlatformID.Unix:
                    // For now we just assume Linux.
                    result = "Linux";
                    break;

                case PlatformID.MacOSX:
                    result = "macOS";
                    break;

                default:
                    result = "Other";
                    break;
            }

            interpreter.Push(Value.From(result));
        }


        public static void Register(SorthInterpreter interpreter)
        {
            interpreter.AddWord("user.env@", WordUserEnvRead,
                "Read an environment variable",
                "name -- value_or_empty");

            interpreter.AddWord("user.os", WordUserOsRead,
                "Get the name of the OS the script is running under.",
                " -- os_name");
        }
    }

}
