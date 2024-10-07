
using System.Reflection;
using System.Reflection.Emit;
using Sorth.Interpreter.Language.Source;
using Sorth.Interpreter.Runtime;
using Sorth.Interpreter.Runtime.DataStructures;



namespace Sorth.Interpreter.Language.Code
{


    public class ConstantHandler
    {
        public readonly Value Constant;

        public ConstantHandler(Value new_constant)
        {
            Constant = new_constant;
        }

        public void Handler(SorthInterpreter interpreter)
        {
            interpreter.Push(Constant);
        }

        public void Register(SorthInterpreter interpreter, string name)
        {
            interpreter.AddWord(name,
                                this.Handler,
                                $"Push value for constant {name}.",
                                " -- value");
        }
    }


    public class VariableHandler
    {
        public readonly long Index;

        public VariableHandler(long new_index)
        {
            Index = new_index;
        }

        public void Handler(SorthInterpreter interpreter)
        {
            interpreter.Push(Value.From(Index));
        }

        public void Register(SorthInterpreter interpreter, string name)
        {
            interpreter.AddWord(name,
                                this.Handler,
                                $"Push index for variable {name}.",
                                " -- index");
        }
    }


    struct CompileInfo
    {
        public List<( string, Value )> Constants;

        public FieldBuilder LocationArray;
        public List<Location> Locations;
    }



    public static class SorthILGenerator
    {
        // The assembly and module to hold all our generated code.
        private static AssemblyBuilder Builder;
        private static ModuleBuilder Module;

        static SorthILGenerator()
        {
            var name = new AssemblyName("SorthDynamicAssembly");

            Builder = AssemblyBuilder.DefineDynamicAssembly(name,
                                                            AssemblyBuilderAccess.RunAndCollect);
            Module = Builder.DefineDynamicModule("SorthDynamicModule");

            UniqueIndex = 0;
        }

        // Use an index to make sure that the generated name of the handler class is unique.
        private static uint UniqueIndex;

        private static uint Index()
        {
            return Interlocked.Increment(ref UniqueIndex);
        }

        // Instead of generating a class and method for an empty code body, return this null-handler
        // instead.
        private static void NullHandler(SorthInterpreter _)
        {
        }


        // Generate a static class and word handler for the given bytecode.
        public static WordHandler GenerateHandler(SorthInterpreter interpreter,
                                                  string name,
                                                  List<ByteCode> code,
                                                  bool with_context_handling = false)
        {
            // Check to see if there's anything to generate in the first place.
            if (code.Count == 0)
            {
                return NullHandler;
            }

            // Create a new static class to hold the newly generated word.
            var type_builder = Module.DefineType($"{name}_Word{Index()}",
                           TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract);


            // Define a new static method and generate the CIL for it.
            var interpreter_type = typeof(SorthInterpreter);
            var method_builder = type_builder.DefineMethod("Handler",
                                                  MethodAttributes.Public | MethodAttributes.Static,
                                                  typeof(void),
                                                  new Type[] { interpreter_type });
            var generator = method_builder.GetILGenerator();
            CompileInfo compile_info;

            // Generate the code for the user function.
            if (with_context_handling)
            {
                compile_info = GenerateContextualUserFunction(interpreter,
                                                              interpreter_type,
                                                              type_builder,
                                                              code,
                                                              generator);
            }
            else
            {
                compile_info = GenerateUserFunction(interpreter,
                                                    interpreter_type,
                                                    type_builder,
                                                    code,
                                                    generator);
            }

            // Finalize the new class and method.
            Type new_class = type_builder.CreateType();

            // Populate the constant fields with their values.
            foreach (var constant in compile_info.Constants)
            {
                var field = new_class.GetField(constant.Item1);

                if (field != null)
                {
                    field.SetValue(null, constant.Item2);
                }
                else
                {
                    interpreter.ThrowError(
                                       $"Internal error, missing constant field {constant.Item1}.");
                }
            }

            // Load up the location array.
            var locations_field = new_class.GetField(compile_info.LocationArray.Name);

            if (locations_field != null)
            {
                locations_field.SetValue(null, compile_info.Locations.ToArray());
            }

            // Get the info for the new method.
            MethodInfo? handler_info = new_class.GetMethod("Handler");

            // Make sure that this was successful, and create a delegate that can call our generated
            // method.
            if (handler_info != null)
            {
                return (WordHandler)Delegate.CreateDelegate(typeof(WordHandler), handler_info);
            }
            else
            {
                interpreter.ThrowError("Could not access generated word handler.");
            }

            // It looks like we failed, so the exception should be thrown and this code should be
            // unreachable.
            return NullHandler;
        }

        // Translate our bytecode into CIL for execution.
        private static CompileInfo GenerateUserFunction(SorthInterpreter interpreter,
                                                        Type interpreter_type,
                                                        TypeBuilder new_type,
                                                        List<ByteCode> code,
                                                        ILGenerator generator)
        {
            // The actual user code...
            var info = GenerateUserCode(interpreter,
                                        interpreter_type,
                                        new_type,
                                        code,
                                        generator);

            // Return to caller.
            generator.Emit(OpCodes.Ret);

            return info;
        }

        // Generate a function with wrapper code that makes sure to call MarkContext and
        // ReleaseContext.
        private static CompileInfo GenerateContextualUserFunction(SorthInterpreter interpreter,
                                                                  Type interpreter_type,
                                                                  TypeBuilder new_type,
                                                                  List<ByteCode> code,
                                                                  ILGenerator generator)
        {
            var mark_context = GetMethod(interpreter_type, "MarkContext");
            var release_context = GetMethod(interpreter_type, "ReleaseContext");

            // Mark the context for all our local variables.
            var base_end_label = generator.DefineLabel();

            generator.BeginExceptionBlock();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, mark_context);

            // The actual user code...
            var info = GenerateUserCode(interpreter,
                                        interpreter_type,
                                        new_type,
                                        code,
                                        generator);

            // All done, release the context and jump to the end.
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, release_context);

            generator.Emit(OpCodes.Leave_S, base_end_label);

            // If any exceptions occur, make sure that the context is still released.  Then rethrow
            // the original exception.
            generator.BeginCatchBlock(typeof(Exception));

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, release_context);

            generator.Emit(OpCodes.Rethrow);

            generator.EndExceptionBlock();
            generator.MarkLabel(base_end_label);

            // Return to caller.
            generator.Emit(OpCodes.Ret);

            return info;
        }

        private static CompileInfo GenerateUserCode(SorthInterpreter interpreter,
                                                    Type interpreter_type,
                                                    TypeBuilder new_type,
                                                    List<ByteCode> code,
                                                    ILGenerator generator)
        {
            var constant_index = 0;
            var constant_field_list = new List<( string, Value )>();

            FieldBuilder location_array = new_type.DefineField("Locations",
                                                   typeof(Location[]),
                                                   FieldAttributes.Public | FieldAttributes.Static);
            List<Location> locations = new List<Location>(50);

            var labels = new Dictionary<int, Label>();
            var variables = new Dictionary<string, LocalBuilder>();
            var constants = new Dictionary<string, LocalBuilder>();
            var loop_markers = new Stack<(int, int)>();
            var catch_targets = new HashSet<int>();

            var var_handler = typeof(VariableHandler);
            var var_handler_ctor = GetConstructor(var_handler, new Type[] { typeof(long) });
            var var_handler_register = GetMethod(var_handler, "Register");
            var var_handler_index = GetField(var_handler, "Index");

            var const_handler = typeof(ConstantHandler);
            var const_handler_ctor = GetConstructor(const_handler, new Type[] { typeof(Value) });
            var const_handler_register = GetMethod(const_handler, "Register");
            var const_handler_const = GetField(const_handler, "Constant");


            var value_type = typeof(Value);
            var default_val = GetMethod(value_type, "Default");
            var as_integer = GetMethod(value_type, "AsInteger");
            var as_boolean = GetMethod(value_type, "AsBoolean");
            var from_long = GetMethod(value_type, "From", new[] { typeof(long) });
            var from_double = GetMethod(value_type, "From", new[] { typeof(double) });
            var from_string= GetMethod(value_type, "From", new[] { typeof(string) });
            var from_bool = GetMethod(value_type, "From", new[] { typeof(bool) });
            var value_clone = GetMethod(value_type, "Clone");

            var list_type = typeof(ContextualList<Value>);
            var insert = GetMethod(list_type, "Insert");
            var get_item = GetMethod(list_type, "get_Item");
            var set_item = GetMethod(list_type, "set_Item");

            var push = GetMethod(interpreter_type, "Push");
            var pop = GetMethod(interpreter_type, "Pop");
            var get_variables = GetMethod(interpreter_type, "get_Variables");
            var find_word = GetMethod(interpreter_type, "FindWord", new[] { typeof(string) });
            var throw_error = GetMethod(interpreter_type, "ThrowError");
            var execute_word_index = GetMethod(interpreter_type,
                                               "ExecuteWord",
                                               new[] { typeof(long) });
            var execute_word_name = GetMethod(interpreter_type,
                                              "ExecuteWord",
                                              new[] { typeof(string) });

            var set_location = GetPropertySetter(interpreter_type, "CurrentLocation");

            var found_word_tuple = typeof(( bool, Word? ));
            var item_1 = GetField(found_word_tuple, "Item1");
            var item_2 = GetField(found_word_tuple, "Item2");

            var exception = typeof(Exception);
            var message = GetMethod(exception, "get_Message");

            LocalBuilder? found_word = null;
            LocalBuilder? exception_value = null;

            var location_type = typeof(Location);
            var location_ctr = GetConstructor(typeof(Location?), new Type[] { location_type });

            // Take a first pass through the code.
            for (int i = 0; i < code.Count; ++i)
            {
                switch (code[i].id)
                {
                    case ByteCode.Id.DefVariable:
                        {
                            var local = generator.DeclareLocal(var_handler);
                            var name = code[i].value.AsString(interpreter);

                            variables[name] = local;
                        }
                        break;

                    case ByteCode.Id.DefConstant:
                        {
                            var local = generator.DeclareLocal(const_handler);
                            var name = code[i].value.AsString(interpreter);

                            constants[name] = local;
                        }
                        break;

                    case ByteCode.Id.JumpTarget:
                        labels[i] = generator.DefineLabel();
                        break;
                }
            }

            // Take a second pass to generate the user code.
            for (int i = 0; i < code.Count; ++i)
            {
                var location = code[i].location;

                if (location != null)
                {
                    locations.Add(location.Value);

                    // interpreter.CurrentLocation = Locations[index];
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldsfld, location_array);
                    generator.Emit(OpCodes.Ldc_I4, locations.Count - 1);
                    generator.Emit(OpCodes.Ldelem, typeof(Location));
                    generator.Emit(OpCodes.Newobj, location_ctr);
                    generator.Emit(OpCodes.Callvirt, set_location);
                }

                switch (code[i].id)
                {
                    case ByteCode.Id.DefVariable:
                        {
                            var name = code[i].value.AsString(interpreter);

                            // var var_index = interpreter.Variables.Insert(Value.Default());
                            // var var_handler = new VariableHandler(var_index);
                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Callvirt, get_variables);
                            generator.Emit(OpCodes.Call, default_val);
                            generator.Emit(OpCodes.Callvirt, insert);

                            generator.Emit(OpCodes.Newobj, var_handler_ctor);
                            generator.Emit(OpCodes.Stloc, variables[name]);

                            // var_handler.Register(interpreter, name);
                            generator.Emit(OpCodes.Ldloc, variables[name]);
                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Ldstr, name);
                            generator.Emit(OpCodes.Callvirt, var_handler_register);
                        }
                        break;

                    case ByteCode.Id.DefConstant:
                        {
                            var name = code[i].value.AsString(interpreter);

                            // var const_handler = new ConstantHandler(interpreter.Pop());
                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Callvirt, pop);
                            generator.Emit(OpCodes.Newobj, const_handler_ctor);
                            generator.Emit(OpCodes.Stloc, constants[name]);

                            // const_handler.Register(interpreter, name);
                            generator.Emit(OpCodes.Ldloc, constants[name]);
                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Ldstr, name);
                            generator.Emit(OpCodes.Callvirt, const_handler_register);
                        }
                        break;

                    case ByteCode.Id.ReadVariable:
                        // var index = (int)interpreter.Pop().AsInteger(interpreter);
                        // interpreter.Push(interpreter.Variables[index]);
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Callvirt, get_variables);
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Callvirt, pop);
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Callvirt, as_integer);
                        generator.Emit(OpCodes.Conv_I4);
                        generator.Emit(OpCodes.Callvirt, get_item);
                        generator.Emit(OpCodes.Callvirt, push);
                        break;

                    case ByteCode.Id.WriteVariable:
                        // var index = (int)interpreter.Pop().AsInteger(interpreter);
                        // interpreter.Variables[index] = interpreter.Pop();
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Callvirt, get_variables);

                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Callvirt, pop);
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Callvirt, as_integer);
                        generator.Emit(OpCodes.Conv_I4);

                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Callvirt, pop);
                        generator.Emit(OpCodes.Callvirt, set_item);
                        break;

                    case ByteCode.Id.Execute:
                        {
                            var op_param = code[i].value;

                            if (op_param.IsNumeric())
                            {
                                var index = op_param.AsInteger(interpreter);

                                // interpreter.ExecuteWord(index);
                                generator.Emit(OpCodes.Ldarg_0);
                                generator.Emit(OpCodes.Ldc_I4, index);
                                generator.Emit(OpCodes.Callvirt, execute_word_index);
                            }
                            else if (op_param.IsString())
                            {
                                var name = op_param.AsString(interpreter);
                                LocalBuilder? local;

                                if (constants.TryGetValue(name, out local))
                                {
                                    // interpreter.Push(const_handler.Constant);
                                    generator.Emit(OpCodes.Ldarg_0);
                                    generator.Emit(OpCodes.Ldloc, local);
                                    generator.Emit(OpCodes.Ldfld, const_handler_const);
                                    generator.Emit(OpCodes.Callvirt, push);
                                }
                                else if (variables.TryGetValue(name, out local))
                                {
                                    // interpreter.Push(Value.From(var_handler.Index));
                                    generator.Emit(OpCodes.Ldarg_0);
                                    generator.Emit(OpCodes.Ldloc, local);
                                    generator.Emit(OpCodes.Ldfld, var_handler_index);
                                    generator.Emit(OpCodes.Call, from_long);
                                    generator.Emit(OpCodes.Callvirt, push);
                                }
                                else
                                {
                                    var ( found, word_info ) = interpreter.FindWord(name);

                                    if (found && (word_info != null))
                                    {
                                        var handler_index = word_info.Value.handler_index;

                                        // interpreter.ExecuteWord(handler_index);
                                        generator.Emit(OpCodes.Ldarg_0);
                                        generator.Emit(OpCodes.Ldc_I4, handler_index);
                                        generator.Emit(OpCodes.Callvirt, execute_word_index);
                                    }
                                    else
                                    {
                                        // interpreter.ExecuteWord(name);
                                        generator.Emit(OpCodes.Ldarg_0);
                                        generator.Emit(OpCodes.Ldstr, name);
                                        generator.Emit(OpCodes.Callvirt, execute_word_name);
                                    }
                                }
                            }
                            else
                            {
                                interpreter.ThrowError(
                                                     $"Unsupported execute value type {op_param}.");
                            }
                        }
                        break;

                    case ByteCode.Id.WordIndex:
                        {
                            var name = code[i].value.AsString(interpreter);
                            var found_label = generator.DefineLabel();

                            if (found_word == null)
                            {
                                found_word = generator.DeclareLocal(found_word_tuple);
                            }

                            /*
                            ( bool, Word? ) found_word = interpreter.FindWord(name);

                            if (!found_word.Item1)
                            {
                                interpreter.ThrowError("Word, name, not found.");
                            }

                            interpreter.Push(Value.From(found_word.Item2.Value.handler_index));
                            */

                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Ldstr, name);
                            generator.Emit(OpCodes.Callvirt, find_word);
                            generator.Emit(OpCodes.Stloc, found_word);

                            generator.Emit(OpCodes.Ldloc, found_word);
                            generator.Emit(OpCodes.Ldfld, item_1);
                            generator.Emit(OpCodes.Ldc_I4_0);
                            generator.Emit(OpCodes.Ceq);
                            generator.Emit(OpCodes.Brfalse, found_label);

                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Ldstr, $"Word, {name}, not found.");
                            generator.Emit(OpCodes.Callvirt, throw_error);

                            generator.MarkLabel(found_label);

                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Ldloc, found_word);
                            generator.Emit(OpCodes.Ldfld, item_2);
                            generator.Emit(OpCodes.Conv_I8);
                            generator.Emit(OpCodes.Call, from_long);
                            generator.Emit(OpCodes.Callvirt, push);
                        }
                        break;

                    case ByteCode.Id.WordExists:
                        {
                            // interpreter.Push(Value.From(interpreter.FindWord(name).Item1));
                            var name = code[i].value.AsString(interpreter);

                            generator.Emit(OpCodes.Ldarg_0);

                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Ldstr, name);
                            generator.Emit(OpCodes.Callvirt, find_word);
                            generator.Emit(OpCodes.Ldfld, item_1);
                            generator.Emit(OpCodes.Call, from_bool);

                            generator.Emit(OpCodes.Callvirt, push);
                        }
                        break;

                    case ByteCode.Id.PushConstantValue:
                        {
                            // interpreter.Push(Value.From(constant_value));
                            var value = code[i].value;

                            generator.Emit(OpCodes.Ldarg_0);

                            // Extract the raw value from the value object and generate code to push
                            // that onto the native stack.
                            if (value.IsInteger())
                            {
                                var int_val = value.AsInteger(interpreter);

                                generator.Emit(OpCodes.Ldc_I8, int_val);
                                generator.Emit(OpCodes.Call, from_long);
                            }
                            else if (value.IsDouble())
                            {
                                var double_val = value.AsDouble(interpreter);

                                generator.Emit(OpCodes.Ldc_R8, double_val);
                                generator.Emit(OpCodes.Call, from_double);
                            }
                            else if (value.IsString())
                            {
                                var string_val = value.AsString(interpreter);

                                generator.Emit(OpCodes.Ldstr, string_val);
                                generator.Emit(OpCodes.Call, from_string);
                            }
                            else
                            {
                                // It looks like it's a more complex type.  Instead of generating
                                // code to recreate the value object we'll store it in a field in
                                // the generated class and push a clone of it onto the data stack.

                                // First create a field to hold the live value.
                                var field_name = $"ConstantValue{++constant_index}";
                                var field_ref = new_type.DefineField(
                                                   field_name,
                                                   value_type,
                                                   FieldAttributes.Public | FieldAttributes.Static);

                                // Save the value for later assignment once the type is created.
                                constant_field_list.Add(( field_name, value ));

                                // interpreter.Push(ConstantValue1.Clone());
                                generator.Emit(OpCodes.Ldsfld, field_ref);
                                generator.Emit(OpCodes.Callvirt, value_clone);
                            }

                            // Pop the constant value from the native stack and push it onto the
                            // Forth data stack.
                            generator.Emit(OpCodes.Callvirt, push);
                        }
                        break;

                    case ByteCode.Id.MarkLoopExit:
                        {
                            // Mark the loop's start and end instructions.
                            var index = (int)(i + code[i].value.AsInteger(interpreter));
                            loop_markers.Push((i + 1, index));
                        }
                        break;

                    case ByteCode.Id.UnmarkLoopExit:
                        // Clear the loop markers.
                        loop_markers.Pop();
                        break;

                    case ByteCode.Id.MarkCatch:
                        // try { ...
                        generator.BeginExceptionBlock();

                        // Capture the jump target as an end of catch block.
                        catch_targets.Add((int)(i + code[i].value.AsInteger(interpreter)));
                        break;

                    case ByteCode.Id.UnmarkCatch:
                        {
                            // If we haven't declared a local for holding the exception object,
                            // declare it now.
                            if (exception_value == null)
                            {
                                exception_value = generator.DeclareLocal(typeof(ScriptError));
                            }

                            // An unmark catch is always matched with a jump instruction to bypass
                            // the catch block on successful execution.  So grab that instruction
                            // and use it's target in the generated code.
                            ++i;

                            var jump_op = code[i];
                            var jump_op_target = (int)(i + jump_op.value.AsInteger(interpreter));

                            // catch (ScriptError error) { ...
                            generator.Emit(OpCodes.Leave, labels[jump_op_target]);
                            generator.BeginCatchBlock(typeof(ScriptError));

                            // Exception class is on the stack, save it to a local.
                            generator.Emit(OpCodes.Stloc, exception_value);

                            // interpreter.Push(Value.From(exception_value.Message));
                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Ldloc, exception_value);
                            generator.Emit(OpCodes.Callvirt, message);
                            generator.Emit(OpCodes.Call, from_string);
                            generator.Emit(OpCodes.Callvirt, push);
                        }
                        break;

                    case ByteCode.Id.Jump:
                        {
                            // Convert the bytecode's relative jump into a absolute jump.
                            var index = (int)(i + code[i].value.AsInteger(interpreter));
                            generator.Emit(OpCodes.Br, labels[index]);
                        }
                        break;

                    case ByteCode.Id.JumpIfZero:
                        {
                            // Convert the bytecode's relative jump into a absolute jump.
                            var index = (int)(i + code[i].value.AsInteger(interpreter));

                            // if (interpreter.Pop().AsBoolean(interpreter)) { ...
                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Callvirt, pop);
                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Callvirt, as_boolean);
                            generator.Emit(OpCodes.Brfalse, labels[index]);
                        }
                        break;

                    case ByteCode.Id.JumpIfNotZero:
                        {
                            // Convert the bytecode's relative jump into a absolute jump.
                            var index = (int)(i + code[i].value.AsInteger(interpreter));

                            // if (!interpreter.Pop().AsBoolean(interpreter)) { ...
                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Callvirt, pop);
                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Callvirt, as_boolean);
                            generator.Emit(OpCodes.Brtrue, labels[index]);
                        }
                        break;

                    case ByteCode.Id.JumpLoopStart:
                        // Jump directly to the loop's start instruction.
                        generator.Emit(OpCodes.Br, labels[loop_markers.Peek().Item1]);
                        break;

                    case ByteCode.Id.JumpLoopExit:
                        // Jump directly to the loop's end instruction.
                        generator.Emit(OpCodes.Br, labels[loop_markers.Peek().Item2]);
                        break;

                    case ByteCode.Id.JumpTarget:
                        // If this jump target is the end of the catch block, mark it now.
                        if (catch_targets.TryGetValue(i, out _))
                        {
                            generator.EndExceptionBlock();
                        }

                        // Assign the label it's location.
                        generator.MarkLabel(labels[i]);
                        break;
                }
            }

            return new CompileInfo
                {
                    Constants = constant_field_list,

                    LocationArray = location_array,
                    Locations = locations
                };
        }

        // Helper methods for accessing type information.
        private static ConstructorInfo GetConstructor(Type type, Type[] param_types)
        {
            var result = type.GetConstructor(param_types);

            if (result == null)
            {
                throw new ScriptError($"Internal error, could not access {type.Name} constructor.");
            }

            return result;
        }

        private static MethodInfo GetMethod(Type type, string name)
        {
            var result = type.GetMethod(name);

            if (result == null)
            {
                throw new ScriptError(
                                    $"Internal error, could not access {type.Name} method {name}.");
            }

            return result;
        }

        private static MethodInfo GetMethod(Type type, string name, Type[] param_types)
        {
            var result = type.GetMethod(name, param_types);

            if (result == null)
            {
                throw new ScriptError(
                                    $"Internal error, could not access {type.Name} method {name}.");
            }

            return result;

        }

        private static FieldInfo GetField(Type type, string name)
        {
            var result = type.GetField(name);

            if (result == null)
            {
                throw new ScriptError($"Internal error, could not access {type.Name} field {name}.");
            }

            return result;
        }

        private static MethodInfo GetPropertySetter(Type type, string name)
        {
            var property = type.GetProperty(name);

            if (property != null)
            {
                var setter = property.GetSetMethod();

                if (setter != null)
                {
                    return setter;
                }
                else
                {
                    throw new ScriptError(
                           $"Internal error, could not access {type.Name} property {name} setter.");
                }
            }
            else
            {
                throw new ScriptError(
                                  $"Internal error, could not access {type.Name} property {name}.");
            }
        }
    }

}
