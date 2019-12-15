﻿using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using TokensAPI;
using System.Text;
using System.Linq;

namespace TokensBuilder
{
    enum BlockType
    {
        DEFAULT,
        IF,
        ELIF,
        ELSE,
        WHILE,
        FOR,
        FOREACH,
        SWITCH,
        MODULE,
        NAMESPACE,
        CLASS,
        INTERFACE,
        STRUCT,
        ENUM,
        METHOD,
        CONSTRUCTOR,
        GET,
        SET,
        TRY,
        CATCH,
        FINALLY,
        /// <summary>
        /// For exmaple: <code>MyClass a = new MyClass { field = 0, kek = 1, lol = "lol" };</code>
        /// </summary>
        INIT,
        ENTRYPOINT
    }

    public class Generator
    {
        public ContextInfo context;
        public List<Expression> expressions;

        public Generator()
        {
            expressions = new List<Expression>();
            context = new ContextInfo();
        }

        public void GenerateIL(string assembly_name, string code, bool haveScript = true)
        {
            //create context
            context.appName = assembly_name;
            context.CreateName();
            context.CreateAssembly();
            context.haveScript = haveScript;
            if (haveScript) context.InitilizateScript();

            //parse code to expressions
            string[] lines = code.Split('\n', '\r');
            try
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    int j = 0;
                    StringBuilder buffer = new StringBuilder();
                    try
                    {
                        for (j = 0; !char.IsWhiteSpace(line[j]); j++)
                        {
                            try { buffer.Append(line[j]); }
                            catch { break; }
                        }
                        while (char.IsWhiteSpace(line[j]))
                        {
                            j++;
                        } //skip whitespaces
                        Token token = (Token)Enum.Parse(typeof(Token), buffer.ToString().TrimEnd());
                        buffer.Clear();
                        List<Identifer> args = new List<Identifer>();
                        byte priority = 0;
                        for (j = j; j < line.Length; j++)
                        {
                            char cur = line[j];
                            if (cur == '(')
                            {
                                buffer.Append(cur);
                                priority++;
                            }
                            else if (cur == ')' && priority > 0)
                            {
                                buffer.Append(cur);
                                priority--;
                            }
                            else if (char.IsWhiteSpace(cur) && priority == 0)
                            {
                                args.Add(Identifer.GetIdentifer(buffer.ToString()));
                                buffer.Clear();
                            }
                            else
                            {
                                buffer.Append(cur);
                            }
                        }
                        args.Add(Identifer.GetIdentifer(buffer.ToString()));
                        expressions.Add(new Expression { token = token, args = args });
                    }
                    catch { } //just skip
                }
            }
            catch { }

            //variables for building
            List<BlockType> blockType = new List<BlockType> { BlockType.DEFAULT };
            string namespace_name = "";
            Dictionary<string, Label> labels = new Dictionary<string, Label>();

            //parse expressions
            for (int i = 0; i < expressions.Count; i++)
            {
                Expression e = expressions[i];
                string name = e.args[0].GetValue();
                Console.WriteLine(e.token + string.Join(" ", e.args));
                switch (e.token)
                {
                    case Token.NULL:
                        context.ILGenerator.Emit(OpCodes.Nop);
                        break;
                    case Token.USE:
                        context.ILGenerator.UsingNamespace(name);
                        break;
                    case Token.WRITEVAR:
                        break;
                    case Token.CLASS:
                        context.type = context.module.DefineType(name);
                        blockType.Add(BlockType.CLASS);
                        break;
                    case Token.FIELD:
                        break;
                    case Token.METHOD:
                        if (blockType.Count == 1)
                        {
                            Type type = Type.GetType(name);
                            if (type != null)
                            {
                                //extension function
                            }
                            else if (haveScript)
                            {
                                //script function
                                List<Identifer> argums = e.args.GetRange(1, e.args.Count);
                                for (int j = 0; j < argums.Count; j++)
                                {
                                    string attr = argums[j].GetValue();
                                }
                                context.method = context.mainType.DefineMethod(name, MethodAttributes.Public);
                            }
                            else
                            {
                                throw new Exception("Cannot initiliaze method without class");
                            }
                        }
                        else
                        {
                            blockType.Add(BlockType.METHOD);
                        }
                        break;
                    case Token.END:
                        blockType.RemoveAt(blockType.Count - 1);
                        //context.ILGenerator.Emit(OpCodes.Br);
                        break;
                    case Token.RUNFUNC:
                        for (int j = 2; j < e.args.Count; j++) context.LoadValue(e.args[i].GetValue()); 
                        context.ILGenerator.Emit(OpCodes.Call, Type.GetType(name).GetMethod(e.args[1].GetValue()));
                        break;
                    case Token.WHILE:
                        break;
                    case Token.FOR:
                        break;
                    case Token.FOREACH:
                        break;
                    case Token.BREAK:
                        context.ILGenerator.Emit(OpCodes.Br);
                        break;
                    case Token.CONTINUE:
                        break;
                    case Token.RETURN:
                        context.LoadValue(name);
                        context.ILGenerator.Emit(OpCodes.Ret);
                        break;
                    case Token.IF:
                        blockType.Add(BlockType.IF);
                        break;
                    case Token.ELSE:
                        blockType.Add(BlockType.ELSE);
                        break;
                    case Token.ELIF:
                        blockType.Add(BlockType.ELIF);
                        break;
                    case Token.GOTO:
                        context.ILGenerator.Emit(OpCodes.Jmp, labels[name]);
                        break;
                    case Token.LABEL:
                        Label label = context.ILGenerator.DefineLabel();
                        context.ILGenerator.MarkLabel(label);
                        labels.Add(name, label);
                        break;
                    case Token.YIELD:
                        if (context.method.ReturnType is IEnumerable<object>)
                        {
                            //pass
                        }
                        break;
                    case Token.GETLINK:
                        break;
                    case Token.WRITEINPOINTER:
                        break;
                    case Token.STRUCT:
                        break;
                    case Token.INTERFACE:
                        break;
                    case Token.ENUM:
                        break;
                    case Token.MODULE:
                        break;
                    case Token.CONSTRUCTOR:
                        break;
                    case Token.ATTRIBUTE:
                        if (name == "Entrypoint")
                        {
                            blockType.Add(BlockType.ENTRYPOINT);
                        }
                        break;
                    case Token.GETCONSTRUCTOR:
                        break;
                    case Token.OPCODEADD:
                        context.ILGenerator.Emit((OpCode) typeof(OpCodes).GetField(name).GetValue(null));
                        break;
                    case Token.EVENT:
                        break;
                    case Token.GETEVENT:
                        break;
                    case Token.TRY:
                        blockType.Add(BlockType.TRY);
                        break;
                    case Token.CATCH:
                        try { context.ILGenerator.BeginCatchBlock(Type.GetType(name)); }
                        catch { context.ILGenerator.BeginCatchBlock(typeof(Exception)); }
                        blockType.Add(BlockType.CATCH);
                        break;
                    case Token.IMPLEMENTS:
                        break;
                    case Token.THROW:
                        context.ILGenerator.ThrowException(Type.GetType(name));
                        break;
                    case Token.CALLCONSTRUCTOR:
                        break;
                    case Token.OVERRIDE:
                        context.type.SetParent(Type.GetType(name));
                        break;
                    case Token.GET:
                        break;
                    case Token.SET:
                        break;
                    case Token.TYPEOF:
                        context.ILGenerator.Emit(OpCodes.Ldobj, Type.GetType(name));
                        break;
                    case Token.CONST:
                        break;
                    case Token.ASYNC:
                        break;
                    case Token.AWAIT:
                        break;
                    case Token.SWITCH:
                        break;
                    case Token.CASE:
                        break;
                    case Token.DEFAULT:
                        break;
                    case Token.NEWPOINTER:
                        break;
                    case Token.STARTBLOCK:
                        label = context.ILGenerator.DefineLabel();
                        context.ILGenerator.MarkLabel(label);
                        break;
                    case Token.DIRECTIVA:
                        if (name == "outtype")
                        {
                            context.outputType = (PEFileKinds)Enum.Parse(typeof(PEFileKinds), e.args[1].GetValue(), true);
                        }
                        else if (name == "version")
                        {
                            context.assembly.GetName().Version = new Version(e.args[1].GetValue());
                            context.CreateName();
                        }
                        else if (name == "appname")
                        {
                            context.appName = e.args[1].GetValue();
                            context.CreateName();
                        }
                        else if (name == "enable")
                        {
                            string arg = e.args[1].GetValue();
                            if (arg == "script") context.haveScript = true;
                            else if (arg == "entrypoint") context.haveScript = false;
                        }
                        else
                        {
                            throw new NotSupportedException($"Directiva by name {name} not found (TokensError in line {i})");
                        }
                        break;
                    case Token.NAMESPACE:
                        namespace_name = e.args[0].GetValue();
                        break;
                    case Token.BREAKPOINT:
                        context.ILGenerator.Emit(OpCodes.Break);
                        break;
                    case Token.ADD:
                        foreach (Identifer identifer in e.args)
                        {
                            context.LoadValue(identifer.GetValue());
                        }
                        context.ILGenerator.Emit(OpCodes.Add);
                        break;
                    case Token.SUB:
                        foreach (Identifer identifer in e.args)
                        {
                            context.LoadValue(identifer.GetValue());
                        }
                        context.ILGenerator.Emit(OpCodes.Sub);
                        break;
                    case Token.DIV:
                        foreach (Identifer identifer in e.args)
                        {
                            context.LoadValue(identifer.GetValue());
                        }
                        context.ILGenerator.Emit(OpCodes.Div);
                        break;
                    case Token.MUL:
                        foreach (Identifer identifer in e.args)
                        {
                            context.LoadValue(identifer.GetValue());
                        }
                        context.ILGenerator.Emit(OpCodes.Mul);
                        break;
                    case Token.MOD:
                        foreach (Identifer identifer in e.args)
                        {
                            context.LoadValue(identifer.GetValue());
                        }
                        context.ILGenerator.Emit(OpCodes.Rem);
                        break;
                }
            }

            //close context
            if (context.haveScript) context.EndScript();
            context.EndWrite();
        }

        public void CreatePE(string full_name) => context.assembly.Save(full_name);
    }

    public class ContextInfo
    {
        public bool haveScript;
        public string appName;
        public PEFileKinds outputType;
        public AssemblyBuilder assembly;
        public AssemblyName assemblyName;
        public ModuleBuilder module;
        public TypeBuilder type, mainType;
        public MethodBuilder method, script;
        public FieldBuilder field;
        public ConstructorBuilder constructor;
        public ILGenerator ILGenerator
        {
            get => method.GetILGenerator();
        }

        public ContextInfo()
        {
            appName = "";
            outputType = PEFileKinds.ConsoleApplication;
            assemblyName = new AssemblyName();
            method = null;
            script = null;
            haveScript = true;
        }

        public void EndWrite()
        {
            try { type.CreateType(); }
            catch { }
            mainType.CreateType();
        }

        public void EndMethod()
        {
            //pass
        }

        public void CreateName()
        {
            int index = appName.LastIndexOf('.');
            if (index <= 0) assemblyName.Name = appName;
            else assemblyName.Name = appName.Substring(0, index);
        }

        public void CreateAssembly()
        {
            assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            module = assembly.DefineDynamicModule(assemblyName.Name);
            if (haveScript) mainType = module.DefineType(appName + "Main", TypeAttributes.Class);
        }

        public void InitilizateScript()
        {
            if (haveScript)
            {
                script = mainType.DefineMethod("Main", MethodAttributes.Private | MethodAttributes.Static, CallingConventions.Any,typeof(void), new Type[] { typeof(string[]) });
                method = script;
            }
            else
            {
                throw new InvalidOperationException("Cannot initilizate script in none-script program (TokensError)");
            }
        }

        public void EndScript()
        {
            if (haveScript)
            {
                ILGenerator.Emit(OpCodes.Ret);
                script = method;
                method = null;
                assembly.SetEntryPoint(script.GetBaseDefinition(), outputType);
            }
            else
            {
                throw new InvalidOperationException("Cannot end script in none-script program (TokensError)");
            }
        }

        public void LoadValue(string value)
        {
            if (value[0] == '\"' && value[value.Length - 1] == '\"')
            {
                ILGenerator.Emit(OpCodes.Ldstr, value.Substring(1, value.Length - 2));
            }
            else if (int.TryParse(value, out int a))
            {
                ILGenerator.Emit(OpCodes.Ldc_I4, a);
            }
            else if (long.TryParse(value, out long b))
            {
                ILGenerator.Emit(OpCodes.Ldc_I8, b);
            }
            else if (float.TryParse(value, out float c))
            {
                ILGenerator.Emit(OpCodes.Ldc_R4, c);
            }
            else if (double.TryParse(value, out double d))
            {
                ILGenerator.Emit(OpCodes.Ldc_R8, d);
            }
            else if (value == "null")
            {
                ILGenerator.Emit(OpCodes.Ldnull);
            }
            else if (value[0] == '[' && value[value.Length - 1] == ']')
            {
                value = value.Remove(0, 6);
                string typename = (string) value.TakeWhile((ch) => !char.IsWhiteSpace(ch));
                value = value.Remove(0, typename.Length);
                ILGenerator.Emit(OpCodes.Newarr, Type.GetType(typename));
            }
            else if (value.StartsWith("new "))
            {
                value = value.Remove(0, 4);
            }
            else
            {
                throw new InvalidCastException(value + " is not value (TokensError)");
            }
        }
    }
}
