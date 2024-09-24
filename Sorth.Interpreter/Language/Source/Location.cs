
using System;
using System.Diagnostics.CodeAnalysis;
using static System.Net.Mime.MediaTypeNames;



namespace Sorth.Interpreter.Language.Source
{
    public struct Location
    {
        public string Path { get; private set; }

        public int Line { get; private set; }
        public int Column { get; private set; }

        public Location()
        {
            Path = "";
            Line = 1;
            Column = 1;
        }

        public Location(string NewPath)
        {
            Path = NewPath;
            Line = 1;
            Column = 1;
        }

        public Location(string NewPath, int NewLine, int NewColumn)
        {
            Path = NewPath;
            Line = NewLine;
            Column = NewColumn;

        }

        public void NextLine()
        {
            ++Line;
            Column = 1;
        }

        public void Next()
        {
            ++Column;
        }

        public override string ToString()
        {
            string output = "";

            if (Path.Length > 0)
            {
                output = Path + ":";
            }

            output += Line + ":" + Column;

            return output;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (   (obj != null)
                && (obj is Location other))
            {
                return (Path == other.Path) && (Line == other.Line) && (Column == other.Column);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Path, Line, Column);
        }
    }
}
