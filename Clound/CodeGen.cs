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
        // 全局函数表
        Dictionary<string, MethodBuilder> globalFunctions = new Dictionary<string, MethodBuilder>();

        public CodeGen(Ast ast, string moduleName)
        {
            // 开始编译!
            Compile(ast, moduleName);
        }

        private void Compile(Ast ast, string fileName)
        {
            string asmFileName = fileName + ".exe";

            // 定义程序集（表示可执行文件）
            Reflect.AssemblyName name = new Reflect.AssemblyName(fileName);

            // 从当前域获取动态编译生成器
            AssemblyBuilder asmb = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save);

            // 获取模块生成器
            // 注意，exe的模块名必须和生成的可执行文件同名
            myModule = asmb.DefineDynamicModule(asmFileName);

            // 获取类生成器 （将作为Main所在的类）
            TypeBuilder typeBuilder = myModule.DefineType(fileName, Reflect.TypeAttributes.UnicodeClass);

            // 方法生成器（生成Main函数作为入口点）
            MethodBuilder methb = typeBuilder.DefineMethod("Main", Reflect.MethodAttributes.Static, typeof(void), Type.EmptyTypes);

            // 获取代码生成器
            ILGenerator il = methb.GetILGenerator();

            // 创建全局环境
            Env globals = new Env();

            // 生成代码
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
                // 只能导入.Net模块
                ImportStmt import = (ImportStmt)stmt;
                Reflect.Assembly asm = null;
                if (import.Name == "System")
                {
                    // 首先在核心模块中查找
                    // 获取int类型所在的程序集(mscorlib.dll)
                    asm = Reflect.Assembly.GetAssembly(typeof(int));
                }
                else
                {
                    string root = Framework.GetFrameworkDirectory();
                    string assembly = IO.Path.Combine(root, import.Name + ".dll");
                    asm = Reflect.Assembly.LoadFile(assembly);  // TODO 错误处理
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

                // TODO 保存本地变量的位置索引
                table[declare.Name] = new LocalVariable(il.DeclareLocal(type));

                // set the initial value
                BinaryExpr be = new BinaryExpr();
                Variable var = new Variable();
                var.Name = declare.Name;
                be.Left = var;

                be.Op = "=";
                if (declare.Expr is Undefined)
                {
                    // FIXME 根据所声明变量的类型赋初值
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
                // 定义全局函数
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

                // 代码块的结束位置
                Label End = il.DefineLabel();

                // 生成条件表达式
                GenExpr(ifStmt.Cond, table, il);
                il.Emit(OpCodes.Brfalse, End);

                // then代码块
                //il.BeginScope();
                GenStmt(ifStmt.Then, table, il);
                //il.EndScope();
                il.MarkLabel(End);
            }
            else if (stmt is IfElseStmt)
            {
                IfElseStmt ifElseStmt = (IfElseStmt)stmt;

                // 分支标签
                Label ThenEnd = il.DefineLabel();
                Label End = il.DefineLabel();

                // 条件表达式
                GenExpr(ifElseStmt.Cond, table, il);
                il.Emit(OpCodes.Brfalse, ThenEnd);

                // then分支
                GenStmt(ifElseStmt.Then, table, il);
                il.Emit(OpCodes.Br, End);
                il.MarkLabel(ThenEnd);

                // else分支
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
                // TODO 无括号的函数调用
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

        // 载入变量
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

        // 变量赋值
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
                if (pe.Lhs is Variable) // LHS此时可能是命名空间、类型名、实例变量（包括函数名）等
                {
                    string name = ((Variable)pe.Lhs).Name;
                    if (pe.Postfix is CallExpr)
                    {
                        // TODO 目前不考虑链式调用
                        CallExpr ce = (CallExpr)pe.Postfix;
                        // 函数调用
                        if (globalFunctions.ContainsKey(name))
                        {
                            // 载入参数
                            foreach (var arg in ce.Args)
                            {
                                GenExpr(arg, table, il);
                            }
                            // 调用函数
                            Reflect.MethodInfo mi = globalFunctions[name];
                            il.Emit(OpCodes.Call, mi);
                        }
                        else if (table.HasInstance(name))
                        {
                            throw new Exception(String.Format("'{0}'是变量，不是函数", name));
                        }
                        else
                        {
                            throw new Exception(String.Format("当前上下文中不存在名称'{0}'", name));
                        }
                    }
                    else if (pe.Postfix is MemberAccessExpr)
                    {
                        // 目前只实现了导入模块，还没有实现Class
                        MemberAccessExpr mae = (MemberAccessExpr)pe.Postfix;
                        if (mae.Postfix is MemberAccessExpr)
                        {
                            string ns = ns = name + "." + mae.Ident; ;
                            // 合并命名空间(CLR的类型名在运行时是命名空间的一部分)
                            PrimaryExpr exp = new PrimaryExpr();
                            Variable var = new Variable();
                            var.Name = ns;
                            exp.Lhs = var;
                            exp.Postfix = mae.Postfix;
                            // 递归生成合并后的表达式
                            GenExpr(exp, table, il);
                        }
                        else if (mae.Postfix is CallExpr)
                        {
                            // 调用C#模块中的方法
                            CallExpr ce = (CallExpr)mae.Postfix;
             
                            // 在所有模块中间查找类型和方法
                            Type type = FirstMatchType(name, table);
                            if (type != null)
                            {
                                // 载入参数
                                List<Type> typelist = new List<Type>();
                                foreach (var arg in ce.Args)
                                {
                                    typelist.Add(TypeOfExpr(arg, table));
                                    GenExpr(arg, table, il);
                                }
                                // 调用函数
                                Reflect.MethodInfo mi = type.GetMethod(mae.Ident, typelist.ToArray());
                                if (mi != null)
                                {
                                    il.Emit(OpCodes.Call, mi);
                                }
                                else
                                {
                                    throw new Exception(String.Format("方法{0}未发现", mae.Ident));
                                }
                            }
                            else
                            {
                                throw new Exception(String.Format("类型{0}未发现", name));
                            }
                        }
                        else
                        {
                            //此时 Postfix == null
                            // 访问属性
                            // 例如 a.Length
                            throw new NotImplementedException();
                        }
                    }
                }
                else
                { 
                    // TODO OO语言中，可以考虑在字面量上访问方法
                    // 例如 10.to_s() => "10"
                    throw new NotImplementedException();
                }
            }
            else if (expr is BinaryExpr)
            {
                BinaryExpr be = (BinaryExpr)expr;
                // 赋值
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
                    // 算术运算
                    // FIXME IL没有一些条件运算，需要单独提出来实现
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
                    // .Net中一个类型的全名 = 命名空间 + 类型名
                    // 其中“命名空间”部分是通过import指令引入的
                    // 以System.Console为例，这里分两种情况
                    // 一种情况是没有命名空间限定的
                    // 此时typeName = "Console"
                    // 也可以处理完全限定名
                    // 此时 typeName = "System.Console"，这样是不需要import指令引入命名空间
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
                // TODO 检查函数类型
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

        // 定义全局函数
        private void DefineGlobalMethod(Function func, Env globals)
        {
            // 得到所有形参类型
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
                throw new Exception(String.Format("重复的同名全局函数:{0}", func.Name));
            }

            Env locals = new Env(globals);

            // 构建函数的形参，并加入到本地环境
            for (int i = 0; i < func.ParamList.Length; i++)
            {
                string name = func.ParamList[i].Name;
                ParameterBuilder pb = mb.DefineParameter(i + 1, Reflect.ParameterAttributes.None, name);
                locals.Add(pb.Name, new Parameter(pb, types[i]));
            }

            // 生成函数体
            // TODO 检查函数的返回类型
            ILGenerator il = mb.GetILGenerator();
            GenStmt(func.Body, locals, il);
            il.Emit(OpCodes.Ret);
        }
    }
}