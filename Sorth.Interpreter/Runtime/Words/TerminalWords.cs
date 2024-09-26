
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using Sorth.Interpreter.Runtime.DataStructures;



namespace Sorth.Interpreter.Runtime.Words
{
    static class Windows
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT_RECORD
        {
            public EventType EventType;
            public KEY_EVENT_RECORD KeyEvent;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEY_EVENT_RECORD
        {
            public bool KeyDown;
            public ushort RepeatCount;
            public ushort VirtualKeyCode;
            public ushort VirtualScanCode;
            public uint UnicodeChar;
            public uint ControlKeyState;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CONSOLE_SCREEN_BUFFER_INFO
        {
            public COORD Size;
            public COORD CursorPosition;
            public ushort Attributes;
            public RECT Window;
            public COORD MaxWindowSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COORD
        {
            public short X;
            public short Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private enum EventType
        {
            KEY_EVENT = 1,
            MOUSE_EVENT = 2,
            WINDOW_BUFFER_SIZE_EVENT = 4,
            MENU_EVENT = 8,
            FOCUS_EVENT = 16
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern bool PeekConsoleInput(IntPtr hConsoleInput, 
                                                    IntPtr lpBuffer,
                                                    uint nLength,
                                                    out uint lpNumberOfEvents);

        [DllImport("kernel32.dll")]
        private static extern bool ReadConsoleInput(IntPtr hConsoleInput,
                                                    [Out] INPUT_RECORD[] lpBuffer,
                                                    uint nLength,
                                                    out uint lpNumberOfEvents);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleScreenBufferInfo(IntPtr hConsoleOutput, 
                                                           out CONSOLE_SCREEN_BUFFER_INFO lpBuffer);

        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();

        [DllImport("kernel32.dll")]
        private static extern void SetConsoleCP(uint codePage);

        [DllImport("kernel32.dll")]
        private static extern void SetConsoleOutputCP(uint codePage);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadConsoleA(IntPtr hConsoleInput,
                                                byte[] lpBuffer,
                                                uint nNumberOfBytesToRead,
                                                out uint lpNumberOfBytesRead,
                                                IntPtr lpOverlapped);


        private const int STD_INPUT_HANDLE = -10;
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_ECHO_INPUT = 0x0004;
        private const uint ENABLE_INSERT_MODE = 0x0002;
        private const uint ENABLE_LINE_INPUT = 0x0001;
        private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;
        private const uint ENABLE_PROCESSED_OUTPUT = 0x0001;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0001;
        private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;
        private const uint CP_UTF8 = 65001;


        static uint input_mode = 0;
        static uint output_mode = 0;
        static bool is_in_raw_mode = false;


        public static void InitWinConsole()
        {
            SetConsoleCP(CP_UTF8);
            SetConsoleOutputCP(CP_UTF8);
        }

        private static void FlushEvents()
        {
            IntPtr std_in_handle = GetStdHandle(STD_INPUT_HANDLE);
            uint dw_read;
            INPUT_RECORD[] input = new INPUT_RECORD[1];
            uint numberOfEvents = 0;

            PeekConsoleInput(std_in_handle, IntPtr.Zero, 0, out numberOfEvents);

            if (numberOfEvents > 0)
            {
                while (true)
                {
                    ReadConsoleInput(std_in_handle, input, 1, out dw_read);

                    if (input[0].EventType == EventType.KEY_EVENT 
                                   && input[0].KeyEvent.VirtualKeyCode == (ushort)ConsoleKey.Escape)
                    {
                        break;
                    }
                }
            }
        }

        public static void SetRawMode(SorthInterpreter interpreter, bool set_on)
        {
            IntPtr std_out_handle = GetStdHandle(STD_OUTPUT_HANDLE);
            IntPtr std_in_handle = GetStdHandle(STD_INPUT_HANDLE);

            if (set_on && !is_in_raw_mode)
            {
                if (!GetConsoleMode(std_out_handle, out input_mode))
                {
                    interpreter.ThrowError($"Get console input mode failed: {GetLastError()}");
                }

                if (!GetConsoleMode(std_in_handle, out output_mode))
                {
                    interpreter.ThrowError($"Get console output mode failed: {GetLastError()}");
                }

                uint new_input_mode = input_mode;
                uint new_output_mode = output_mode;

                new_input_mode &= ~(ENABLE_ECHO_INPUT | ENABLE_INSERT_MODE | ENABLE_LINE_INPUT);
                new_input_mode |= ENABLE_VIRTUAL_TERMINAL_INPUT;

                new_output_mode &= ~ENABLE_INSERT_MODE;
                new_output_mode |= ENABLE_PROCESSED_OUTPUT | ENABLE_VIRTUAL_TERMINAL_PROCESSING;

                if (!SetConsoleMode(std_in_handle, new_input_mode))
                {
                    interpreter.ThrowError($"Set console input mode failed: {GetLastError()}");
                }

                if (!SetConsoleMode(std_out_handle, new_output_mode))
                {
                    // interpreter.ThrowError($"Set console output mode failed: {GetLastError()}");
                }
            }
            else if (!set_on && is_in_raw_mode)
            {
                if (!SetConsoleMode(std_in_handle, input_mode))
                {
                    interpreter.ThrowError($"Set console input mode failed: {GetLastError()}");
                }

                if (!SetConsoleMode(std_out_handle, output_mode))
                {
                    interpreter.ThrowError($"Set console output mode failed: {GetLastError()}");
                }

                FlushEvents();
                is_in_raw_mode = false;
            }
        }

        public static ( long, long ) GetTermSize(SorthInterpreter interpreter)
        {
            IntPtr std_out_handle = GetStdHandle(STD_OUTPUT_HANDLE);
            CONSOLE_SCREEN_BUFFER_INFO info;

            if (!GetConsoleScreenBufferInfo(std_out_handle, out info))
            {
                interpreter.ThrowError($"Could not get console information: {GetLastError()}");
            }

            return ( info.MaxWindowSize.X, info.MaxWindowSize.Y );
        }

        public static string GetTermChar(SorthInterpreter interpreter)
        {
            byte[] chars = new byte[1] { (byte)Console.Read() };

            return System.Text.Encoding.ASCII.GetString(chars, 0, 1);
        }
    }

    public static class TerminalWords
    {
        private static void WordTermRawMode(SorthInterpreter interpreter)
        {
            var set_raw_mode = interpreter.Pop().AsBoolean(interpreter);
            Windows.SetRawMode(interpreter, set_raw_mode);
        }

        private static void WordTermSize(SorthInterpreter interpreter)
        {
            var ( width, height ) = Windows.GetTermSize(interpreter);

            interpreter.Push(Value.From(width));
            interpreter.Push(Value.From(height));
        }

        private static void WordTermKey(SorthInterpreter interpreter)
        {
            interpreter.Push(Value.From(Windows.GetTermChar(interpreter)));
        }

        private static void WordTermFlush(SorthInterpreter interpreter)
        {
            Console.Out.Flush();
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
            Windows.InitWinConsole();

            interpreter.AddWord("term.raw_mode", WordTermRawMode,
                "Enter or leave the terminal's 'raw' mode.",
                "bool -- ");

            interpreter.AddWord("term.size@", WordTermSize,
                "Return the number or characters in the rows and columns.",
                " -- columns rows");

            interpreter.AddWord("term.key", WordTermKey,
                "Read a keypress from the terminal.",
                " -- character");

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
