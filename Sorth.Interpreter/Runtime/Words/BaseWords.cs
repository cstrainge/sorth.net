﻿
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Sorth.Interpreter.Language.Code;
using Sorth.Interpreter.Language.Source;
using Sorth.Interpreter.Runtime.DataStructures;



namespace Sorth.Interpreter.Runtime.Words
{

    static class Helper
    {
        public static void MethodUnimplemented(SorthInterpreter interpreter)
        {
            var stack_trace = new StackTrace();
            var stack_frame = stack_trace.GetFrame(1);

            if (stack_frame != null)
            {
                var method = stack_frame.GetMethod();

                if (method != null)
                {
                    interpreter.ThrowError($"Word {method.Name} is unimplemented.");
                }
            }

            interpreter.ThrowError("Word is unimplemented.");
        }

        public static Token GetNextToken(SorthInterpreter interpreter)
        {
            var current_token = ++interpreter.Constructor.CurrentToken;

            if (current_token >= interpreter.Constructor.Tokens.Count)
            {
                interpreter.ThrowError("Trying to read past end of token stream.");
            }

            return interpreter.Constructor.Tokens[current_token];
        }

        public static void StringOrNumericOp(SorthInterpreter interpreter,
                                              Func<double, double, double> double_op,
                                              Func<long, long, long> long_op,
                                              Func<string, string, string> string_op)
        {
            var b = interpreter.Pop();
            var a = interpreter.Pop();

            Value result = Value.Default();

            if (Value.EitherIsString(a, b))
            {
                var str_a = a.AsString(interpreter);
                var str_b = b.AsString(interpreter);

                result = Value.From(string_op(str_a, str_b));
            }
            else if (Value.EitherIsDouble(a, b))
            {
                var double_a = a.AsDouble(interpreter);
                var double_b = b.AsDouble(interpreter);

                result = Value.From(double_op(double_a, double_b));
            }
            else if (Value.EitherIsNumeric(a, b))
            {
                var int_a = a.AsInteger(interpreter);
                var int_b = b.AsInteger(interpreter);

                result = Value.From(long_op(int_a, int_b));
            }
            else
            {
                interpreter.ThrowError("Value types are not compatible with + operation.");
            }

            interpreter.Push(result);
        }

        public static void ComparisonOp(SorthInterpreter interpreter,
                                        Func<double, double, bool> double_op,
                                        Func<long, long, bool> long_op,
                                        Func<string, string, bool> string_op)
        {
            var b = interpreter.Pop();
            var a = interpreter.Pop();

            Value result = Value.Default();

            if (Value.EitherIsDouble(a, b))
            {
                var double_a = a.AsDouble(interpreter);
                var double_b = b.AsDouble(interpreter);

                result = Value.From(double_op(double_a, double_b));
            }
            else if (Value.EitherIsNumeric(a, b))
            {
                var int_a = a.AsInteger(interpreter);
                var int_b = b.AsInteger(interpreter);

                result = Value.From(long_op(int_a, int_b));
            }
            else if (Value.EitherIsString(a, b))
            {
                var str_a = a.AsString(interpreter);
                var str_b = b.AsString(interpreter);

                result = Value.From(string_op(str_a, str_b));
            }
            else
            {
                interpreter.ThrowError("Value types are not compatible with + operation.");
            }

            interpreter.Push(result);
        }

        public static void MathOp(SorthInterpreter interpreter,
                                   Func<double, double, double> double_op,
                                   Func<long, long, long> long_op)
        {
            var b = interpreter.Pop();
            var a = interpreter.Pop();

            Value result = Value.Default();

            if (Value.EitherIsDouble(a, b))
            {
                var double_a = a.AsDouble(interpreter);
                var double_b = b.AsDouble(interpreter);

                result = Value.From(double_op(double_a, double_b));
            }
            else if (Value.EitherIsNumeric(a, b))
            {
                var int_a = a.AsInteger(interpreter);
                var int_b = b.AsInteger(interpreter);

                result = Value.From(long_op(int_a, int_b));
            }
            else
            {
                interpreter.ThrowError("Value type not compatable with math operator.");
            }

            interpreter.Push(result);
        }

        public static void LogicOp(SorthInterpreter interpreter, Func<bool, bool, bool> op)
        {
            var b = interpreter.Pop().AsBoolean(interpreter);
            var a = interpreter.Pop().AsBoolean(interpreter);

            var result = op(a, b);

            interpreter.Push(Value.From(result));
        }

        public static void LogicBitOp(SorthInterpreter interpreter, Func<long, long, long> op)
        {
            var b = interpreter.Pop().AsInteger(interpreter);
            var a = interpreter.Pop().AsInteger(interpreter);

            var result = op(a, b);

            interpreter.Push(Value.From(result));
        }

    }


    static class SorthWords
    {
        private static void WordReset(SorthInterpreter interpreter)
        {
            interpreter.Reset();
        }

        private static void WordInclude(SorthInterpreter interpreter)
        {
            var path = interpreter.Pop().AsString(interpreter);
            interpreter.ProcessSourceFile(path);
        }

        private static void WordIncludeIm(SorthInterpreter interpreter)
        {
            var token = Helper.GetNextToken(interpreter);
            interpreter.ProcessSourceFile(token.Text);
        }

        private static void WordPrintStack(SorthInterpreter interpreter)
        {
            foreach (var item in interpreter.Stack)
            {
                if (item.IsString())
                {
                    Console.WriteLine(Value.Stringify(item.AsString(interpreter)));
                }
                else
                {
                    Console.WriteLine(item);
                }
            }
        }

        private static void WordPrintValue(SorthInterpreter interpreter)
        {
            var value = interpreter.Pop();
            Console.Write(value);
        }

        private static void WordValueNewline(SorthInterpreter interpreter)
        {
            var value = interpreter.Pop();
            Console.WriteLine(value);
        }

        private static void WordPrintDictionary(SorthInterpreter interpreter)
        {
            var words = interpreter.Words;
            int max_size = 0;
            int count = 0;

            foreach (var word in words)
            {
                if (!word.Value.is_hidden)
                {
                    if (word.Key.Length > max_size)
                    {
                        max_size = word.Key.Length;
                    }

                    count++;
                }
            }

            Console.WriteLine($"There are {count} words defined.");

            var format_str = "{0,-" + max_size + "}  {1,4}  {2,9}  --  {3}";

            foreach (var word in words)
            {
                if (!word.Value.is_hidden)
                {
                    var text = string.Format(format_str,
                                             word.Key,
                                             word.Value.handler_index,
                                             word.Value.is_immediate ? "immediate" : "",
                                             word.Value.description);

                    Console.WriteLine(text);
                }
            }
        }

        private static void WordSorthVersion(SorthInterpreter interpreter)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;

            interpreter.Push(Value.From($"{version}.net"));
        }

        private static void WordSorthMaxDepth(SorthInterpreter interpreter)
        {
            var depth = interpreter.MaxDepth;

            interpreter.Push(Value.From(depth));
        }

        private static void WordSorthMemory(SorthInterpreter interpreter)
        {
            var process = Process.GetCurrentProcess();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();

            interpreter.Push(Value.From(process.WorkingSet64));
        }

        private static void WordThrow(SorthInterpreter interpreter)
        {
            interpreter.ThrowError(interpreter.Pop().AsString(interpreter));
        }

        private static void WordThreadShow(SorthInterpreter interpreter)
        {
            var list = interpreter.Threads;

            foreach (var item in list)
            {
                var name = interpreter.Handlers[item.Word.handler_index].name;

                Console.WriteLine($"Thread {item.WordThread.ManagedThreadId} " +
                                  $"word {name}, ({item.Word.handler_index}) " +
                                  $"I/O queues ({item.Inputs.Count}/{item.Outputs.Count}) " +
                                  (item.WordThread.IsAlive ? "is running." : "has completed."));
            }
        }

        private static void WordThreadNew(SorthInterpreter interpreter)
        {
            var name = interpreter.Pop().AsString(interpreter);
            var ( found, word ) = interpreter.FindWord(name);

            if (found && word != null)
            {
                var id = interpreter.ExecuteWordThreaded(word.Value);
                interpreter.Push(Value.From(id));
            }
            else
            {
                interpreter.ThrowError($"Could not start thread, word {name} not found.");
            }
        }

        private static void WordThreadPushTo(SorthInterpreter interpreter)
        {
            var id = (int)interpreter.Pop().AsInteger(interpreter);
            var value = interpreter.Pop();

            interpreter.ThreadPushInput(id, value);
        }

        private static void WordThreadPopFrom(SorthInterpreter interpreter)
        {
            var id = (int)interpreter.Pop().AsInteger(interpreter);
            var value = interpreter.ThreadPopOutput(id);

            interpreter.Push(value);
        }

        private static void WordThreadPush(SorthInterpreter interpreter)
        {
            var value = interpreter.Pop();
            var id = Thread.CurrentThread.ManagedThreadId;

            interpreter.ThreadPushOutput(id, value);
        }

        private static void WordThreadPop(SorthInterpreter interpreter)
        {
            var id = Thread.CurrentThread.ManagedThreadId;

            interpreter.Push(interpreter.ThreadPopInput(id));
        }


        public static void Register(SorthInterpreter interpreter)
        {
            interpreter.AddWord("reset", WordReset,
                "Reset the interpreter to it's default state.",
                " -- ");

            interpreter.AddWord("include", WordInclude,
                "Include and execute another source file.",
                "source_path -- ");


            interpreter.AddWord("[include]", WordIncludeIm,
                "Include and execute another source file.",
                "[include] file/to/include.f",
                true);

            interpreter.AddWord(".", WordPrintValue,
                "Print out the value at the top of the stack.",
                " -- ");

            interpreter.AddWord(".cr", WordValueNewline,
                "Print out the value at the top of the stack with a new line.",
                " -- ");

            interpreter.AddWord(".s", WordPrintStack,
                "Print out the data stack without changing it.",
                " -- ");

            interpreter.AddWord(".w", WordPrintDictionary,
                "Print out the current word dictionary.",
                " -- ");

            interpreter.AddWord("sorth.version", WordSorthVersion,
                "Get the current version of the interpreter.",
                " -- version_string");

            interpreter.AddWord("sorth.max_depth", WordSorthMaxDepth,
                "Get the max depth of the sorth data stack.",
                " -- depth");

            interpreter.AddWord("sorth.memory", WordSorthMemory,
                "Get the size of the process's working set.",
                " -- size");

            interpreter.AddWord("throw", WordThrow,
                "Throw an exception with the given message.",
                "message -- ");

            interpreter.AddWord(".t", WordThreadShow,
                "Print out the list of interpreter threads.",
                " -- ");

            interpreter.AddWord("thread.new", WordThreadNew,
                "Create a new thread and run the specified word and return the new thread id.",
                "word-index -- thread-id");

            interpreter.AddWord("thread.push-to", WordThreadPushTo,
                "Push the top value to another thread's input stack.",
                "value thread-id -- ");

            interpreter.AddWord("thread.pop-from", WordThreadPopFrom,
                "Pop a value off of the threads input queue, block if there's nothing available.",
                "thread-id -- input-value");

            interpreter.AddWord("thread.push", WordThreadPush,
                "Push the top value onto the thread's output queue.",
                "output-value -- ");

            interpreter.AddWord("thread.pop", WordThreadPop,
                "Pop from another thread's output stack and push onto the local data stack.",
                " -- value");
        }
    }


    static class StackWords
    {
        private static void WordDup(SorthInterpreter interpreter)
        {
            var value = interpreter.Pop();

            interpreter.Push(value);
            interpreter.Push(value);
        }

        private static void WordDrop(SorthInterpreter interpreter)
        {
            interpreter.Pop();
        }

        private static void WordSwap(SorthInterpreter interpreter)
        {
            var a = interpreter.Pop();
            var b = interpreter.Pop();

            interpreter.Push(a);
            interpreter.Push(b);
        }

        private static void WordOver(SorthInterpreter interpreter)
        {
            var a = interpreter.Pop();
            var b = interpreter.Pop();

            interpreter.Push(a);
            interpreter.Push(b);
            interpreter.Push(a);
        }

        private static void WordRot(SorthInterpreter interpreter)
        {
            var c = interpreter.Pop();
            var b = interpreter.Pop();
            var a = interpreter.Pop();

            interpreter.Push(c);
            interpreter.Push(a);
            interpreter.Push(b);
        }

        private static void WordStackDepth(SorthInterpreter interpreter)
        {
            interpreter.Push(Value.From(interpreter.Stack.Count));
        }

        private static void WordPick(SorthInterpreter interpreter)
        {
            var index = (int)interpreter.Pop().AsInteger(interpreter);
            var count = interpreter.Stack.Count;

            if ((index < 0) || (index >= count))
            {
                interpreter.ThrowError($"Index {index} is out of range of stack, {count}.");
            }

            interpreter.Push(interpreter.Pick(index));
        }

        private static void WordPushTo(SorthInterpreter interpreter)
        {
            var index = (int)interpreter.Pop().AsInteger(interpreter);
            var count = interpreter.Stack.Count;

            if ((index < 0) || (index >= count))
            {
                interpreter.ThrowError($"Index {index} is out of range of stack, {count}.");
            }

            interpreter.PushTo(index);
        }


        public static void Register(SorthInterpreter interpreter)
        {
            interpreter.AddWord("dup", WordDup,
                "Duplicate the top value on the data stack.",
                "value -- value value");

            interpreter.AddWord("drop", WordDrop,
                "Discard the top value on the data stack.",
                "value -- ");

            interpreter.AddWord("swap", WordSwap,
                "Swap the top 2 values on the data stack.",
                "a b -- b a");

            interpreter.AddWord("over", WordOver,
                "Make a copy of the top value and place the copy under the second.",
                "a b -- b a b");

            interpreter.AddWord("rot", WordRot,
                "Rotate the top 3 values on the stack.",
                "a b c -- c a b");

            interpreter.AddWord("stack.depth", WordStackDepth,
                "Get the current depth of the stack.",
                " -- depth");

            interpreter.AddWord("pick", WordPick,
                "Pick the value n locations down in the stack and push it on the top.",
                "n -- value");

            interpreter.AddWord("push-to", WordPushTo,
                "Pop the top value and push it back into the stack a position from the top.",
                "n -- <updated-stack>>");
        }
    }


    static class ConstantWords
    {
        public static void Register(SorthInterpreter interpreter)
        {
            interpreter.AddWord("exit_success", (interpreter) => interpreter.Push(Value.From(0)),
                "Constant value for a process success exit code.",
                " -- success");

            interpreter.AddWord("exit_failure", (interpreter) => interpreter.Push(Value.From(1)),
                "Constant value for a process fail exit code.",
                " -- failure");

            interpreter.AddWord("true", (interpreter) => interpreter.Push(Value.From(true)),
                "Push the value true onto the data stack.",
                " -- true");

            interpreter.AddWord("false", (interpreter) => interpreter.Push(Value.From(false)),
                "Push the value false onto the data stack.",
                " -- false");
        }
    }


    static class ByteCodeWords
    {
        public static void InsertUserInstruction(SorthInterpreter interpreter,
                                                  ByteCode.Id id,
                                                  Value? value = null)
        {
            var constructor = interpreter.Constructor;
            var new_value = value ?? Value.Default();
            var instruction = new ByteCode(id, new_value, null);

            if (!constructor.UserIsInsertingAtBeginning)
            {
                constructor.Stack.Peek().ByteCode.Add(instruction);
            }
            else
            {
                constructor.Stack.Peek().ByteCode.Insert(0, instruction);
            }
        }


        private static void WordOpDefVariable(SorthInterpreter interpreter)
        {
            InsertUserInstruction(interpreter, ByteCode.Id.DefVariable, interpreter.Pop());
        }

        private static void WordOpDefConstant(SorthInterpreter interpreter)
        {
            InsertUserInstruction(interpreter, ByteCode.Id.DefConstant, interpreter.Pop());
        }

        private static void WordOpReadVariable(SorthInterpreter interpreter)
        {
            InsertUserInstruction(interpreter, ByteCode.Id.ReadVariable);
        }

        private static void WordOpWriteVariable(SorthInterpreter interpreter)
        {
            InsertUserInstruction(interpreter, ByteCode.Id.WriteVariable);
        }

        private static void WordOpExecute(SorthInterpreter interpreter)
        {
            InsertUserInstruction(interpreter, ByteCode.Id.Execute, interpreter.Pop());
        }

        private static void WordOpPushConstantValue(SorthInterpreter interpreter)
        {
            InsertUserInstruction(interpreter, ByteCode.Id.PushConstantValue, interpreter.Pop());
        }

        private static void WordMarkLoopExit(SorthInterpreter interpreter)
        {
            InsertUserInstruction(interpreter, ByteCode.Id.MarkLoopExit, interpreter.Pop());
        }

        private static void WordUnmarkLoopExit(SorthInterpreter interpreter)
        {
            InsertUserInstruction(interpreter, ByteCode.Id.UnmarkLoopExit);
        }

        private static void WordOpMarkCatch(SorthInterpreter interpreter)
        {
            InsertUserInstruction(interpreter, ByteCode.Id.MarkCatch, interpreter.Pop());
        }

        private static void WordOpUnmarkCatch(SorthInterpreter interpreter)
        {
            InsertUserInstruction(interpreter, ByteCode.Id.UnmarkCatch);
        }

        private static void WordOpJump(SorthInterpreter interpreter)
        {
            InsertUserInstruction(interpreter, ByteCode.Id.Jump, interpreter.Pop());
        }

        private static void WordOpJumpIfZero(SorthInterpreter interpreter)
        {
            InsertUserInstruction(interpreter, ByteCode.Id.JumpIfZero, interpreter.Pop());
        }

        private static void WordOpJumpIfNotZero(SorthInterpreter interpreter)
        {
            InsertUserInstruction(interpreter, ByteCode.Id.JumpIfNotZero, interpreter.Pop());
        }

        private static void WordJumpLoopStart(SorthInterpreter interpreter)
        {
            InsertUserInstruction(interpreter, ByteCode.Id.JumpLoopStart);
        }

        private static void WordJumpLoopExit(SorthInterpreter interpreter)
        {
            InsertUserInstruction(interpreter, ByteCode.Id.JumpLoopExit);
        }

        private static void WordOpJumpTarget(SorthInterpreter interpreter)
        {
            InsertUserInstruction(interpreter, ByteCode.Id.JumpTarget, interpreter.Pop());
        }


        private static void WordCodeNewBlock(SorthInterpreter interpreter)
        {
            interpreter.Constructor.Stack.Push(new Construction());
        }

        private static void WordCodeMergeStackBlock(SorthInterpreter interpreter)
        {
            var code = interpreter.Constructor.Stack.Peek().ByteCode;

            interpreter.Constructor.Stack.Pop();
            interpreter.Constructor.Stack.Peek().ByteCode.AddRange(code);
        }

        private static void WordCodePopStackBlock(SorthInterpreter interpreter)
        {
            var code = Value.From(interpreter.Constructor.Stack.Pop().ByteCode);
            interpreter.Push(code);
        }

        private static void WordCodePushStackBlock(SorthInterpreter interpreter)
        {
            var construction = new Construction();

            construction.ByteCode = interpreter.Pop().AsByteCode(interpreter);
            interpreter.Constructor.Stack.Push(construction);
        }

        private static void WordCodeStackBlockSize(SorthInterpreter interpreter)
        {
            var size = Value.From(interpreter.Constructor.Stack.Peek().ByteCode.Count);
            interpreter.Push(size);
        }

        private static void WordCodeResolveJumps(SorthInterpreter interpreter)
        {
            static bool IsJump(ByteCode code)
            {
                return    (code.id == ByteCode.Id.Jump)
                       || (code.id == ByteCode.Id.JumpIfZero)
                       || (code.id == ByteCode.Id.JumpIfNotZero)
                       || (code.id == ByteCode.Id.MarkLoopExit)
                       || (code.id == ByteCode.Id.MarkCatch);
            }

            var top_code = interpreter.Constructor.Stack.Peek().ByteCode;

            var jump_indices = new List<int>();
            var jump_targets = new Dictionary<string, int>();

            for (int i = 0; i < top_code.Count; ++i)
            {
                if (IsJump(top_code[i]))
                {
                    jump_indices.Add(i);
                }
                else if (   (top_code[i].id == ByteCode.Id.JumpTarget)
                         && (top_code[i].value.IsString()))
                {
                    jump_targets[top_code[i].value.AsString(interpreter)] = i;
                    top_code[i] = new ByteCode(ByteCode.Id.JumpTarget, Value.Default(), null);
                }
            }

            foreach (var jump_index in jump_indices)
            {
                var jump_op = top_code[jump_index];

                if (jump_op.value.IsString())
                {
                    var jump_name = jump_op.value.AsString(interpreter);
                    var target_index = jump_targets[jump_name];

                    jump_op.value = Value.From(target_index - jump_index);

                    top_code[jump_index] = jump_op;
                }
            }
        }

        private static void WordCodeCompileUntilWords(SorthInterpreter interpreter)
        {
            static ( bool, string ) IsOneOfWords(List<string> words, string match)
            {
                foreach (var word in words)
                {
                    if (word == match)
                    {
                        return ( true, word );
                    }
                }

                return ( false, "" );
            }

            var count = interpreter.Pop().AsInteger(interpreter);
            var word_list = new List<string>();

            for (int i = 0; i < count; ++i)
            {
                word_list.Add(interpreter.Pop().AsString(interpreter));
            }

            for (++interpreter.Constructor.CurrentToken;
                 interpreter.Constructor.CurrentToken < interpreter.Constructor.Tokens.Count;
                 ++interpreter.Constructor.CurrentToken)
            {
                var token = interpreter.Constructor.Tokens[interpreter.Constructor.CurrentToken];
                var ( found, word ) = IsOneOfWords(word_list, token.Text);

                if (found && token.Type == Token.TokenType.Word)
                {
                    interpreter.Push(Value.From(word));
                    return;
                }

                interpreter.Constructor.CompileToken(interpreter, token);
            }

            string message;

            if (count == 1)
            {
                message = $"Missing word, {word_list[0]} in source.";
            }
            else
            {
                message = "Missing matching word, expected one of [ ";

                foreach (var word in word_list)
                {
                    message += $"{word} ";
                }

                message += "].";
            }

            interpreter.ThrowError(message);
        }

        private static void WordCodeInsertAtFront(SorthInterpreter interpreter)
        {
            bool IsAtBeginning = interpreter.Pop().AsBoolean(interpreter);
            interpreter.Constructor.UserIsInsertingAtBeginning = IsAtBeginning;
        }

        private static void WordCodeExecuteSource(SorthInterpreter interpreter)
        {
            var code = interpreter.Pop().AsString(interpreter);
            interpreter.ProcessSource(code);
        }


        public static void Register(SorthInterpreter interpreter)
        {
            interpreter.AddWord("op.def_variable", WordOpDefVariable,
                "Insert this instruction into the byte stream.",
                "new-name -- ");

            interpreter.AddWord("op.def_constant", WordOpDefConstant,
                "Insert this instruction into the byte stream.",
                "new-name -- ");

            interpreter.AddWord("op.read_variable", WordOpReadVariable,
                "Insert this instruction into the byte stream.",
                " -- ");

            interpreter.AddWord("op.write_variable", WordOpWriteVariable,
                "Insert this instruction into the byte stream.",
                " -- ");

            interpreter.AddWord("op.execute", WordOpExecute,
                "Insert this instruction into the byte stream.",
                "index -- ");

            interpreter.AddWord("op.push_constant_value", WordOpPushConstantValue,
                "Insert this instruction into the byte stream.",
                "value -- ");

            interpreter.AddWord("op.mark_loop_exit", WordMarkLoopExit,
                "Insert this instruction into the byte stream.",
                "identifier -- ");

            interpreter.AddWord("op.unmark_loop_exit", WordUnmarkLoopExit,
                "Insert this instruction into the byte stream.",
                " -- ");

            interpreter.AddWord("op.mark_catch", WordOpMarkCatch,
                "Insert this instruction into the byte stream.",
                "identifier -- ");

            interpreter.AddWord("op.unmark_catch", WordOpUnmarkCatch,
                "Insert this instruction into the byte stream.",
                " -- ");

            interpreter.AddWord("op.jump", WordOpJump,
                "Insert this instruction into the byte stream.",
                "identifier -- ");

            interpreter.AddWord("op.jump_if_zero", WordOpJumpIfZero,
                "Insert this instruction into the byte stream.",
                "identifier -- ");

            interpreter.AddWord("op.jump_if_not_zero", WordOpJumpIfNotZero,
                "Insert this instruction into the byte stream.",
                "identifier -- ");

            interpreter.AddWord("op.jump_loop_start", WordJumpLoopStart,
                "Insert this instruction into the byte stream.",
                " -- ");

            interpreter.AddWord("op.jump_loop_exit", WordJumpLoopExit,
                "Insert this instruction into the byte stream.",
                " -- ");

            interpreter.AddWord("op.jump_target", WordOpJumpTarget,
                "Insert this instruction into the byte stream.",
                "identifier -- ");


            interpreter.AddWord("code.new_block", WordCodeNewBlock,
                "Create a new sub-block on the code generation stack.",
                " -- ");

            interpreter.AddWord("code.merge_stack_block", WordCodeMergeStackBlock,
                "Merge the top code block into the one below.",
                " -- ");

            interpreter.AddWord("code.pop_stack_block", WordCodePopStackBlock,
                "Pop a code block off of the code stack and onto the data stack.",
                " -- code_block");

            interpreter.AddWord("code.push_stack_block", WordCodePushStackBlock,
                "Pop a block from the data stack and back onto the code stack.",
                "code_block -- ");

            interpreter.AddWord("code.stack_block_size@", WordCodeStackBlockSize,
                "Read the size of the code block at the top of the stack.",
                " -- code_size");

            interpreter.AddWord("code.resolve_jumps", WordCodeResolveJumps,
                "Resolve all of the jumps in the top code block.",
                " -- ");

            interpreter.AddWord("code.compile_until_words", WordCodeCompileUntilWords,
                "Compile words until one of the given words is found.",
                "words... word_count -- found_word");

            interpreter.AddWord("code.insert_at_front", WordCodeInsertAtFront,
                "When true new instructions are added beginning of the block.",
                "bool -- ");


            interpreter.AddWord("code.execute_source", WordCodeExecuteSource,
                "Interpret and execute a string like it is source code.",
                "string_to_execute -- ???");
        }
    }


    static class WordWords
    {
        private static void WordWord(SorthInterpreter interpreter)
        {
            var token = Helper.GetNextToken(interpreter);
            interpreter.Push(Value.From(token));
        }

        private static void WordGetWordTable(SorthInterpreter interpreter)
        {
            var dictionary = interpreter.Words;
            var result = new Dictionary<Value, Value>(dictionary.Count);

            foreach (var entry in dictionary)
            {
                result[Value.From(entry.Key)] = DataStructureWords.WordDataFromWord(entry.Key,
                                                                                    entry.Value);
            }

            interpreter.Push(Value.From(result));
        }

        private static void WordWordIndex(SorthInterpreter interpreter)
        {
            var name = Helper.GetNextToken(interpreter).Text;
            var ( found, word ) = interpreter.FindWord(name);

            if (found && (word != null))
            {
                var code = new ByteCode(ByteCode.Id.PushConstantValue,
                                        Value.From(word.Value.handler_index),
                                        null);

                interpreter.Constructor.Stack.Peek().ByteCode.Add(code);
            }
            else
            {
                var code = new ByteCode(ByteCode.Id.WordIndex,
                                        Value.From(name),
                                        null);

                interpreter.Constructor.Stack.Peek().ByteCode.Add(code);
            }
        }

        private static void WordExecute(SorthInterpreter interpreter)
        {
            var value = interpreter.Pop();

            if (value.IsNumeric())
            {
                interpreter.ExecuteWord(value.AsInteger(interpreter));
            }
            else if (value.IsString())
            {
                interpreter.ExecuteWord(value.AsString(interpreter));
            }
            else
            {
                interpreter.ThrowError("Bad executable value.");
            }
        }

        private static void WordIsDefined(SorthInterpreter interpreter)
        {
            var token = Helper.GetNextToken(interpreter);

            ByteCodeWords.InsertUserInstruction(interpreter,
                                                ByteCode.Id.WordExists,
                                                Value.From(token.Text));
        }

        private static void WordIsDefinedIm(SorthInterpreter interpreter)
        {
            var token = Helper.GetNextToken(interpreter);

            var ( found, _ ) = interpreter.FindWord(token.Text);
            interpreter.Push(Value.From(found));
        }

        private static void WordIsUndefinedIm(SorthInterpreter interpreter)
        {
            var token = Helper.GetNextToken(interpreter);

            var ( found, _ ) = interpreter.FindWord(token.Text);
            interpreter.Push(Value.From(!found));
        }

        private static void WordIfIm(SorthInterpreter interpreter)
        {
            static bool IsOneOf(string found, string[] words)
            {
                foreach (var word in words)
                {
                    if (found == word)
                    {
                        return true;
                    }
                }

                return false;
            }

            static string SkipUntil(SorthInterpreter interpreter, string[] words)
            {
                var token = Helper.GetNextToken(interpreter);

                while (!IsOneOf(token.Text, words))
                {
                    token = Helper.GetNextToken(interpreter);
                }

                return token.Text;
            }

            static string CompileUntil(SorthInterpreter interpreter, string[] words)
            {
                var token = Helper.GetNextToken(interpreter);

                while (!IsOneOf(token.Text, words))
                {
                    interpreter.Constructor.CompileToken(interpreter, token);
                    token = Helper.GetNextToken(interpreter);
                }

                return token.Text;
            }

            var result = interpreter.Pop().AsBoolean(interpreter);

            if (result)
            {
                var found_word = CompileUntil(interpreter, new[] { "[else]", "[then]" });

                if (found_word == "[else]")
                {
                    SkipUntil(interpreter, new[] { "[then]" });
                }
            }
            else
            {
                var found_word = SkipUntil(interpreter, new[] { "[else]", "[then]" });

                if (found_word == "[else]")
                {
                    CompileUntil(interpreter, new[] { "[then]" });
                }
            }
        }


        public static void Register(SorthInterpreter interpreter)
        {
            interpreter.AddWord("word", WordWord,
                "Get the next word in the token stream.",
                " -- next_word");

            interpreter.AddWord("words.get{}", WordGetWordTable,
                "Get a copy of the word table as it exists at time of calling.",
                " -- all_defined_words");

            interpreter.AddWord("`", WordWordIndex,
                "Get the index of the next word.",
                " -- index",
                true);

            interpreter.AddWord("execute", WordExecute,
                "Execute a word name or index.",
                "word_name_or_index -- ???");

            interpreter.AddWord("defined?", WordIsDefined,
                "Is the given word defined?",
                " -- bool",
                true);

            interpreter.AddWord("[defined?]", WordIsDefinedIm,
                "Evaluate at compile time, is the given word defined?",
                " -- bool",
                true);

            interpreter.AddWord("[undefined?]", WordIsUndefinedIm,
                "Evaluate at compile time, is the given word not defined?",
                " -- bool",
                true);

            interpreter.AddWord("[if]", WordIfIm,
                "Evaluate an if at compile time.  Only the code on successful branch is compiled.",
                "[if] <code> [else] <code> [then]",
                true);
        }
    }


    static class WordCreationWords
    {
        private static void WordStartWord(SorthInterpreter interpreter)
        {
            var current_token = ++interpreter.Constructor.CurrentToken;

            var name = interpreter.Constructor.Tokens[current_token].Text;
            var location = interpreter.Constructor.Tokens[current_token].Location;

            var construction = new Construction();
            construction.Name = name;
            construction.Location = location;

            interpreter.Constructor.Stack.Push(construction);
        }

        private static void WordEndWord(SorthInterpreter interpreter)
        {
            var construction = interpreter.Constructor.Stack.Pop();
            var word_handler = SorthILGenerator.GenerateHandler(interpreter,
                                                                construction.Name,
                                                                construction.ByteCode,
                                                                true);

            var location = construction.Location ?? new Location();

            interpreter.AddWord(construction.Name,
                                word_handler,
                                location,
                                construction.Description,
                                construction.Signature,
                                construction.IsImmediate,
                                construction.IsHidden,
                                true);
        }

        private static void WordImmediate(SorthInterpreter interpreter)
        {
            interpreter.Constructor.Stack.Peek().IsImmediate = true;
        }

        private static void WordHidden(SorthInterpreter interpreter)
        {
            interpreter.Constructor.Stack.Peek().IsHidden = true;
        }

        private static void WordDescription(SorthInterpreter interpreter)
        {
            var current_token = ++interpreter.Constructor.CurrentToken;

            if (current_token >= interpreter.Constructor.Tokens.Count)
            {
                interpreter.ThrowError("Unexpected end of token stream.");
            }

            var token = interpreter.Constructor.Tokens[current_token];

            if (token.Type != Token.TokenType.String)
            {
                interpreter.ThrowError("Expected description to be a string.");
            }

            interpreter.Constructor.Stack.Peek().Description = token.Text;
        }

        private static void WordSignature(SorthInterpreter interpreter)
        {
            var current_token = ++interpreter.Constructor.CurrentToken;

            if (current_token >= interpreter.Constructor.Tokens.Count)
            {
                interpreter.ThrowError("Unexpected end of token stream.");
            }

            var token = interpreter.Constructor.Tokens[current_token];

            if (token.Type != Token.TokenType.String)
            {
                interpreter.ThrowError("Expected signature to be a string.");
            }

            interpreter.Constructor.Stack.Peek().Signature = token.Text;
        }


        public static void Register(SorthInterpreter interpreter)
        {
            interpreter.AddWord(":", WordStartWord,
                "The start of a new word definition.",
                " -- ",
                true);

            interpreter.AddWord(";", WordEndWord,
                "The end of a new word definition.",
                " -- ",
                true);

            interpreter.AddWord("immediate", WordImmediate,
                "Mark the current word being built as immediate.",
                " -- ",
                true);

            interpreter.AddWord("hidden", WordHidden,
                "Mark the current word being built as hidden.",
                " -- ",
                true);

            interpreter.AddWord("description:", WordDescription,
                "Give a new word it's description.",
                " -- ",
                true);

            interpreter.AddWord("signature:", WordSignature,
                "Describe a new word's stack signature.",
                " -- ",
                true);
        }
    }


    static class ValueTypeWords
    {
        private static void WordValueIsNumber(SorthInterpreter interpreter)
        {
            var value = interpreter.Pop();

            interpreter.Push(Value.From(value.IsNumeric()));
        }

        private static void WordValueIsBoolean(SorthInterpreter interpreter)
        {
            var value = interpreter.Pop();

            interpreter.Push(Value.From(value.IsBoolean()));
        }

        private static void WordValueIsString(SorthInterpreter interpreter)
        {
            var value = interpreter.Pop();

            interpreter.Push(Value.From(value.IsString()));
        }

        private static void WordValueIsStructure(SorthInterpreter interpreter)
        {
            var value = interpreter.Pop();

            interpreter.Push(Value.From(value.IsDataObject()));
        }

        private static void WordValueIsArray(SorthInterpreter interpreter)
        {
            var value = interpreter.Pop();

            interpreter.Push(Value.From(value.IsArray()));
        }

        private static void WordValueIsBuffer(SorthInterpreter interpreter)
        {
            var value = interpreter.Pop();

            interpreter.Push(Value.From(value.IsByteBuffer()));
        }

        private static void WordValueIsHashTable(SorthInterpreter interpreter)
        {
            var value = interpreter.Pop();

            interpreter.Push(Value.From(value.IsHashMap()));
        }

        private static void WordValueIsByteCode(SorthInterpreter interpreter)
        {
            var value = interpreter.Pop();

            interpreter.Push(Value.From(value.IsByteCode()));
        }

        private static void WordValueCopy(SorthInterpreter interpreter)
        {
            interpreter.Push(interpreter.Pop().Clone());
        }


        public static void Register(SorthInterpreter interpreter)
        {
            interpreter.AddWord("value.is-number?", WordValueIsNumber,
                "Is the value a number?",
                "value -- bool");

            interpreter.AddWord("value.is-boolean?", WordValueIsBoolean,
                "Is the value a boolean?",
                "value -- bool");

            interpreter.AddWord("value.is-string?", WordValueIsString,
                "Is the value a string?",
                "value -- bool");

            interpreter.AddWord("value.is-structure?", WordValueIsStructure,
                "Is the value a structure?",
                "value -- bool");

            interpreter.AddWord("value.is-array?", WordValueIsArray,
                "Is the value an array?",
                "value -- bool");

            interpreter.AddWord("value.is-buffer?", WordValueIsBuffer,
                "Is the value a byte buffer?",
                "value -- bool");

            interpreter.AddWord("value.is-hash-table?", WordValueIsHashTable,
                "Is the value a hash table?",
                "value -- bool");

            interpreter.AddWord("value.is-bytecode?", WordValueIsByteCode,
                "Is the value bytecode?",
                "value -- bool");

            interpreter.AddWord("value.copy", WordValueCopy,
                "Create a new value that's a copy of another.  Deep copy as required.",
                "value -- new_copy");
        }
    }


    static class StringWords
    {
        private static int unique_index = 0;


        private static void WordHex(SorthInterpreter interpreter)
        {
            var value = interpreter.Pop().AsInteger(interpreter);
            interpreter.Push(Value.From(value.ToString("x")));
        }

        private static void WordUniqueStr(SorthInterpreter interpreter)
        {
            int new_index = Interlocked.Increment(ref unique_index);
            var new_string = $"unique-{new_index:D4}";
            unique_index++;

            interpreter.Push(Value.From(new_string));
        }

        private static void WordStringLength(SorthInterpreter interpreter)
        {
            var value = interpreter.Pop().AsString(interpreter);
            interpreter.Push(Value.From(value.Length));
        }

        private static void WordStringInsert(SorthInterpreter interpreter)
        {
            var base_str = interpreter.Pop().AsString(interpreter);
            var position = interpreter.Pop().AsInteger(interpreter);
            var sub_str = interpreter.Pop().AsString(interpreter);

            interpreter.Push(Value.From(base_str.Insert((int)position, sub_str)));
        }

        private static void WordStringRemove(SorthInterpreter interpreter)
        {
            var base_str = interpreter.Pop().AsString(interpreter);
            var position = interpreter.Pop().AsInteger(interpreter);
            var count = interpreter.Pop().AsInteger(interpreter);

            if (count == -1)
            {
                interpreter.Push(Value.From(base_str.Remove((int)position)));
            }
            else
            {
                interpreter.Push(Value.From(base_str.Remove((int)position, (int)count)));
            }
        }

        private static void WordStringFind(SorthInterpreter interpreter)
        {
            var base_str = interpreter.Pop().AsString(interpreter);
            var search = interpreter.Pop().AsString(interpreter);

            interpreter.Push(Value.From(base_str.IndexOf(search)));
        }

        private static void WordStringSubString(SorthInterpreter interpreter)
        {
            var base_str = interpreter.Pop().AsString(interpreter);
            var end = (int)interpreter.Pop().AsInteger(interpreter);
            var start = (int)interpreter.Pop().AsInteger(interpreter);

            var result = "";

            if (end == -1)
            {
                result = base_str.Substring(start);
            }
            else
            {
                var length = end - start;
                base_str.Substring(start, length);
            }

            interpreter.Push(Value.From(result));
        }

        private static void WordStringIndexRead(SorthInterpreter interpreter)
        {
            var base_str = interpreter.Pop().AsString(interpreter);
            var index = (int)interpreter.Pop().AsInteger(interpreter);

            string result = base_str[index].ToString();

            interpreter.Push(Value.From(result));
        }

        private static void WordStringToNumber(SorthInterpreter interpreter)
        {
            var base_str = interpreter.Pop().AsString(interpreter);
            Value result;

            if (base_str.IndexOf('.') != -1)
            {
                result = Value.From(double.Parse(base_str));
            }
            else
            {
                result = Value.From(long.Parse(base_str));
            }

            interpreter.Push(result);
        }

        private static void WordToString(SorthInterpreter interpreter)
        {
            Value value = interpreter.Pop();
            interpreter.Push(Value.From(value.ToString() ?? ""));
        }


        public static void Register(SorthInterpreter interpreter)
        {
            interpreter.AddWord("hex", WordHex,
                "Convert a number into a hex string.",
                "number -- hex_string");

            interpreter.AddWord("unique_str", WordUniqueStr,
                "Generate a unique string and push it onto the data stack.",
                " -- string");

            interpreter.AddWord("string.size@", WordStringLength,
                "Get the length of a given string.",
                "string -- size");

            interpreter.AddWord("string.[]!", WordStringInsert,
                "Insert a string into another string.",
                "sub_string position string -- updated_string");

            interpreter.AddWord("string.remove", WordStringRemove,
                "Remove some characters from a string.",
                "count position string -- updated_string");

            interpreter.AddWord("string.find", WordStringFind,
                "Find the first instance of a string within another.",
                "search_string string -- index");

            interpreter.AddWord("string.sub_string", WordStringSubString,
                "Return the string segment between a given start and end point.",
                "start end string -- sub_string");

            interpreter.AddWord("string.[]@", WordStringIndexRead,
                "Read a character from the given string.",
                "index string -- character");

            interpreter.AddWord("string.to_number", WordStringToNumber,
                "Convert a string into a number.",
                "string -- number");

            interpreter.AddWord("to_string", WordToString,
                "Convert a value to a string.",
                "value -- string");

            interpreter.AddWord("string.npos", (interpreter) => interpreter.Push(Value.From(-1)),
                "Constant value that indicates a search has failed.",
                " -- npos");
        }
    }


    static class DataStructureWords
    {
        private static void AddFieldWords(SorthInterpreter interpreter,
                                          Location location,
                                          DataObjectDefinition definition,
                                          int field_index,
                                          Word swap,
                                          Word struct_write,
                                          Word struct_read,
                                          bool is_hidden)
        {
            const bool is_immediate = false;
            const bool is_scripted = false;

            interpreter.AddWord($"{definition.Name}.{definition.FieldNames[field_index]}",
                (interpreter) =>
                {
                    interpreter.Push(Value.From(field_index));
                },
                location,
                $"Access the structure {definition.Name} field " +
                    $"index {definition.FieldNames[field_index]}.",
                " -- field_index",
                is_immediate,
                is_hidden,
                is_scripted);

            interpreter.AddWord($"{definition.Name}.{definition.FieldNames[field_index]}!",
                (interpreter) =>
                {
                    interpreter.Push(Value.From(field_index));
                    interpreter.ExecuteWord(swap.handler_index);
                    interpreter.ExecuteWord(struct_write.handler_index);
                },
                location,
                $"Write to the structure field {definition.FieldNames[field_index]}.",
                "new_value structure -- ",
                is_immediate,
                is_hidden,
                is_scripted);

            interpreter.AddWord($"{definition.Name}.{definition.FieldNames[field_index]}@",
                (interpreter) =>
                {
                    interpreter.Push(Value.From(field_index));
                    interpreter.ExecuteWord(swap.handler_index);
                    interpreter.ExecuteWord(struct_read.handler_index);
                },
                location,
                $"Read from structure field {definition.FieldNames[field_index]}.",
                "structure -- value",
                is_immediate,
                is_hidden,
                is_scripted);

            interpreter.AddWord($"{definition.Name}.{definition.FieldNames[field_index]}!!",
                (interpreter) =>
                {
                    var var_index = (int)interpreter.Pop().AsInteger(interpreter);

                    interpreter.Push(Value.From(field_index));
                    interpreter.Push(interpreter.Variables[var_index]);
                    interpreter.ExecuteWord(struct_write.handler_index);
                },
                location,
                $"Write to the structure field {definition.FieldNames[field_index]} in a variable.",
                "new_value structure_var -- ",
                is_immediate,
                is_hidden,
                is_scripted);

            interpreter.AddWord($"{definition.Name}.{definition.FieldNames[field_index]}@@",
                (interpreter) =>
                {
                    var var_index = (int)interpreter.Pop().AsInteger(interpreter);

                    interpreter.Push(Value.From(field_index));
                    interpreter.Push(interpreter.Variables[var_index]);
                    interpreter.ExecuteWord(struct_read.handler_index);
                },
                location,
                "Read from the structure field " +
                    $"{definition.FieldNames[field_index]} in a variable.",
                "structure_var -- value",
                is_immediate,
                is_hidden,
                is_scripted);
        }


        public static void CreateDataDefinitionWords(SorthInterpreter interpreter,
                                                     Location location,
                                                     DataObjectDefinition definition,
                                                     bool is_hidden = false)
        {
            const bool is_immediate = false;
            const bool is_scripted = false;

            interpreter.AddWord($"{definition.Name}.new",
                (interpreter) =>
                {
                    var new_data = new DataObject(definition);
                    interpreter.Push(Value.From(new_data));
                },
                location,
                $"Create a new instance of the structure {definition.Name}.",
                $" -- {definition.Name}",
                is_immediate,
                is_hidden,
                is_scripted);

            var ( swap_found, swap ) = interpreter.FindWord("swap");
            var ( struct_write_found, struct_write) = interpreter.FindWord("#!");
            var ( struct_read_found, struct_read ) = interpreter.FindWord("#@");

            if (   (!(swap_found || struct_write_found || struct_write_found))
                || ((swap == null) || (struct_write == null) || (struct_read == null)))
            {
                throw new ScriptError(location, "Internal error, could not find structure words.");
            }

            for (int i = 0; i < definition.FieldNames.Count; ++i)
            {
                AddFieldWords(interpreter,
                              location,
                              definition,
                              i,
                              swap.Value,
                              struct_write.Value,
                              struct_read.Value,
                              is_hidden);
            }
        }


        private static DataObjectDefinition LocationDefinition =
            new DataObjectDefinition("sorth.location",
                                     false,
                                     [
                                         "path",
                                         "line",
                                         "column"
                                     ],
                                     [
                                         Value.From(""),
                                         Value.From(1),
                                         Value.From(1)
                                     ]);


        private static DataObjectDefinition WordInfoDefinition =
            new DataObjectDefinition("sorth.word",
                                     false,
                                     [
                                         "name",
                                         "is_immediate",
                                         "is_scripted",
                                         "description",
                                         "signature",
                                         "handler_index",
                                         "location"
                                     ],
                                     [
                                         Value.From(""),
                                         Value.From(false),
                                         Value.From(false),
                                         Value.From(""),
                                         Value.From(""),
                                         Value.From(0),
                                         Value.From(new DataObject(LocationDefinition))
                                     ]);


        private static void RegisterWordInfoStruct(SorthInterpreter interpreter,
                                                  [CallerFilePath] string file_path = "",
                                                  [CallerLineNumber] int line_number = 0)
        {
            var location = new Location(file_path, line_number, 1);
            CreateDataDefinitionWords(interpreter, location, LocationDefinition, true);
            CreateDataDefinitionWords(interpreter, location, WordInfoDefinition, true);
        }


        public static Value WordDataFromWord(string name, Word word)
        {
            var data_object = new DataObject(WordInfoDefinition);

            data_object.Fields[0] = Value.From(name);
            data_object.Fields[1] = Value.From(word.is_immediate);
            data_object.Fields[2] = Value.From(word.is_scripted);
            data_object.Fields[3] = Value.From(word.description);
            data_object.Fields[4] = Value.From(word.signature);
            data_object.Fields[5] = Value.From(word.handler_index);

            // If location is null, just use the default value.
            if (word.location != null)
            {
                var location_object = new DataObject(LocationDefinition);

                location_object.Fields[0] = Value.From(word.location.Value.Path);
                location_object.Fields[1] = Value.From(word.location.Value.Line);
                location_object.Fields[2] = Value.From(word.location.Value.Column);

                data_object.Fields[6] = Value.From(location_object);
            }

            return Value.From(data_object);
        }


        private static void WordDataDefinition(SorthInterpreter interpreter)
        {
            var location = interpreter.CurrentLocation ?? new Location();

            var found_initializers = interpreter.Pop().AsBoolean(interpreter);
            var is_hidden = interpreter.Pop().AsBoolean(interpreter);
            var fields = interpreter.Pop().AsArray(interpreter);
            var name = interpreter.Pop().AsString(interpreter);

            var defaults = new List<Value>(fields.Count);

            if (found_initializers)
            {
                defaults = interpreter.Pop().AsArray(interpreter);
            }
            else
            {
                foreach (var field in fields)
                {
                    defaults.Add(Value.Default());
                }
            }

            var field_names = new List<string>(fields.Count);

            foreach (var field in fields)
            {
                field_names.Add(field.AsString(interpreter));
            }

            var definition = new DataObjectDefinition(name, is_hidden, field_names, defaults);

            CreateDataDefinitionWords(interpreter, location, definition, is_hidden);
        }

        private static void WordReadField(SorthInterpreter interpreter)
        {
            var data = interpreter.Pop().AsDataObject(interpreter);
            var index = (int)interpreter.Pop().AsInteger(interpreter);

            interpreter.Push(data.Fields[index]);
        }

        private static void WordWriteField(SorthInterpreter interpreter)
        {
            var data = interpreter.Pop().AsDataObject(interpreter);
            var index = (int)interpreter.Pop().AsInteger(interpreter);
            var value = interpreter.Pop();

            data.Fields[index] = value;
        }

        private static void WordStructureIterate(SorthInterpreter interpreter)
        {
            var data = interpreter.Pop().AsDataObject(interpreter);
            var word_index = (int)interpreter.Pop().AsInteger(interpreter);

            for (int i = 0; i < data.Fields.Count; ++i)
            {
                interpreter.Push(Value.From(data.Definition.FieldNames[i]));
                interpreter.Push(data.Fields[i]);

                interpreter.ExecuteWord(word_index);
            }
        }

        private static void WordStructureFieldExists(SorthInterpreter interpreter)
        {
            var data = interpreter.Pop().AsDataObject(interpreter);
            var field = interpreter.Pop().AsString(interpreter);

            var found = false;

            foreach (var next_name in data.Definition.FieldNames)
            {
                if (field == next_name)
                {
                    found = true;
                    break;
                }
            }

            interpreter.Push(Value.From(found));
        }

        private static void WordStructureCompare(SorthInterpreter interpreter)
        {
            Value data_object_b = interpreter.Pop();
            Value data_object_a = interpreter.Pop();

            if (   (!data_object_a.IsDataObject())
                || (!data_object_b.IsDataObject()))
            {
                var a_message = data_object_a.IsDataObject() ? "data object" : "non-data object";
                var b_message = data_object_b.IsDataObject() ? "data object" : "non-data object";

                interpreter.ThrowError("Expected both values to be data objects, " +
                                       $"but got {a_message} and {b_message}.");
            }

            interpreter.Push(Value.From(data_object_a.Equals(data_object_b)));
        }


        public static void Register(SorthInterpreter interpreter)
        {
            interpreter.AddWord("#", WordDataDefinition,
                "Beginning of a structure definition.",
                " -- ");

            interpreter.AddWord("#@", WordReadField,
                "Read a field from a structure.",
                "field_index structure -- value");

            interpreter.AddWord("#!", WordWriteField,
                "Write to a field of a structure.",
                "value field_index structure -- ");

            interpreter.AddWord("#.iterate", WordStructureIterate,
                "Call an iterator for each member of a structure.",
                "word_or_index -- ");

            interpreter.AddWord("#.field-exists?", WordStructureFieldExists,
                "Check if the named structure field exits.",
                "field_name structure -- boolean");

            interpreter.AddWord("#.=", WordStructureCompare,
                "Check if two structures are the same.",
                "a b -- boolean");

            RegisterWordInfoStruct(interpreter);
        }
    }


    static class ArrayWords
    {
        private static void WordArrayNew(SorthInterpreter interpreter)
        {
            var count = (int)interpreter.Pop().AsInteger(interpreter);
            var array = new List<Value>(count);

            for (int i = 0; i < count; ++i)
            {
                array.Add(Value.Default());
            }

            interpreter.Push(Value.From(array));
        }

        private static void WordArraySize(SorthInterpreter interpreter)
        {
            var array = interpreter.Pop().AsArray(interpreter);

            interpreter.Push(Value.From(array.Count));
        }

        private static void WordArrayWriteIndex(SorthInterpreter interpreter)
        {
            var array = interpreter.Pop().AsArray(interpreter);
            var index = (int)interpreter.Pop().AsInteger(interpreter);
            var new_value = interpreter.Pop();

            if ((index < 0) && (index >= array.Count))
            {
                interpreter.ThrowError($"Array index {index} is out of bounds, {array.Count}.");
            }

            array[index] = new_value;
        }

        private static void WordArrayReadIndex(SorthInterpreter interpreter)
        {
            var array = interpreter.Pop().AsArray(interpreter);
            var index = (int)interpreter.Pop().AsInteger(interpreter);

            if ((index < 0) && (index >= array.Count))
            {
                interpreter.ThrowError($"Array index {index} is out of bounds, {array.Count}.");
            }

            interpreter.Push(array[index]);
        }

        private static void WordArrayInsert(SorthInterpreter interpreter)
        {
            var array = interpreter.Pop().AsArray(interpreter);
            var index = (int)interpreter.Pop().AsInteger(interpreter);
            var new_value = interpreter.Pop();

            array.Insert(index, new_value);
        }

        private static void WordArrayDelete(SorthInterpreter interpreter)
        {
            var array = interpreter.Pop().AsArray(interpreter);
            var index = (int)interpreter.Pop().AsInteger(interpreter);

            array.RemoveAt(index);
        }

        private static void WordArrayResize(SorthInterpreter interpreter)
        {
            var array = interpreter.Pop().AsArray(interpreter);
            var new_size = (int)interpreter.Pop().AsInteger(interpreter);

            if (new_size > array.Count)
            {
                var count = new_size - array.Count;

                for (var i = 0; i < count; ++i)
                {
                    array.Add(Value.Default());
                }
            }
            else if (new_size < array.Count)
            {
                array.RemoveRange(new_size, array.Count - new_size);
            }
        }

        private static void WordArrayPlus(SorthInterpreter interpreter)
        {
            var array_src = interpreter.Pop().AsArray(interpreter);
            var array_dst = interpreter.Pop().AsArray(interpreter);

            for (int i = 0; i < array_src.Count; ++i)
            {
                array_dst.Add(array_src[i].Clone());
            }

            interpreter.Push(Value.From(array_dst));
        }

        private static void WordArrayCompare(SorthInterpreter interpreter)
        {
            Value array_b = interpreter.Pop();
            Value array_a = interpreter.Pop();

            if (   (!array_a.IsArray())
                || (!array_b.IsArray()))
            {
                var a_message = array_a.IsArray() ? "array" : "non-array";
                var b_message = array_b.IsArray() ? "array" : "non-array";

                interpreter.ThrowError("Expected both values to be arrays, " +
                                       $"but got {a_message} and {b_message}.");
            }

            interpreter.Push(Value.From(array_a.Equals(array_b)));
        }

        private static void WordPushFront(SorthInterpreter interpreter)
        {
            var array = interpreter.Pop().AsArray(interpreter);
            var value = interpreter.Pop();

            array.Insert(0, value);
        }

        private static void WordPushBack(SorthInterpreter interpreter)
        {
            var array = interpreter.Pop().AsArray(interpreter);
            var value = interpreter.Pop();

            array.Add(value);
        }

        private static void WordPopFront(SorthInterpreter interpreter)
        {
            var array = interpreter.Pop().AsArray(interpreter);

            if (array.Count == 0)
            {
                interpreter.ThrowError("Pop from empty array.");
            }

            var value = array[0];
            array.RemoveRange(0, 1);

            interpreter.Push(value);
        }

        private static void WordPopBack(SorthInterpreter interpreter)
        {
            var array = interpreter.Pop().AsArray(interpreter);

            if (array.Count == 0)
            {
                interpreter.ThrowError("Pop from empty array.");
            }

            var value = array[array.Count - 1];
            array.RemoveRange(array.Count - 1, 1);

            interpreter.Push(value);
        }


        public static void Register(SorthInterpreter interpreter)
        {
            interpreter.AddWord("[].new", WordArrayNew,
                "Create a new array with the given default size.",
                "size -- array");

            interpreter.AddWord("[].size@", WordArraySize,
                "Read the size of the array object.",
                "array -- size");

            interpreter.AddWord("[]!", WordArrayWriteIndex,
                "Write to a value in the array.",
                "value index array -- ");

            interpreter.AddWord("[]@", WordArrayReadIndex,
                "Read a value from the array.",
                "index array -- value");

            interpreter.AddWord("[].insert", WordArrayInsert,
                "Grow an array by inserting a value at the given location.",
                "value index array -- ");

            interpreter.AddWord("[].delete", WordArrayDelete,
                "Shrink an array by removing the value at the given location.",
                "index array -- ");

            interpreter.AddWord("[].size!", WordArrayResize,
                "Grow or shrink the array to the new size.",
                "new_size array -- ");

            interpreter.AddWord("[].+", WordArrayPlus,
                "Take two arrays and deep copy the contents from the second into the first.",
                "dest source -- dest");

            interpreter.AddWord("[].=", WordArrayCompare,
                "Take two arrays and compare the contents to each other.",
                "dest source -- dest");

            interpreter.AddWord("[].push_front!", WordPushFront,
                "Push a value to the front of an array.",
                "value array -- ");

            interpreter.AddWord("[].push_back!", WordPushBack,
                "Push a value to the end of an array.",
                "value array -- ");

            interpreter.AddWord("[].pop_front!", WordPopFront,
                "Pop a value from the front of an array.",
                "array -- value");

            interpreter.AddWord("[].pop_back!", WordPopBack,
                "Pop a value from the back of an array.",
                "array -- value");
        }
    }


    static class ByteBufferWords
    {
        private static void CheckBufferIndex(SorthInterpreter interpreter,
                                             ByteBuffer buffer,
                                             int byte_size)
        {
            if ((buffer.Position + byte_size) > buffer.Count)
            {
                var message = $"Writing a value of size {byte_size} at a position of " +
                              $"{buffer.Position} would exceed the buffer size, " +
                              $"{buffer.Count}.";

                interpreter.ThrowError(message);
            }
        }

        public static void WordBufferNew(SorthInterpreter interpreter)
        {
            var size = (int)interpreter.Pop().AsInteger(interpreter);
            var buffer = new ByteBuffer(size);

            interpreter.Push(Value.From(buffer));
        }

        public static void WordBufferWriteInt(SorthInterpreter interpreter)
        {
            var byte_size = (int)interpreter.Pop().AsInteger(interpreter);
            var buffer = interpreter.Pop().AsByteBuffer(interpreter);
            var value = interpreter.Pop().AsInteger(interpreter);

            CheckBufferIndex(interpreter, buffer, byte_size);

            if (   (byte_size != 1)
                && (byte_size != 2)
                && (byte_size != 4)
                && (byte_size != 8))
            {
                interpreter.ThrowError($"Bad integer byte size, {byte_size}.");
            }

            buffer.WriteInt(byte_size, value);
        }

        public static void WordBufferReadInt(SorthInterpreter interpreter)
        {
            var is_signed = interpreter.Pop().AsBoolean(interpreter);
            var byte_size = (int)interpreter.Pop().AsInteger(interpreter);
            var buffer = interpreter.Pop().AsByteBuffer(interpreter);

            CheckBufferIndex(interpreter, buffer, byte_size);

            if (   (byte_size != 1)
                && (byte_size != 2)
                && (byte_size != 4)
                && (byte_size != 8))
            {
                interpreter.ThrowError($"Bad integer byte size, {byte_size}.");
            }

            var value = buffer.ReadInt(byte_size, is_signed);
            interpreter.Push(Value.From(value));
        }

        public static void WordBufferWriteFloat(SorthInterpreter interpreter)
        {
            var byte_size = (int)interpreter.Pop().AsInteger(interpreter);
            var buffer = interpreter.Pop().AsByteBuffer(interpreter);
            var value = interpreter.Pop().AsDouble(interpreter);

            CheckBufferIndex(interpreter, buffer, byte_size);

            if (   (byte_size != 4)
                && (byte_size != 8))
            {
                interpreter.ThrowError($"Bad float byte size, {byte_size}.");
            }

            buffer.WriteDouble(byte_size, value);
        }

        public static void WordBufferReadFloat(SorthInterpreter interpreter)
        {
            var byte_size = (int)interpreter.Pop().AsInteger(interpreter);
            var buffer = interpreter.Pop().AsByteBuffer(interpreter);

            CheckBufferIndex(interpreter, buffer, byte_size);

            if (   (byte_size != 4)
                && (byte_size != 8))
            {
                interpreter.ThrowError($"Bad float byte size, {byte_size}.");
            }

            var value = buffer.ReadDouble(byte_size);
            interpreter.Push(Value.From(value));
        }

        public static void WordBufferWriteString(SorthInterpreter interpreter)
        {
            var byte_size = (int)interpreter.Pop().AsInteger(interpreter);
            var buffer = interpreter.Pop().AsByteBuffer(interpreter);
            var value = interpreter.Pop().AsString(interpreter);

            CheckBufferIndex(interpreter, buffer, byte_size);

            buffer.WriteString(byte_size, value);
        }

        public static void WordBufferReadString(SorthInterpreter interpreter)
        {
            var byte_size = (int)interpreter.Pop().AsInteger(interpreter);
            var buffer = interpreter.Pop().AsByteBuffer(interpreter);

            CheckBufferIndex(interpreter, buffer, byte_size);

            var value = buffer.ReadString(byte_size);
            interpreter.Push(Value.From(value));
        }

        public static void WordBufferSetPosition(SorthInterpreter interpreter)
        {
            var buffer = interpreter.Pop().AsByteBuffer(interpreter);
            var new_position = (int)interpreter.Pop().AsInteger(interpreter);

            buffer.Position = new_position;
        }

        public static void WordBufferGetPosition(SorthInterpreter interpreter)
        {
            var buffer = interpreter.Pop().AsByteBuffer(interpreter);

            interpreter.Push(Value.From(buffer.Position));
        }


        public static void Register(SorthInterpreter interpreter)
        {
            interpreter.AddWord("buffer.new", WordBufferNew,
                "Create a new byte buffer.",
                "size -- buffer");

            interpreter.AddWord("buffer.int!", WordBufferWriteInt,
                "Write an integer of a given size to the buffer.",
                "value buffer byte_size -- ");

            interpreter.AddWord("buffer.int@", WordBufferReadInt,
                "Read an integer of a given size from the buffer.",
                "buffer byte_size is_signed -- value");


            interpreter.AddWord("buffer.float!", WordBufferWriteFloat,
                "Write a float of a given size to the buffer.",
                "value buffer byte_size -- ");

            interpreter.AddWord("buffer.float@", WordBufferReadFloat,
                "read a float of a given size from the buffer.",
                "buffer byte_size -- value");


            interpreter.AddWord("buffer.string!", WordBufferWriteString,
                "Write a string of given size to the buffer.  Padded with 0s if needed.",
                "value buffer size -- ");

            interpreter.AddWord("buffer.string@", WordBufferReadString,
                "Read a string of a given max size from the buffer.",
                "buffer size -- value");


            interpreter.AddWord("buffer.position!", WordBufferSetPosition,
                "Set the position of the buffer pointer.",
                "position buffer -- ");

            interpreter.AddWord("buffer.position@", WordBufferGetPosition,
                "Get the position of the buffer pointer.",
                "buffer -- position");
        }
    }


    static class HashTableWords
    {
        private static void WordHashTableNew(SorthInterpreter interpreter)
        {
            var map = new Dictionary<Value, Value>();

            interpreter.Push(Value.From(map));
        }

        private static void WordHashTableInsert(SorthInterpreter interpreter)
        {
            var map = interpreter.Pop().AsHashMap(interpreter);
            var key = interpreter.Pop();
            var value = interpreter.Pop();

            map[key] = value;
        }

        private static void WordHashTableFind(SorthInterpreter interpreter)
        {
            var map = interpreter.Pop().AsHashMap(interpreter);
            var key = interpreter.Pop();

            if (map.TryGetValue(key, out var value))
            {
                if (value != null)
                {
                    interpreter.Push(value);
                }
                else
                {
                    interpreter.ThrowError("Found value null.");
                }
            }
            else
            {
                interpreter.ThrowError($"Value {key} does not exist in the table.");
            }
        }

        private static void WordHashTableExists(SorthInterpreter interpreter)
        {
            var map = interpreter.Pop().AsHashMap(interpreter);
            var key = interpreter.Pop();

            if (map.TryGetValue(key, out var value))
            {
                interpreter.Push(Value.From(true));
            }
            else
            {
                interpreter.Push(Value.From(false));
            }
        }

        private static void WordHashPlus(SorthInterpreter interpreter)
        {
            var map_src = interpreter.Pop().AsHashMap(interpreter);
            var map_dst = interpreter.Pop().AsHashMap(interpreter);

            foreach (var entry in map_src)
            {
                map_dst[entry.Key.Clone()] = entry.Value.Clone();
            }

            interpreter.Push(Value.From(map_dst));
        }

        private static void WordHashCompare(SorthInterpreter interpreter)
        {
            Value hash_b = interpreter.Pop();
            Value hash_a = interpreter.Pop();

            if (   (!hash_a.IsHashMap())
                || (!hash_b.IsHashMap()))
            {
                var a_message = hash_a.IsHashMap() ? "hash-map" : "non-hash-map";
                var b_message = hash_b.IsHashMap() ? "hash-map" : "non-hash-map";

                interpreter.ThrowError("Expected both values to be hash-maps, " +
                                       $"but got {a_message} and {b_message}.");
            }

            interpreter.Push(Value.From(hash_a.Equals(hash_b)));
        }

        private static void WordHashSize(SorthInterpreter interpreter)
        {
            var map = interpreter.Pop().AsHashMap(interpreter);

            interpreter.Push(Value.From(map.Count));
        }

        private static void WordHashTableIterate(SorthInterpreter interpreter)
        {
            var map = interpreter.Pop().AsHashMap(interpreter);
            var word_index = (int)interpreter.Pop().AsInteger(interpreter);

            foreach (var entry in map)
            {
                interpreter.Push(entry.Key);
                interpreter.Push(entry.Value);

                interpreter.ExecuteWord(word_index);
            }
        }


        public static void Register(SorthInterpreter interpreter)
        {
            interpreter.AddWord("{}.new", WordHashTableNew,
                "Create a new hash table.",
                " -- new_hash_table");

            interpreter.AddWord("{}!", WordHashTableInsert,
                "Write a value to a given key in the table.",
                "value key table -- ");

            interpreter.AddWord("{}@", WordHashTableFind,
                "Read a value from a given key in the table.",
                "key table -- value");

            interpreter.AddWord("{}?", WordHashTableExists,
                "Check if a given key exists in the table.",
                "key table -- bool");

            interpreter.AddWord("{}.+", WordHashPlus,
                "Take two hashes and deep copy the contents from the second into the first.",
                "dest source -- dest");

            interpreter.AddWord("{}.=", WordHashCompare,
                "Compare two hash-maps for equality.",
                "a b -- is-equal?");

            interpreter.AddWord("{}.size@", WordHashSize,
                "Get the size of the hash table.",
                "table -- size");

            interpreter.AddWord("{}.iterate", WordHashTableIterate,
                "Iterate through a hash table and call a word for each item.",
                "word_index hash_table -- ");
        }
    }


    static class MathLogicAndBitWords
    {
        private static void WordAdd(SorthInterpreter interpreter)
        {
            Helper.StringOrNumericOp(interpreter,
                                     (a, b) => a + b,
                                     (a, b) => a + b,
                                     (a, b) => a + b);
        }

        private static void WordSubtract(SorthInterpreter interpreter)
        {
            Helper.MathOp(interpreter,
                          (a, b) => a - b,
                          (a, b) => a - b);
        }

        private static void WordMultiply(SorthInterpreter interpreter)
        {
            Helper.MathOp(interpreter,
                          (a, b) => a * b,
                          (a, b) => a * b);
        }

        private static void WordDivide(SorthInterpreter interpreter)
        {
            Helper.MathOp(interpreter,
                          (a, b) => a / b,
                          (a, b) => a / b);
        }

        private static void WordMod(SorthInterpreter interpreter)
        {
            Helper.MathOp(interpreter,
                          (a, b) => a % b,
                          (a, b) => a % b);
        }


        private static void WordLogicAnd(SorthInterpreter interpreter)
        {
            Helper.LogicOp(interpreter, (a, b) => a && b);
        }

        private static void WordLogicOr(SorthInterpreter interpreter)
        {
            Helper.LogicOp(interpreter, (a, b) => a || b);
        }

        private static void WordLogicNot(SorthInterpreter interpreter)
        {
            var value = !interpreter.Pop().AsBoolean(interpreter);
            interpreter.Push(Value.From(value));
        }


        private static void WordBitAnd(SorthInterpreter interpreter)
        {
            Helper.LogicBitOp(interpreter, (a, b) => a & b);
        }

        private static void WordBitOr(SorthInterpreter interpreter)
        {
            Helper.LogicBitOp(interpreter, (a, b) => a | b);
        }

        private static void WordBitXor(SorthInterpreter interpreter)
        {
            Helper.LogicBitOp(interpreter, (a, b) => a ^ b);
        }

        private static void WordBitNot(SorthInterpreter interpreter)
        {
            var value = ~interpreter.Pop().AsInteger(interpreter);
            interpreter.Push(Value.From(value));
        }

        private static void WordBitLeftShift(SorthInterpreter interpreter)
        {
            Helper.LogicBitOp(interpreter, (value, amount) => value << (int)amount);
        }

        private static void WordBitRightShift(SorthInterpreter interpreter)
        {
            Helper.LogicBitOp(interpreter, (value, amount) => value >> (int)amount);
        }


        public static void Register(SorthInterpreter interpreter)
        {
            // Basic math.
            interpreter.AddWord("+", WordAdd,
                "Add 2 numbers or strings together.",
                "a b -- result");

            interpreter.AddWord("-", WordSubtract,
                "Subtract 2 numbers.",
                "a b -- result");

            interpreter.AddWord("*", WordMultiply,
                "Multiply 2 numbers.",
                "a b -- result");

            interpreter.AddWord("/", WordDivide,
                "Divide 2 numbers.",
                "a b -- result");

            interpreter.AddWord("%", WordMod,
                "Divide 2 numbers.",
                "a b -- result");


            // Logical comparison.
            interpreter.AddWord("&&", WordLogicAnd,
                "Logically compare 2 values.",
                "a b -- bool");

            interpreter.AddWord("||", WordLogicOr,
                "Logically compare 2 values.",
                "a b -- bool");

            interpreter.AddWord("'", WordLogicNot,
                "Logically invert a boolean value.",
                "bool -- bool");


            // Bitwise operators.
            interpreter.AddWord("&", WordBitAnd,
                "Bitwise AND two numbers together.",
                "a b -- result");

            interpreter.AddWord("|", WordBitOr,
                "Bitwise OR two numbers together.",
                "a b -- result");

            interpreter.AddWord("^", WordBitXor,
                "Bitwise XOR two numbers together.",
                "a b -- result");

            interpreter.AddWord("~", WordBitNot,
                "Bitwise NOT a number.",
                "number -- result");

            interpreter.AddWord("<<", WordBitLeftShift,
                "Shift a numbers bits to the left.",
                "value amount -- result");

            interpreter.AddWord(">>", WordBitRightShift,
                "Shift a numbers bits to the right.",
                "value amount -- result");
        }
    }


    static class EqualityWords
    {
        private static void WordEqual(SorthInterpreter interpreter)
        {
            Helper.ComparisonOp(interpreter,
                                (a, b) => a == b,
                                (a, b) => a == b,
                                (a, b) => a == b);
        }

        private static void WordNotEqual(SorthInterpreter interpreter)
        {
            Helper.ComparisonOp(interpreter,
                                (a, b) => a != b,
                                (a, b) => a != b,
                                (a, b) => a != b);
        }

        private static void WordGreaterEqual(SorthInterpreter interpreter)
        {
            Helper.ComparisonOp(interpreter,
                                (a, b) => a >= b,
                                (a, b) => a >= b,
                                (a, b) => String.Compare(a, b) >= 0);
        }

        private static void WordLessEqual(SorthInterpreter interpreter)
        {
            Helper.ComparisonOp(interpreter,
                                (a, b) => a <= b,
                                (a, b) => a <= b,
                                (a, b) => String.Compare(a, b) <= 0);
        }

        private static void WordGreater(SorthInterpreter interpreter)
        {
            Helper.ComparisonOp(interpreter,
                                (a, b) => a > b,
                                (a, b) => a > b,
                                (a, b) => String.Compare(a, b) > 0);
        }

        private static void WordLess(SorthInterpreter interpreter)
        {
            Helper.ComparisonOp(interpreter,
                                (a, b) => a < b,
                                (a, b) => a < b,
                                (a, b) => String.Compare(a, b) < 0);
        }


        public static void Register(SorthInterpreter interpreter)
        {
            interpreter.AddWord("=", WordEqual,
                "Are 2 values equal?",
                "a b -- bool");

            interpreter.AddWord("<>", WordNotEqual,
                "Are 2 values not equal?",
                "a b -- bool");

            interpreter.AddWord(">=", WordGreaterEqual,
                "Is one value greater or equal to another?",
                "a b -- bool");

            interpreter.AddWord("<=", WordLessEqual,
                "Is one value less than or equal to another?",
                "a b -- bool");

            interpreter.AddWord(">", WordGreater,
                "Is one value greater than another?",
                "a b -- bool");

            interpreter.AddWord("<", WordLess,
                "Is one value less than another?",
                "a b -- bool");
        }
    }


    public static class BaseWords
    {
        public static void Register(SorthInterpreter interpreter)
        {
            SorthWords.Register(interpreter);
            StackWords.Register(interpreter);
            ConstantWords.Register(interpreter);
            ByteCodeWords.Register(interpreter);
            WordWords.Register(interpreter);
            WordCreationWords.Register(interpreter);
            ValueTypeWords.Register(interpreter);
            StringWords.Register(interpreter);
            DataStructureWords.Register(interpreter);
            ArrayWords.Register(interpreter);
            ByteBufferWords.Register(interpreter);
            HashTableWords.Register(interpreter);
            MathLogicAndBitWords.Register(interpreter);
            EqualityWords.Register(interpreter);
        }
    }

}
