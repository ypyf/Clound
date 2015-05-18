using System.Collections.Generic;
using Reflect = System.Reflection;
using System.Reflection.Emit;
using IO = System.IO;
using System;

namespace Clound
{
    public class CodeGen
    {
        ModuleBuilder myModule = null;
        // ȫ�ֺ�����
        Dictionary<string, MethodBuilder> globalFunctions = new Dictionary<string, MethodBuilder>();

        public CodeGen(Ast ast, string moduleName)
        {
            // ��ʼ����!
            Compile(ast, moduleName);
        }

        private void Compile(Ast ast, string fileName)
        {
            string asmFileName = fileName + ".exe";

            // ������򼯣���ʾ��ִ���ļ���
            Reflect.AssemblyName name = new Reflect.AssemblyName(fileName);

            // �ӵ�ǰ���ȡ��̬����������
            AssemblyBuilder asmb = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save);

            // ��ȡģ��������
            // ע�⣬exe��ģ������������ɵĿ�ִ���ļ�ͬ��
            myModule = asmb.DefineDynamicModule(asmFileName);

            // ��ȡ�������� ������ΪMain���ڵ��ࣩ
            TypeBuilder typeBuilder = myModule.DefineType(fileName, Reflect.TypeAttributes.UnicodeClass);

            // ����������������Main������Ϊ��ڵ㣩
            MethodBuilder methb = typeBuilder.DefineMethod("Main", Reflect.MethodAttributes.Static, typeof(void), Type.EmptyTypes);

            // ��ȡ����������
            ILGenerator il = methb.GetILGenerator();

            // ����ȫ�ֻ���
            Env globals = new Env();

            // ���ɴ���
            GenASTree(ast, globals, il);

            il.Emit(OpCodes.Ret);
            typeBuilder.CreateType();
            myModule.CreateGlobalFunctions();
            asmb.SetEntryPoint(methb, PEFileKinds.ConsoleApplication);
            asmb.Save(asmFileName);
        }

        private void GenASTree(Ast ast, Env table, ILGenerator il)
        {
            if (ast is Stmt)
                GenStmt((Stmt)ast, table, il);
        }

        private void GenStmt(Stmt stmt, Env table, ILGenerator il)
        {
            if (stmt is Sequence)
            {
                Sequence seq = (Sequence)stmt;
                GenStmt(seq.First, table, il);
                GenStmt(seq.Second, table, il);
            }
            else if (stmt is ImportStmt)
            {
                // ֻ�ܵ���.Netģ��
                ImportStmt import = (ImportStmt)stmt;
                Reflect.Assembly asm = null;
                if (import.Name == "System")
                {
                    // �����ں���ģ���в���
                    // ��ȡint�������ڵĳ���(mscorlib.dll)
                    asm = Reflect.Assembly.GetAssembly(typeof(int));
                }
                else
                {
                    string root = Framework.GetFrameworkDirectory();
                    string assembly = IO.Path.Combine(root, import.Name + ".dll");
                    asm = Reflect.Assembly.LoadFile(assembly);  // TODO ������
                }
                table.AddModule(new Module(import.Name, asm));
            }
            else if (stmt is DeclareVar)
            {
                DeclareVar declare = (DeclareVar)stmt;
                Type type = declare.Type;
                if (type == null)
                {
                    type = TypeOfExpr(declare.Expr, table);
                }
                if (table.IsDeclared(declare.Name))
                {
                    throw new Exception(String.Format("A local variable named '{0}' is already defined in this scope", declare.Name));
                }

                // TODO ���汾�ر�����λ������
                table[declare.Name] = new LocalVariable(il.DeclareLocal(type));

                // set the initial value
                BinaryExpr be = new BinaryExpr();
                Variable var = new Variable();
                var.Name = declare.Name;
                be.Left = var;

                be.Op = "=";
                if (declare.Expr is Undefined)
                {
                    // FIXME �������������������͸���ֵ
                    IntLiteral expr = new IntLiteral();
                    expr.Value = 0;
                    be.Right = expr;
                }
                else
                {
                    be.Right = declare.Expr;
                }
                GenExpr(be, table, il);
            }
            else if (stmt is Return)
            {
                Return ret = (Return)stmt;
                GenExpr(ret.Expr, table, il);
            }
            else if (stmt is Function)
            {
                // ����ȫ�ֺ���
                DefineGlobalMethod((Function)stmt, table);
            }
            else if (stmt is Print)
            {
                Print print = (Print)stmt;
                GenExpr(print.Expr, table, il);
                Reflect.MethodInfo mi = typeof(Console).GetMethod("WriteLine", new Type[] { TypeOfExpr(print.Expr, table) });
                il.Emit(OpCodes.Call, mi);
            }
            else if (stmt is IfStmt)
            {
                IfStmt ifStmt = (IfStmt)stmt;

                // �����Ľ���λ��
                Label End = il.DefineLabel();

                // �����������ʽ
                GenExpr(ifStmt.Cond, table, il);
                il.Emit(OpCodes.Brfalse, End);

                // then�����
                //il.BeginScope();
                GenStmt(ifStmt.Then, table, il);
                //il.EndScope();
                il.MarkLabel(End);
            }
            else if (stmt is IfElseStmt)
            {
                IfElseStmt ifElseStmt = (IfElseStmt)stmt;

                // ��֧��ǩ
                Label ThenEnd = il.DefineLabel();
                Label End = il.DefineLabel();

                // �������ʽ
                GenExpr(ifElseStmt.Cond, table, il);
                il.Emit(OpCodes.Brfalse, ThenEnd);

                // then��֧
                GenStmt(ifElseStmt.Then, table, il);
                il.Emit(OpCodes.Br, End);
                il.MarkLabel(ThenEnd);

                // else��֧
                GenStmt(ifElseStmt.Else, table, il);
                il.MarkLabel(End);
            }
            else if (stmt is WhileLoop)
            {
                WhileLoop whileLoop = (WhileLoop)stmt;

                Label LoopStart = il.DefineLabel();
                Label LoopEnd = il.DefineLabel();

                il.MarkLabel(LoopStart);
                GenExpr(whileLoop.Cond, table, il);
                il.Emit(OpCodes.Brfalse, LoopEnd);
                GenStmt(whileLoop.Body, table, il);
                il.Emit(OpCodes.Br, LoopStart);
                il.MarkLabel(LoopEnd);
            }
            //else if (stmt is ForLoop)
            //{
            //    ForLoop forLoop = (ForLoop)stmt;
            //    Assign assign = new Assign();
            //    assign.Ident = forLoop.Name;
            //    assign.Expr = forLoop.From;
            //    GenStmt(assign, table, il);
            //    // jump to the test
            //    Label test = il.DefineLabel();
            //    il.Emit(OpCodes.Br, test);

            //    // statements in the body of the for loop
            //    Label body = il.DefineLabel();
            //    il.MarkLabel(body);
            //    GenStmt(forLoop.Body, table, il);

            //    // to (increment the value of x)
            //    il.Emit(OpCodes.Ldloc, table[forLoop.Name]);
            //    il.Emit(OpCodes.Ldc_I4, 1);
            //    il.Emit(OpCodes.Add);
            //    Store(forLoop.Name, table, il);

            //    // **test** does x equal 100? (do the test)
            //    il.MarkLabel(test);
            //    il.Emit(OpCodes.Ldloc, table[forLoop.Name]);
            //    GenExpr(forLoop.To, table, il);
            //    il.Emit(OpCodes.Blt, body);
            //}
            //else if (stmt is Expr)
            //{
            //    GenExpr((Expr)stmt, table, il);
            //}
            else if (stmt is Simple)
            {
                Simple simple = (Simple)stmt;
                GenExpr(simple.Expr, table, il);
                // TODO �����ŵĺ�������
            }
            else if (stmt is EmptyStmt)
            {
                // empty statement
                // do nothing
            }
            else
            {
                throw new Exception("don't know how to gen a " + stmt.GetType().Name);
            }
        }

        // �������
        private void Load(string name, Env table, ILGenerator il)
        {
            if (table.HasInstance(name))
            {
                Symbol sym = table[name];
                sym.Load(il);
            }
            else
            {
                throw new Exception("load: undeclared variable '" + name + "'");
            }
        }

        // ������ֵ
        private void Store(string name, Env table, ILGenerator il)
        {
            if (table.HasInstance(name))
            {
                Symbol sym = table[name];
                sym.Store(il);
            }
            else
            {
                throw new Exception("store: undeclared variable '" + name + "'");
            }
        }

        private void GenExpr(Expr expr, Env table, ILGenerator il)
        {
            if (expr is StrLiteral)
            {
                il.Emit(OpCodes.Ldstr, ((StrLiteral)expr).Value);
            }
            else if (expr is IntLiteral)
            {
                il.Emit(OpCodes.Ldc_I4, ((IntLiteral)expr).Value);
            }
            else if (expr is BoolLiteral)
            {
                il.Emit(OpCodes.Ldc_I4, ((BoolLiteral)expr).Value);
            }
            else if (expr is Variable)
            {
                string name = ((Variable)expr).Name;
                Load(name, table, il);
            }
            else if (expr is PrimaryExpr)
            {
                PrimaryExpr pe = (PrimaryExpr)expr;
                //GenExpr(pe.Lhs, table, il);
                if (pe.Lhs is Variable) // LHS��ʱ�����������ռ䡢��������ʵ����������������������
                {
                    string name = ((Variable)pe.Lhs).Name;
                    if (pe.Postfix is CallExpr)
                    {
                        // TODO Ŀǰ��������ʽ����
                        CallExpr ce = (CallExpr)pe.Postfix;
                        // ��������
                        if (globalFunctions.ContainsKey(name))
                        {
                            // �������
                            foreach (var arg in ce.Args)
                            {
                                GenExpr(arg, table, il);
                            }
                            // ���ú���
                            Reflect.MethodInfo mi = globalFunctions[name];
                            il.Emit(OpCodes.Call, mi);
                        }
                        else if (table.HasInstance(name))
                        {
                            throw new Exception(String.Format("'{0}'�Ǳ��������Ǻ���", name));
                        }
                        else
                        {
                            throw new Exception(String.Format("��ǰ�������в���������'{0}'", name));
                        }
                    }
                    else if (pe.Postfix is MemberAccessExpr)
                    {
                        // Ŀǰֻʵ���˵���ģ�飬��û��ʵ��Class
                        MemberAccessExpr mae = (MemberAccessExpr)pe.Postfix;
                        if (mae.Postfix is MemberAccessExpr)
                        {
                            string ns = ns = name + "." + mae.Ident; ;
                            // �ϲ������ռ�(CLR��������������ʱ�������ռ��һ����)
                            PrimaryExpr exp = new PrimaryExpr();
                            Variable var = new Variable();
                            var.Name = ns;
                            exp.Lhs = var;
                            exp.Postfix = mae.Postfix;
                            // �ݹ����ɺϲ���ı��ʽ
                            GenExpr(exp, table, il);
                        }
                        else if (mae.Postfix is CallExpr)
                        {
                            // ����C#ģ���еķ���
                            CallExpr ce = (CallExpr)mae.Postfix;
             
                            // ������ģ���м�������ͺͷ���
                            Type type = FirstMatchType(name, table);
                            if (type != null)
                            {
                                // �������
                                List<Type> typelist = new List<Type>();
                                foreach (var arg in ce.Args)
                                {
                                    typelist.Add(TypeOfExpr(arg, table));
                                    GenExpr(arg, table, il);
                                }
                                // ���ú���
                                Reflect.MethodInfo mi = type.GetMethod(mae.Ident, typelist.ToArray());
                                if (mi != null)
                                {
                                    il.Emit(OpCodes.Call, mi);
                                }
                                else
                                {
                                    throw new Exception(String.Format("����{0}δ����", mae.Ident));
                                }
                            }
                            else
                            {
                                throw new Exception(String.Format("����{0}δ����", name));
                            }
                        }
                        else
                        {
                            //��ʱ Postfix == null
                            // ��������
                            // ���� a.Length
                            throw new NotImplementedException();
                        }
                    }
                }
                else
                { 
                    // TODO OO�����У����Կ������������Ϸ��ʷ���
                    // ���� 10.to_s() => "10"
                    throw new NotImplementedException();
                }
            }
            else if (expr is BinaryExpr)
            {
                BinaryExpr be = (BinaryExpr)expr;
                // ��ֵ
                if (be.Op.Equals("="))
                {
                    GenExpr(be.Right, table, il);
                    if (be.Left is Variable)
                    {
                        Store(((Variable)be.Left).Name, table, il);
                    }
                    else
                    {
                        throw new Exception("except a lvalue at assign");
                    }
                }
                else
                {
                    // ��������
                    // FIXME ILû��һЩ�������㣬��Ҫ���������ʵ��
                    GenExpr(be.Left, table, il);
                    GenExpr(be.Right, table, il);
                    switch (be.Op)
                    {
                        case "+":
                            il.Emit(OpCodes.Add);
                            break;
                        case "-":
                            il.Emit(OpCodes.Sub);
                            break;
                        case "*":
                            il.Emit(OpCodes.Mul);
                            break;
                        case "/":
                            il.Emit(OpCodes.Div);
                            break;
                        case "%":
                            il.Emit(OpCodes.Rem);
                            break;
                        case "<":
                            il.Emit(OpCodes.Clt);
                            break;
                        case ">":
                            il.Emit(OpCodes.Cgt);
                            break;
                        case "==":
                            il.Emit(OpCodes.Ceq);
                            break;
                        case "<=":
                            //il.Emit(OpCodes.Cgt);   // FIXME
                            //break;
                        case ">=":
                            //il.Emit(OpCodes.Clt);   // FIXME
                            //break;
                        case "!=":
                            //il.Emit(OpCodes.Ceq);   // FIXME
                            //break;
                        default:
                            throw new Exception("Unrecognized operator: " + be.Op);
                    }
                }
            }
            else if (expr is Undefined)
            {
                return;
            }
            else
            {
                throw new Exception("don't know how to generate " + expr.GetType().Name);
            }
        }

        private Type FirstMatchType(string typeName, Env table)
        {
            foreach (Module m in table.GetModules())
            {
                foreach (Type t in m.Assembly.GetTypes())
                {
                    // .Net��һ�����͵�ȫ�� = �����ռ� + ������
                    // ���С������ռ䡱������ͨ��importָ�������
                    // ��System.ConsoleΪ����������������
                    // һ�������û�������ռ��޶���
                    // ��ʱtypeName = "Console"
                    // Ҳ���Դ�����ȫ�޶���
                    // ��ʱ typeName = "System.Console"�������ǲ���Ҫimportָ�����������ռ�
                    if (t.IsClass && ((t.Namespace == m.Name && t.Name == typeName) || (t.Namespace + "." + t.Name == typeName)))
                    {
                        //Console.WriteLine(t.Namespace);
                        return t;
                    }
                }
            }
            return null;    // not found
        }

        private Type TypeOfExpr(Expr expr, Env table)
        {
            if (expr is StrLiteral)
            {
                return typeof(string);
            }
            else if (expr is IntLiteral)
            {
                return typeof(int);
            }
            else if (expr is Variable)
            {
                Variable var = (Variable)expr;
                if (table.HasInstance(var.Name))
                {
                    return table[var.Name].SymbolType;
                }
                else
                {
                    throw new Exception("undeclared variable '" + var.Name + "'");
                }
            }
            else if (expr is BoolLiteral)
            {
                return typeof(bool);
            }
            else if (expr is CallExpr)
            {
                // TODO ��麯������
                return typeof(object);
            }
            else if (expr is MemberAccessExpr)
            {
                MemberAccessExpr mae = (MemberAccessExpr)expr;
                return TypeOfExpr(mae.Postfix, table); 
            }
            else if (expr is PrimaryExpr)
            { 
                PrimaryExpr pe = (PrimaryExpr)expr;
                return TypeOfExpr(pe.Postfix, table);
            }
            //else if (expr is Undefined)
            //{
            //    return typeof(object);
            //}
            else
            {
                throw new Exception("don't know how to calculate the type of " + expr.GetType().Name);
            }
        }

        // ����ȫ�ֺ���
        private void DefineGlobalMethod(Function func, Env globals)
        {
            // �õ������β�����
            List<Type> tlist = new List<Type>();
            foreach (var p in func.ParamList)
            {
                tlist.Add(p.Type);
            }
            Type[] types = tlist.ToArray();
            if (types.Length == 0)
                types = Type.EmptyTypes;

            MethodBuilder mb = myModule.DefineGlobalMethod(func.Name, Reflect.MethodAttributes.Assembly | Reflect.MethodAttributes.Static, func.Type, types);
            if (!globalFunctions.ContainsKey(func.Name))
            {
                globalFunctions.Add(func.Name, mb);
            }
            else
            {
                throw new Exception(String.Format("�ظ���ͬ��ȫ�ֺ���:{0}", func.Name));
            }

            Env locals = new Env(globals);

            // �����������βΣ������뵽���ػ���
            for (int i = 0; i < func.ParamList.Length; i++)
            {
                string name = func.ParamList[i].Name;
                ParameterBuilder pb = mb.DefineParameter(i + 1, Reflect.ParameterAttributes.None, name);
                locals.Add(pb.Name, new Parameter(pb, types[i]));
            }

            // ���ɺ�����
            // TODO ��麯���ķ�������
            ILGenerator il = mb.GetILGenerator();
            GenStmt(func.Body, locals, il);
            il.Emit(OpCodes.Ret);
        }
    }
}