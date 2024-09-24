
using Sorth.Interpreter.Runtime.DataStructures;



namespace Sorth.Interpreter.Runtime.Words
{

    public static class TerminalWords
    {
        private static void WordTermFlush(SorthInterpreter interpreter)
        {
        }

        private static void WordTermReadLine(SorthInterpreter interpreter)
        {
            interpreter.Push(Value.From(Console.ReadLine() ?? ""));
        }

        private static void WordTermWrite(SorthInterpreter interpreter)
        {
            Console.Write(interpreter.Pop());
        }

        private static void WordTermIsPrintable(SorthInterpreter interpreter)
        {
            char value = interpreter.Pop().AsString(interpreter)[0];

            bool result =    (value >= 32)
                          || (value == '\n')
                          || (value == '\t');

            interpreter.Push(Value.From(value));
        }


        public static void Register(SorthInterpreter interpreter)
        {
            /*interpreter.AddWord("term.raw_mode", WordTermRawMode,
                "Enter or leave the terminal's 'raw' mode.",
                "bool -- ");

            interpreter.AddWord("term.size@", WordTermSize,
                "Return the number or characters in the rows and columns.",
                " -- columns rows");

            interpreter.AddWord("term.key", WordTermKey,
                "Read a keypress from the terminal.",
                " -- character");*/

            interpreter.AddWord("term.flush", WordTermFlush,
                "Flush the terminals buffers.",
                " -- ");

            interpreter.AddWord("term.readline", WordTermReadLine,
                "Read a line of text from the terminal.",
                " -- string");

            interpreter.AddWord("term.!", WordTermWrite,
                "Write a value to the terminal.",
                "value -- ");

            interpreter.AddWord("term.is_printable?", WordTermIsPrintable,
                "Is the given character printable?",
                "character -- bool");
        }
    }

}
