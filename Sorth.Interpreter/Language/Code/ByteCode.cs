
using Sorth.Interpreter.Language.Source;
using Sorth.Interpreter.Runtime;
using Sorth.Interpreter.Runtime.DataStructures;



namespace Sorth.Interpreter.Language.Code
{

    public struct ByteCode
    {
        public enum Id
        {
            DefVariable,
            DefConstant,

            ReadVariable,
            WriteVariable,

            Execute,

            WordIndex,
            WordExists,

            PushConstantValue,

            MarkLoopExit,
            UnmarkLoopExit,

            MarkCatch,
            UnmarkCatch,

            Jump,
            JumpIfZero,
            JumpIfNotZero,
            JumpLoopStart,
            JumpLoopExit,

            JumpTarget
        }

        public Id id;
        public Value value;
        public Location? location;

        public ByteCode(Id new_id, Value new_value, Location? new_location)
        {
            id = new_id;
            value = new_value;
            location = new_location;
        }

        public static string ToString(SorthInterpreter interpreter, List<ByteCode> code)
        {
            return "";
        }

        public override string ToString()
        {
            string output = IdToString(id);

            if (!DoesNotHaveParameter(id))
            {
                output += " ";

                if (value.IsString())
                {
                    output += value.ToString();
                }
                else
                {
                    output += value;
                }
            }

            return output;
        }

        public static string IdToString(Id id)
        {
            string value = "";

            switch (id)
            {
                case Id.DefVariable:       value = "DefVariable      "; break;
                case Id.DefConstant:       value = "DefConstant      "; break;
                case Id.ReadVariable:      value = "ReadVariable     "; break;
                case Id.WriteVariable:     value = "WriteVariable    "; break;
                case Id.Execute:           value = "Execute          "; break;
                case Id.WordIndex:         value = "WordIndex        "; break;
                case Id.WordExists:        value = "WordExists       "; break;
                case Id.PushConstantValue: value = "PushConstantValue"; break;
                case Id.MarkLoopExit:      value = "MarkLoopExit     "; break;
                case Id.UnmarkLoopExit:    value = "UnmarkLoopExit   "; break;
                case Id.MarkCatch:         value = "MarkCatch        "; break;
                case Id.UnmarkCatch:       value = "UnmarkCatch      "; break;
                case Id.Jump:              value = "Jump             "; break;
                case Id.JumpIfZero:        value = "JumpIfZero       "; break;
                case Id.JumpIfNotZero:     value = "JumpIfNotZero    "; break;
                case Id.JumpLoopStart:     value = "JumpLoopStart    "; break;
                case Id.JumpLoopExit:      value = "JumpLoopExit     "; break;
                case Id.JumpTarget:        value = "JumpTarget       "; break;
            }

            return value;
        }

        public static bool DoesNotHaveParameter(Id id)
        {
            return    (id == Id.ReadVariable)
                   || (id == Id.WriteVariable)
                   || (id == Id.JumpTarget)
                   || (id == Id.UnmarkLoopExit)
                   || (id == Id.UnmarkCatch)
                   || (id == Id.JumpLoopExit);
        }
    }

}
