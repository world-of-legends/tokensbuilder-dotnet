﻿using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using TokensAPI;
using System.Text;

namespace TokensBuilder
{
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
            byte entrypoint = 0;
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
                    case Token.NEWCLASS:
                        context.type = context.module.DefineType(name);
                        break;
                    case Token.NEWVAR:
                        break;
                    case Token.NEWFUNC:
                        break;
                    case Token.END:
                        context.ILGenerator.Emit(OpCodes.Br);
                        break;
                    case Token.GETCLASS:
                        break;
                    case Token.GETVAR:
                        break;
                    case Token.GETFUNC:
                        break;
                    case Token.RUNFUNC:
                        context.ILGenerator.Emit(OpCodes.Nop);
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
                        context.AddValue(name);
                        context.ILGenerator.Emit(OpCodes.Ret);
                        break;
                    case Token.IF:
                        break;
                    case Token.ELSE:
                        break;
                    case Token.ELIF:
                        break;
                    case Token.GOTO:
                        break;
                    case Token.LABEL:
                        Label label = context.ILGenerator.DefineLabel();
                        context.ILGenerator.MarkLabel(label);
                        labels.Add(name, label);
                        break;
                    case Token.YIELD:
                        break;
                    case Token.GETLINK:
                        break;
                    case Token.WRITEINPOINTER:
                        break;
                    case Token.NEWSTRUCT:
                        break;
                    case Token.NEWINTERFACE:
                        break;
                    case Token.NEWENUM:
                        break;
                    case Token.NEWMODULE:
                        break;
                    case Token.NEWCONSTRUCTOR:
                        break;
                    case Token.NEWATTRIBUTE:
                        break;
                    case Token.GETATTRIBUTE:
                        if (name == "Entrypoint")
                        {
                            entrypoint = 1;
                        }
                        break;
                    case Token.GETCONSTRUCTOR:
                        break;
                    case Token.OPCODEADD:
                        break;
                    case Token.NEWEVENT:
                        break;
                    case Token.GETEVENT:
                        break;
                    case Token.TRY:
                        break;
                    case Token.CATCH:
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
                        break;
                    case Token.CONST:
                        break;
                    case Token.OPERATOR:
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
                    case Token.ENDCLASS:
                        context.type.CreateType();
                        context.type = null;
                        break;
                    case Token.ENDMETHOD:
                        context.EndMethod();
                        break;
                    case Token.NAMESPACE:
                        namespace_name = e.args[0].GetValue();
                        break;
                    case Token.ENDNAMESPACE:
                        namespace_name = "";
                        break;
                }
            }

            //close context
            context.EndScript();
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
                script = mainType.DefineMethod("Main", MethodAttributes.Private | MethodAttributes.Static, typeof(void), new Type[] { typeof(string[]) });
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

        public void AddValue(string value)
        {
            if (value.StartsWith("var "))
            {
                ILGenerator.Emit(OpCodes.Ldfld, module.GetField(value.Remove(0, 4)));
            }
            else if (value.StartsWith("string "))
            {
                ILGenerator.Emit(OpCodes.Ldstr, value.Remove(0, 7));
            }
            else if (value.StartsWith("native int "))
            {
                ILGenerator.Emit(OpCodes.Ldind_I, value.Remove(0, 11));
            }
            else if (value == "null")
            {
                ILGenerator.Emit(OpCodes.Ldnull);
            }
            else
            {
                throw new InvalidCastException(value + " is not value (TokensError)");
            }
        }
    }
}
