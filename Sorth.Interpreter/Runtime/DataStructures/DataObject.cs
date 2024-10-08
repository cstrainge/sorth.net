


namespace Sorth.Interpreter.Runtime.DataStructures
{

    public class DataObjectDefinition
    {
        public string Name { get; private set; }

        public bool IsHidden { get; private set; }

        public List<string> FieldNames { get; private set; }
        public List<Value> Defaults { get; private set; }

        public static DataObjectDefinition NullDefinition =
                     new DataObjectDefinition("null", false, new List<string>(), new List<Value>());


        public DataObjectDefinition(string name, bool is_hidden, List<string> field_names,
                                    List<Value> defaults)
        {
            Name = name;
            IsHidden = is_hidden;
            FieldNames = field_names;
            Defaults = defaults;
        }

        public static int indent = 0;

        public override string ToString()
        {
            string outer_spaces = new string(' ', indent);
            string result = $"# {Name}\n";

            indent += 4;
            string inner_spaces = new string(' ', indent);

            for (int i = 0; i < Defaults.Count; ++i)
            {
                result += $"{inner_spaces}{FieldNames[i]} -> {Defaults[i]}";

                if (i < Defaults.Count - 1)
                {
                    result += " , \n";
                }
                else
                {
                    result += "\n";
                }
            }
            indent -= 4;

            return result + $"{outer_spaces};";
        }
    }


    public class DataObject
    {
        public DataObjectDefinition Definition { get; private set; }
        public List<Value> Fields { get; set; }

        public DataObject()
        {
            Definition = DataObjectDefinition.NullDefinition;
            Fields = new List<Value>();
        }

        public DataObject(DataObjectDefinition definition)
        {
            Definition = definition;
            Fields = new List<Value>(Definition.Defaults.Count);

            foreach (var value in Definition.Defaults)
            {
                Fields.Add(value.Clone());
            }
        }

        public override int GetHashCode()
        {
            return Fields.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (   (obj != null)
                && (obj is DataObject other))
            {
                if (   (Definition.Name == other.Definition.Name)
                    && (Fields.Count == other.Fields.Count))
                {
                    for (int i = 0; i < Fields.Count; ++i)
                    {
                        if (!Fields[i].Equals(other.Fields[i]))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            string outer_spaces = new string(' ', DataObjectDefinition.indent);
            string result = $"# {Definition.Name}\n";

            for (int i = 0; i < Fields.Count; ++i)
            {
                DataObjectDefinition.indent += 4;
                string spaces = new string(' ', DataObjectDefinition.indent);

                string field = Fields[i].IsString() ? Value.Stringify(Fields[i].ToString() ?? "")
                                                    : Fields[i].ToString() ?? "";

                result += $"{spaces}{Definition.FieldNames[i]} -> {field}";

                if (i < Fields.Count - 1)
                {
                    result += " , \n";
                }
                else
                {
                    result += "\n";
                }

                DataObjectDefinition.indent -= 4;
            }

            return result + $"{outer_spaces};";
        }
    }

}
