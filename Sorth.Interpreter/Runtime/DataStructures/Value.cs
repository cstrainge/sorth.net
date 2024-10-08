
using System.Text;
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


    class IntValue : Value, IEquatable<IntValue>
    {
        public readonly long Value;
        public IntValue(long value) => Value = value;
        public override string ToString() => Value.ToString();
        public override int GetHashCode() => Value.GetHashCode();

        public bool Equals(IntValue? other)
        {
            return other != null && Value == other.Value;;
        }

        public override bool Equals(object? obj) => Equals(obj as IntValue);

        public override Value Clone() => new IntValue(Value);
    }

    class DoubleValue : Value, IEquatable<DoubleValue>
    {
        public readonly double Value;
        public DoubleValue(double value) => Value = value;
        public override string ToString() => Value.ToString();
        public override int GetHashCode() => Value.GetHashCode();

        public bool Equals(DoubleValue? other)
        {
            return other != null && Value == other.Value;
        }

        public override bool Equals(object? obj) => Equals(obj as DoubleValue);

        public override Value Clone() => new DoubleValue(Value);
    }

    class BoolValue : Value, IEquatable<BoolValue>
    {
        public readonly bool Value;
        public BoolValue(bool value) => Value = value;
        public override string ToString() => Value ? "true" : "false";
        public override int GetHashCode() => Value.GetHashCode();

        public bool Equals(BoolValue? other)
        {
            return other != null && Value == other.Value;
        }

        public override bool Equals(object? obj) => Equals(obj as BoolValue);

        public override Value Clone() => new BoolValue(Value);
    }

    class StringValue : Value, IEquatable<StringValue>
    {
        public readonly string Value;
        public StringValue(string value) => Value = value;
        public override string ToString() => Value;
        public override int GetHashCode() => Value.GetHashCode();

        public bool Equals(StringValue? other)
        {
            return other != null && Value == other.Value;
        }

        public override bool Equals(object? obj) => Equals(obj as StringValue);

        public override Value Clone() => new StringValue(Value);
    }

    class TokenValue : Value, IEquatable<TokenValue>
    {
        public readonly Token Value;
        public TokenValue(Token value) => Value = value;
        public override string ToString() => Value.Text;
        public override int GetHashCode() => Value.GetHashCode();

        public bool Equals(TokenValue? other)
        {
            return other != null && Value.Equals(other.Value);
        }

        public override bool Equals(object? obj) => Equals(obj as TokenValue);

        public override Value Clone() => new TokenValue(Value);
    }

    class ArrayValue : Value, IEquatable<ArrayValue>
    {
        public readonly List<Value> Value;
        public ArrayValue(List<Value> value) => Value = value;

        public override string ToString()
        {
            var result = new StringBuilder();

            result.Append("[ ");

            for (int i = 0; i < Value.Count; ++i)
            {
                var value = Value[i];

                if (value is StringValue string_value)
                {
                    result.Append(Stringify(string_value.Value));
                }
                else
                {
                    result.Append(value);
                }

                result.Append(i < (Value.Count - 1) ? " , " : " ");
            }

            result.Append("]");

            return result.ToString();
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();

            foreach (var item in Value)
            {
                hash.Add(item);
            }

            return hash.ToHashCode();
        }

        public bool Equals(ArrayValue? other)
        {
            if (other != null)
            {
                return Value.SequenceEqual(other.Value);
            }

            return false;
        }

        public override bool Equals(object? obj) => Equals(obj as ArrayValue);

        public override Value Clone()
        {
            var new_values = new List<Value>(Value.Count);

             foreach (var item in Value)
             {
                 new_values.Add(item.Clone());
             }

            return new ArrayValue(new_values);
        }
    }

    class HashMapValue : Value, IEquatable<HashMapValue>
    {
        public readonly Dictionary<Value, Value> Value;
        public HashMapValue(Dictionary<Value, Value> value) => Value = value;

        public override string ToString()
        {
            string outer_spaces = new string(' ', DataObjectDefinition.indent);
            var result = new StringBuilder();

            result.Append("{\n");

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

                result.AppendFormat("{0}{1} -> {2}", inner_spaces, key, value);

                if (index < count - 1)
                {
                    result.Append(" , \n");
                }
                else
                {
                    result.Append("\n");
                }

                ++index;
            }
            DataObjectDefinition.indent -= 4;

            result.Append(outer_spaces);
            result.Append("}");

            return result.ToString();
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();

            foreach (var item in Value)
            {
                hash.Add(HashCode.Combine(item.Key ?? (object)0, item.Value ?? (object)0));
            }

            return hash.ToHashCode();
        }

        public bool Equals(HashMapValue? other)
        {
            if (other == null)
            {
                return false;
            }

            if (Value.Count != other.Value.Count)
            {
                return false;
            }

            foreach (var item in Value)
            {
                if (!other.Value.TryGetValue(item.Key, out var other_value))
                {
                    return false;
                }

                if (!item.Value.Equals(other_value))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as HashMapValue);

        public override Value Clone()
        {
            var new_map = new Dictionary<Value, Value>(Value.Count);

            foreach (var entry in Value)
            {
                new_map[entry.Key?.Clone() ?? DataStructures.Value.Default()] =
                                             entry.Value?.Clone() ?? DataStructures.Value.Default();
            }

            return new HashMapValue(new_map);
        }
    }

    class DataObjectValue : Value, IEquatable<DataObjectValue>
    {
        public readonly DataObject Value;
        public DataObjectValue(DataObject value) => Value = value;
        public override string ToString() => Value.ToString();
        public override int GetHashCode() => Value.GetHashCode();

        public bool Equals(DataObjectValue? other)
        {
            if (other != null)
            {
                return Value.Equals(other.Value);
            }

            return false;
        }

        public override bool Equals(object? obj) => Equals(obj as DataObjectValue);

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

    class ByteCodeValue : Value, IEquatable<ByteCodeValue>
    {
        public readonly List<ByteCode> Value;
        public ByteCodeValue(List<ByteCode> value) => Value = value;
        public override string ToString() => "<bytecode>";
        public override int GetHashCode() => Value.GetHashCode();


        public bool Equals(ByteCodeValue? other)
        {
            if (other != null)
            {
                return Value.Equals(other.Value);
            }

            return false;
        }

        public override bool Equals(object? obj) => Equals(obj as ByteCodeValue);

        public override Value Clone()
        {
            var new_code = new List<ByteCode>(Value);
            return new ByteCodeValue(new_code);
        }
    }

    class ByteBufferValue : Value, IEquatable<ByteBufferValue>
    {
        public readonly ByteBuffer Value;
        public ByteBufferValue(ByteBuffer value) => Value = value;
        public override string ToString() => Value.ToString();
        public override int GetHashCode() => Value.GetHashCode();
        public bool Equals(ByteBufferValue? other) => other != null && Value.Equals(other.Value);
        public override bool Equals(object? obj) => Equals(obj as ByteBufferValue);
        public override Value Clone() => new ByteBufferValue(Value.Clone());
    }

}
