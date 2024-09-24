

using Sorth.Interpreter.Language.Source;
using Sorth.Interpreter.Runtime;
using Sorth.Interpreter.Runtime.DataStructures;


namespace Sorth.Interpreter.Language.Code
{

    public class Construction
    {
        public bool IsImmediate;
        public bool IsHidden;

        public string Name;
        public string Description;
        public string Signature;

        public Location? Location;

        public List<ByteCode> ByteCode;

        public Construction()
        {
            IsImmediate = false;
            IsHidden = false;

            Name = "";
            Description = "";
            Signature = "";

            Location = null;

            ByteCode = new List<ByteCode>();
        }
    }


    public class Constructor
    {
        public Stack<Construction> Stack;
        public bool UserIsInsertingAtBeginning;

        public List<Token> Tokens;
        public int CurrentToken;

        public Construction? Top
        {
            get
            {
                if (Stack.Count > 0)
                {
                    return Stack.Peek();
                }

                return null;
            }
        }

        public Constructor(List<Token> tokens)
        {
            Stack = new Stack<Construction>();
            Stack.Push(new Construction());

            UserIsInsertingAtBeginning = false;

            Tokens = tokens;
            CurrentToken = 0;
        }

        public void CompileToken(SorthInterpreter interpreter, Token token)
        {
            // In Forth anything can be a word, so first we see if it's defined in the dictionary.  
            // If it is, we either compile or execute the word depending on if it's an immediate.
            var ( found, word ) = token.Type != Token.TokenType.String
                                  ? interpreter.FindWord(token.Text) : ( false, null );

            if (found && word.HasValue)
            {
                var the_word = word.Value;

                if (the_word.is_immediate)
                {
                    interpreter.ExecuteWord(token.Location, the_word);
                }
                else
                {
                    Stack.Peek().ByteCode.Add(new ByteCode(ByteCode.Id.Execute,
                                                           Value.From(the_word.handler_index),
                                                           token.Location));
                }
            }
            else
            {
                // We didn't find a word, so try to process the token as the tokenizer found it.
                switch (token.Type)
                {
                    case Token.TokenType.Number:
                        if (token.Text.IndexOf('.') != -1)
                        {
                            Stack.Peek().ByteCode.Add(new ByteCode(ByteCode.Id.PushConstantValue,
                                                               Value.From(double.Parse(token.Text)),
                                                               null));
                        }
                        else
                        {
                            long value = 0;

                            if (token.Text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                            {
                                value = Convert.ToInt64(token.Text.Substring(2), 16);
                            }
                            else if (token.Text.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
                            {
                                value = Convert.ToInt64(token.Text.Substring(2), 2);
                            }
                            else
                            {
                                value = long.Parse(token.Text);
                            }

                            Stack.Peek().ByteCode.Add(new ByteCode(ByteCode.Id.PushConstantValue,
                                                                   Value.From(value),
                                                                   null));
                        }
                        break;

                    case Token.TokenType.String:
                        Stack.Peek().ByteCode.Add(new ByteCode(ByteCode.Id.PushConstantValue,
                                                               Value.From(token.Text),
                                                               null));
                        break;

                    case Token.TokenType.Word:
                        Stack.Peek().ByteCode.Add(new ByteCode(ByteCode.Id.Execute,
                                                               Value.From(token.Text),
                                                               token.Location));
                        break;
                }
            }
        }

        public void CompileTokenList(SorthInterpreter interpreter)
        {
            for (CurrentToken = 0; CurrentToken < Tokens.Count; ++CurrentToken)
            {
                CompileToken(interpreter, Tokens[CurrentToken]);
            }
        }
    }

}
