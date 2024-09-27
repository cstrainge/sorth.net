
using Sorth.Interpreter.Language.Code;
using Sorth.Interpreter.Language.Source;



namespace Sorth.Interpreter.Runtime.DataStructures
{

    public abstract class Value
    {
        public static Value Default() => From(0);

        public static Value From(long value) => new IntValue(value);
        public static Value From(double value) => new DoubleValue(value);
        public static Value From(bool value) => new BoolValue(value);
        public static Value From(string value) => new StringValue(value);
        public static Value From(Token value) => new TokenValue(value);
        public static Value From(List<Value> value) => new ArrayValue(value);
        public static Value From(Dictionary<Value, Value> value) => new HashMapValue(value);
        public static Value From(DataObject value) => new DataObjectValue(value);
        public static Value From(List<ByteCode> value) => new ByteCodeValue(value);
        public static Value From(ByteBuffer value) => new ByteBufferValue(value);

        public bool IsNumeric() => IsInteger() || IsDouble() || IsBoolean();
        public bool IsInteger() => this is IntValue;
        public bool IsDouble() => this is DoubleValue;
        public bool IsBoolean() => this is BoolValue;
        public bool IsString() => (this is StringValue) || (this is TokenValue);
        public bool IsToken() => this is TokenValue;
        public bool IsArray() => this is ArrayValue;
        public bool IsHashMap() => this is HashMapValue;
        public bool IsDataObject() => this is DataObjectValue;
        public bool IsByteCode() => this is ByteCodeValue;
        public bool IsByteBuffer() => this is ByteBufferValue;

        public static bool BothAreNumeric(Value a, Value b) => a.IsNumeric() && b.IsNumeric();

        public static bool EitherIsNumeric(Value a, Value b) => a.IsNumeric() || b.IsNumeric();
        public static bool EitherIsInteger(Value a, Value b) => a.IsInteger() || b.IsInteger();
        public static bool EitherIsDouble(Value a, Value b) => a.IsDouble() || b.IsDouble();
        public static bool EitherIsBoolean(Value a, Value b) => a.IsBoolean() || b.IsBoolean();
        public static bool EitherIsString(Value a, Value b) => a.IsString() || b.IsString();

        public abstract Value Clone();

        public long AsInteger(SorthInterpreter interpreter)
        {
            long result = 0;

            if (this is IntValue int_value)
            {
                result = int_value.Value;
            }
            else if (this is DoubleValue double_value)
            {
                result = (long)double_value.Value;
            }
            else if (this is BoolValue bool_value)
            {
                result = bool_value.Value ? 1 : 0;
            }
            else
            {
                interpreter.ThrowError("Value not convertible to integer.");
            }

            return result;
        }

        public double AsDouble(SorthInterpreter interpreter)
        {
            double result = 0.0;

            if (this is IntValue int_value)
            {
                result = (double)int_value.Value;
            }
            else if (this is DoubleValue double_value)
            {
                result = double_value.Value;
            }
            else if (this is BoolValue bool_value)
            {
                result = bool_value.Value ? 1.0 : 0.0;
            }
            else
            {
                interpreter.ThrowError("Value not convertible to double.");
            }

            return result;
        }

        public bool AsBoolean(SorthInterpreter interpreter)
        {
            bool result = false;

            if (this is IntValue int_value)
            {
                result = int_value.Value > 0;
            }
            else if (this is DoubleValue double_value)
            {
                result = double_value.Value > 0.0;
            }
            else if (this is BoolValue bool_value)
            {
                result = bool_value.Value;
            }
            else if (this is StringValue string_value)
            {
                result = string_value.Value != "";
            }
            else
            {
                interpreter.ThrowError("Value not convertible to boolean.");
            }

            return result;
        }

        public string AsString(SorthInterpreter interpreter)
        {
            string result = "";

            if (this is StringValue string_value)
            {
                result = string_value.Value;
            }
            else if (this is TokenValue token)
            {
                result = token.Value.Text;
            }
            else
            {
                result = ToString() ?? "";
            }

            return result;
        }

        public Token AsToken(SorthInterpreter interpreter)
        {
            Token result = new Token();

            if (this is TokenValue token)
            {
                result = token.Value;
            }
            else
            {
                interpreter.ThrowError("Value is not a token.");
            }

            return result;
        }

        public List<Value> AsArray(SorthInterpreter interpreter)
        {
            var result = new List<Value>();

            if (this is ArrayValue code)
            {
                result = code.Value;
            }
            else
            {
                interpreter.ThrowError("Value is not an array.");
            }

            return result;
        }

        public Dictionary<Value, Value> AsHashMap(SorthInterpreter interpreter)
        {
            var result = new Dictionary<Value, Value>();

            if (this is HashMapValue map)
            {
                result = map.Value;
            }
            else
            {
                interpreter.ThrowError("Value is not an hash map.");
            }

            return result;
        }

        public DataObject AsDataObject(SorthInterpreter interpreter)
        {
            var result = new DataObject();

            if (this is DataObjectValue data)
            {
                result = data.Value;
            }
            else
            {
                interpreter.ThrowError("Value is not a data object.");
            }

            return result;
        }

        public List<ByteCode> AsByteCode(SorthInterpreter interpreter)
        {
            var result = new List<ByteCode>();

            if (this is ByteCodeValue code)
            {
                result = code.Value;
            }
            else
            {
                interpreter.ThrowError("Value is not byte code.");
            }

            return result;
        }

        public ByteBuffer AsByteBuffer(SorthInterpreter interpreter)
        {
            var result = new ByteBuffer(0);

            if (this is ByteBufferValue buffer)
            {
                result = buffer.Value;
            }
            else
            {
                interpreter.ThrowError("Value is not byte code.");
            }

            return result;
        }

        public static string Stringify(string str_value)
        {
            string output = "\"";

            for (int i = 0; i < str_value.Length; ++i)
            {
                char next = str_value[i];

                switch (next)
                {
                    case '\r': output += "\\r"; break;
                    case '\n': output += "\\n"; break;
                    case '\t': output += "\\t"; break;
                    case '\"': output += "\\\""; break;

                    default: output += next; break;
                }
            }

            output += "\"";

            return output;
        }
    }


    class IntValue : Value
    {
        public readonly long Value;
        public IntValue(long value) => Value = value;
        public override string ToString() => Value.ToString();
        public override int GetHashCode() => Value.GetHashCode();

        public override bool Equals(object? obj)
        {
            if (   (obj != null)
                && (obj is IntValue other))
            {
                return Value == other.Value;
            }

            return false;
        }

        public override Value Clone() => new IntValue(Value);
    }

    class DoubleValue : Value
    {
        public readonly double Value;
        public DoubleValue(double value) => Value = value;
        public override string ToString() => Value.ToString();
        public override int GetHashCode() => Value.GetHashCode();

        public override bool Equals(object? obj)
        {
            if (   (obj != null)
                && (obj is DoubleValue other))
            {
                return Value == other.Value;
            }

            return false;
        }

        public override Value Clone() => new DoubleValue(Value);
    }

    class BoolValue : Value
    {
        public readonly bool Value;
        public BoolValue(bool value) => Value = value;
        public override string ToString() => Value ? "true" : "false";
        public override int GetHashCode() => Value.GetHashCode();

        public override bool Equals(object? obj)
        {
            if (   (obj != null)
                && (obj is BoolValue other))
            {
                return Value == other.Value;
            }

            return false;
        }

        public override Value Clone() => new BoolValue(Value);
    }

    class StringValue : Value
    {
        public readonly string Value;
        public StringValue(string value) => Value = value;
        public override string ToString() => Value;
        public override int GetHashCode() => Value.GetHashCode();

        public override bool Equals(object? obj)
        {
            if (   (obj != null)
                && (obj is StringValue other))
            {
                return Value == other.Value;
            }

            return false;
        }

        public override Value Clone() => new StringValue(Value);
    }

    class TokenValue : Value
    {
        public readonly Token Value;
        public TokenValue(Token value) => Value = value;
        public override string ToString() => Value.Text;
        public override int GetHashCode() => Value.GetHashCode();

        public override bool Equals(object? obj)
        {
            if (   (obj != null)
                && (obj is TokenValue other))
            {
                return Value.Equals(other.Value);
            }

            return false;
        }

        public override Value Clone() => new TokenValue(Value);
    }

    class ArrayValue : Value
    {
        public readonly List<Value> Value;
        public ArrayValue(List<Value> value) => Value = value;

        public override string ToString()
        {
            string result = "[ ";

            for (int i = 0; i < Value.Count; ++i)
            {
                var value = Value[i];

                if (value is StringValue string_value)
                {
                    result += Stringify(string_value.Value);
                }
                else
                {
                    result += value;
                }

                result += i < (Value.Count - 1) ? " , " : " ";
            }

            return result + "]";
        }

        public override int GetHashCode() => Value.GetHashCode();

        public override bool Equals(object? obj)
        {
            if (   (obj != null)
                && (obj is ArrayValue other))
            {
                return Value.Equals(other.Value);
            }

            return false;
        }

        public override Value Clone()
        {
            List<Value> new_values = new List<Value>(Value.Count);

            for (int i = 0; i < Value.Count; ++i)
            {
                new_values.Add(Value[i].Clone());
            }

            return new ArrayValue(new_values);
        }
    }

    class HashMapValue : Value
    {
        public readonly Dictionary<Value, Value> Value;
        public HashMapValue(Dictionary<Value, Value> value) => Value = value;

        public override string ToString()
        {
            string outer_spaces = new string(' ', DataObjectDefinition.indent);
            string result = "{\n";

            DataObjectDefinition.indent += 4;
            string inner_spaces = new string(' ', DataObjectDefinition.indent);

            int count = Value.Count;
            int index = 0;

            foreach (var entry in Value)
            {
                string key = entry.Key.IsString() ? Stringify(entry.Key.ToString() ?? "")
                                                  : entry.Key.ToString() ?? "";
                string value = entry.Value.IsString() ? Stringify(entry.Value.ToString() ?? "")
                                                      : entry.Value.ToString() ?? "";

                result += $"{inner_spaces}{key} -> {value}";

                if (index < count - 1)
                {
                    result += " , \n";
                }
                else
                {
                    result += "\n";
                }

                ++index;
            }
            DataObjectDefinition.indent -= 4;

            return result + outer_spaces + "}";
        }

        public override int GetHashCode() => Value.GetHashCode();

        public override bool Equals(object? obj)
        {
            if (   (obj != null)
                && (obj is HashMapValue other))
            {
                return Value.Equals(other.Value);
            }

            return false;
        }

        public override Value Clone()
        {
            var new_map = new Dictionary<Value, Value>(Value.Count);

            foreach (var entry in Value)
            {
                new_map[entry.Key.Clone()] = entry.Value.Clone();
            }

            return new HashMapValue(new_map);
        }
    }

    class DataObjectValue : Value
    {
        public readonly DataObject Value;
        public DataObjectValue(DataObject value) => Value = value;
        public override string ToString() => Value.ToString();
        public override int GetHashCode() => Value.GetHashCode();

        public override bool Equals(object? obj)
        {
            if (   (obj != null)
                && (obj is DataObjectValue other))
            {
                return Value.Equals(other.Value);
            }

            return false;
        }

        public override Value Clone()
        {
            DataObject data = new DataObject(Value.Definition);

            for (int i = 0; i < data.Fields.Count; ++i)
            {
                data.Fields[i] = Value.Fields[i].Clone();
            }

            return new DataObjectValue(data);
        }
    }

    class ByteCodeValue : Value
    {
        public readonly List<ByteCode> Value;
        public ByteCodeValue(List<ByteCode> value) => Value = value;
        public override string ToString() => "<bytecode>";
        public override int GetHashCode() => Value.GetHashCode();

        public override bool Equals(object? obj)
        {
            if (   (obj != null)
                && (obj is ByteCodeValue other))
            {
                return Value.Equals(other.Value);
            }

            return false;
        }

        public override Value Clone()
        {
            var new_code = new List<ByteCode>(Value);
            return new ByteCodeValue(new_code);
        }
    }

    class ByteBufferValue : Value
    {
        public readonly ByteBuffer Value;
        public ByteBufferValue(ByteBuffer value) => Value = value;
        public override string ToString() => Value.ToString();
        public override int GetHashCode() => Value.GetHashCode();
        public override bool Equals(object? obj) => Value.Equals(obj);
        public override Value Clone() => new ByteBufferValue(Value.Clone());
    }

}
