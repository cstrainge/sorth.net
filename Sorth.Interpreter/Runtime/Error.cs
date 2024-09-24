
using Sorth.Interpreter.Language.Source;



namespace Sorth.Interpreter.Runtime
{

    public class ScriptError : Exception
    {
        public ScriptError(string message)
        : base(message)
        {
        }

        public ScriptError(Location location, string message)
        : base(location + ": " + message)
        {
        }

        public ScriptError(Location location, string message, Exception inner_exception)
        : base(location + ": " + message, inner_exception)
        {
        }
    }

}
