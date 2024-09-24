
using System;
using System.IO;



namespace Sorth.Interpreter.Language.Source
{
    public class SourceBuffer
    {
        private string Source;
        private int Position;
        private Location SourceLocation;

        public SourceBuffer()
        {
            Source = "";
            Position = 0;
            SourceLocation = new Location("<empty>");
        }

        public SourceBuffer(String Name, String NewSource)
        {
            Source = NewSource;
            Position = 0;
            SourceLocation = new Location(Name);
        }

        public SourceBuffer(string NewPath)
        {
            Source = File.ReadAllText(NewPath);
            Position = 0;
            SourceLocation = new Location(NewPath);
        }

        public bool Eob()
        {
            return Position >= Source.Length;
        }

        public char PeekNext()
        {
            if (Eob())
            {
                return ' ';
            }

            return Source[Position];
        }

        public char Next()
        {
            char next = PeekNext();

            if (!Eob())
            {
                IncrementLocation(next);
            }

            return next;
        }

        public Location CurrentLocation()
        {
            return SourceLocation;
        }

        private void IncrementLocation(char next)
        {
            ++Position;

            if (next == '\n')
            {
                SourceLocation.NextLine();
            }
            else
            {
                SourceLocation.Next();
            }
        }
    }
}
