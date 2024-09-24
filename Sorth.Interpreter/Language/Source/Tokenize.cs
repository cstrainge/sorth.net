
using System.Diagnostics.CodeAnalysis;
using Sorth.Interpreter.Runtime;



namespace Sorth.Interpreter.Language.Source
{

    public readonly struct Token
    {
        public enum TokenType
        {
            Number,
            String,
            Word
        }

        public readonly TokenType Type;
        public readonly Location Location;
        public readonly string Text;

        public Token(TokenType new_type, Location new_location, string new_text)
        {
            Type = new_type;
            Location = new_location;
            Text = new_text;
        }

        public override string ToString()
        {
            string output = Location + ": ";

            if (Type == TokenType.String)
            {
                output += "\"" + Text + "\"";
            }
            else
            {
                output += Text;
            }

            return output;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (   (obj != null)
                && (obj is Token other))
            {
                return (Type == other.Type) && (Text == other.Text);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Location, Text);
        }
    }



    public static class Tokenizer
    {

        private static bool IsWhitespace(char next)
        {
            return    (next == ' ')
                   || (next == '\t')
                   || (next == '\r')
                   || (next == '\n');
        }

        private static bool IsNumeric(string text)
        {
            if ((text[0] >= '0') && (text[0] <= '9'))
            {
                return true;
            }

            if (   ((text[0] == '-') || (text[0] == '+'))
                && (text.Length >= 2))
            {
                return (text[1] >= '0') && (text[1] <= '9');
            }

            return false;
        }

        private static bool SkipWhitespace(SourceBuffer buffer)
        {
            char next = buffer.PeekNext();

            while (!buffer.Eob() && IsWhitespace(next))
            {
                buffer.Next();
                next = buffer.PeekNext();
            }

            return !buffer.Eob();
        }

        private static void SkipWhitespaceUntilColumn(SourceBuffer buffer, int column)
        {
            char next = buffer.PeekNext();

            while (   (!buffer.Eob())
                   && (IsWhitespace(next))
                   && (buffer.CurrentLocation().Column < column))
            {
                buffer.Next();
                next = buffer.PeekNext();
            }
        }

        private static void AppendNewLines(ref string text, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                text += '\n';
            }
        }

        private static char ProcessEscapeLiteral(SourceBuffer buffer)
        {
            char next = buffer.Next();

            switch (next)
            {
                case 'n':
                    next = '\n';
                    break;

                case 'r':
                    next = '\r';
                    break;

                case 't':
                    next = '\t';
                    break;

                case '0':
                    Location start = buffer.CurrentLocation();
                    string number_string = "";

                    while (!buffer.Eob())
                    {
                        next = buffer.PeekNext();

                        if ((next >= '0') && (next <= '9'))
                        {
                            number_string += next;
                            buffer.Next();
                        }
                        else
                        {
                            break;
                        }
                    }

                    int numeric = int.Parse(number_string);

                    if (numeric >= 256)
                    {
                        throw new ScriptError(start, "Numeric literal out of range.");
                    }

                    next = (char)numeric;
                    break;
            }

            return next;
        }

        private static string ProcessMultiLineString(Location start, SourceBuffer buffer)
        {
            // Extract the *.
            buffer.Next();

            SkipWhitespace(buffer);

            int target_column = buffer.CurrentLocation().Column;
            char next = (char)0;
            string new_string = "";

            while (!buffer.Eob())
            {
                next = buffer.Next();

                // Found an asterisk, check to see if the next char is a quote.  If it is, we're
                // done with this string.
                if (next == '*')
                {
                    if (buffer.PeekNext() == '"')
                    {
                        next = buffer.Next();
                        break;
                    }
                }
                else if (next == '\\')
                {
                    // Process the escaped character.
                    new_string += ProcessEscapeLiteral(buffer);
                }
                else if (next == '\n')
                {
                    new_string += next;

                    // We're on a new line, so get rid of the whitespace until we either find text
                    // or reach the column we're looking for.  Any whitespace after that column will
                    // be included in the string.  Any skiped new lines should be added to the
                    // string.
                    int start_line = buffer.CurrentLocation().Line;

                    SkipWhitespaceUntilColumn(buffer, target_column);

                    int current_line = buffer.CurrentLocation().Line;

                    if (current_line > start_line)
                    {
                        AppendNewLines(ref new_string, current_line - start_line);
                    }
                }
                else
                {
                    // No special processing needed for this character.
                    new_string += next;
                }
            }

            return new_string;
        }

        private static string ProcessString(SourceBuffer buffer)
        {
            Location start = buffer.CurrentLocation();
            string new_string = "";

            buffer.Next();

            if (buffer.PeekNext() == '*')
            {
                new_string = ProcessMultiLineString(start, buffer);
            }
            else
            {
                char next = ' ';

                while (!buffer.Eob())
                {
                    next = buffer.Next();

                    if (next == '\"')
                    {
                        break;
                    }

                    if (next == '\n')
                    {
                        throw new ScriptError(start, "Unexpected new line in string literal.");
                    }

                    if (next == '\\')
                    {
                        next = ProcessEscapeLiteral(buffer);
                    }

                    new_string += next;
                }

                if (next != '\"')
                {
                    throw new ScriptError(start, "Missing end of string literal.");
                }
            }

            return new_string;
        }

        private static string GetWhileNotWhitespace(SourceBuffer buffer)
        {
            string new_string = "";
            char next = buffer.PeekNext();

            while (   (!buffer.Eob())
                   && (!IsWhitespace(next)))
            {
                new_string += buffer.Next();
                next = buffer.PeekNext();
            }

            return new_string;
        }

        public static List<Token> Tokenize(SourceBuffer buffer)
        {
            var tokens = new List<Token>();

            while (!buffer.Eob())
            {
                if (!SkipWhitespace(buffer))
                {
                    break;
                }

                Token.TokenType type = Token.TokenType.Word;
                Location location = buffer.CurrentLocation();
                string text;

                if (buffer.PeekNext() == '\"')
                {
                    type = Token.TokenType.String;
                    text = ProcessString(buffer);
                }
                else
                {
                    text = GetWhileNotWhitespace(buffer);
                }

                if (   (type != Token.TokenType.String)
                    && (IsNumeric(text)))
                {
                    type = Token.TokenType.Number;
                }

                tokens.Add(new Token(type, location, text));
            }

            return tokens;
        }
    }

}
