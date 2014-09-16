using System.Collections.Generic;
using Text = System.Text;
using System;

namespace Clound
{
    public sealed class Parser
    {
        private Operators operators = new Operators();    // 操作符表
        private Token lookahead;   // 向前看终结符
        private Scanner scanner;

        private readonly Ast result;

        public Ast Result
        {
            get { return result; }
        }

        private void Match(string terminal)
        {
            if (IsToken(terminal))
            {
                lookahead = scanner.Scan();
            }
            else
            {
                throw new System.Exception(String.Format("syntax error: expect a '{0}'", terminal));
            }
        }

        private void Match(Type token)
        {
            if (lookahead.GetType() == token)
            {
                lookahead = scanner.Scan();
            }
            else
            {
                throw new System.Exception(String.Format("syntax error: expect a '{0}'", token));
            }
        }

        // 返回指定类型的Token，否则抛出异常
        private Token Expect(Type token)
        {
            if (lookahead.GetType() == token)
            {
                return lookahead;
            }
            else
            {
                throw new System.Exception(String.Format("syntax error: expect a '{0}'", token));
            }
        }

        private void EatToken()
        {
            lookahead = scanner.Scan();
        }

        private bool IsToken(string terminal)
        {
            return terminal.Equals(lookahead.Text);
        }

        // 语句分隔符
        private bool IsDelimiter()
        {
            return lookahead == Token.EOL || IsToken(";");
        }

        private void InitOperators()
        {
            // 数字越大，优先级越高
            operators.Add("=", 1, Operators.Right);
            operators.Add("==", 2, Operators.Left);
            operators.Add("!=", 2, Operators.Left);
            operators.Add("<", 3, Operators.Left);
            operators.Add(">", 3, Operators.Left);
            operators.Add("<=", 3, Operators.Left);
            operators.Add(">=", 3, Operators.Left);
            operators.Add("+", 4, Operators.Left);
            operators.Add("-", 4, Operators.Left);
            operators.Add("*", 5, Operators.Left);
            operators.Add("/", 5, Operators.Left);
            operators.Add("%", 5, Operators.Left);
            //operators.Add(".", 10, Operators.Left);
        }

        public Parser(Scanner scanner)
        {
            InitOperators();
            this.scanner = scanner;
            lookahead = scanner.Scan();
            result = Program();
        }

        private Ast Program()
        {
            return ParseStmt();
        }

        private Stmt ParseStmt()
        {
            Stmt stmt = null;

            while (IsDelimiter())
            {
                EatToken();
            }

            if (lookahead == Token.EOF)
                return Stmt.EmptyStmt;

            // 声明语句
            if (IsToken("var"))
            {
                EatToken();
                stmt = DeclareVar();
            }
            else if (IsToken("import"))
            {
                stmt = ImportStmt();
            }
            else if (IsToken("func"))
            {
                EatToken();
                stmt = Function();
            }
            else if (IsToken("print"))
            {
                EatToken();
                stmt = PrintStmt();
            }
            else if (IsToken("if"))
            {
                EatToken();
                stmt = IfStmt();
            }
            else if (IsToken("while"))
            {
                EatToken();
                stmt = WhileStmt();
            }
            else if (IsToken("return"))
            {
                EatToken();
                stmt = ReturnStmt();
            }
            else if (IsToken("}"))
            {
                return Stmt.EmptyStmt;
            }
            else
            {
                stmt = Simple();  // 表达式语句
            }
            //else
            //{
            //    throw new System.Exception(String.Format("unknown statement: {0}", lookahead.Text));
            //}

            if (IsDelimiter())
            {
                EatToken();
                Sequence seq = new Sequence();
                seq.First = stmt;
                seq.Second = ParseStmt();
                return seq;
            }
            else if (lookahead == Token.EOF)
            {
                return stmt;
            }
            else
            {
                throw new System.Exception("excpet a EOL or ';'");
            }
        }

        // import :=  "import" STRING [ "{" <ident> { "," <ident> } "}" ]
        // import "System"
        private Stmt ImportStmt()
        {
            Match("import");
            ImportStmt import = new ImportStmt();
            import.Name = ((StrToken)Expect(typeof(StrToken))).Text;
            EatToken();
            return import;
        }

        private Stmt ReturnStmt()
        {
            Return stmt = new Return();
            stmt.Expr = ParseExpr();
            return stmt;
        }

        private DeclareVar DeclareVar()
        {
            DeclareVar declareVar = new DeclareVar();
            declareVar.Name = lookahead.Text;
            Match(typeof(IdentToken));

            // 未指定类型
            if (IsToken("="))
            {
                EatToken();
                //declareVar.Type = typeof(object);
            }
            else if (IsToken(":")) // 带类型的变量声明
            {
                EatToken();
                declareVar.Type = TypeSpec();

                if (IsToken("="))
                {
                    EatToken();
                }
            }
            else
            {
                throw new System.Exception("syntax error: expected type spec or identifier");
            }

            declareVar.Expr = ParseExpr();
            return declareVar;
        }

        // 函数定义
        private Stmt Function()
        {
            Function func = new Function();
            func.Name = lookahead.Text;
            Match(typeof(IdentToken));
            Match("(");
            func.ParamList = ParamList();
            Match(")");
            if (IsToken(":"))
            {
                EatToken();
                func.Type = TypeSpec();
            }
            else
            {
                func.Type = typeof(void);
            }
            func.Body = Block();
            
            return func;
        }

        //param_list := "(" [ <params> ] ")"
        //params := <param> { "," <param> }
        //param := <ident> ":" <type_spec>
        private Param[] ParamList()
        {
            List<Param> result = new List<Param>();

            while (!IsToken(")"))
            {
                Param p = new Param();
                p.Name = lookahead.Text;
                Match(typeof(IdentToken));
                Match(":");
                p.Type = TypeSpec();
                if (IsToken(","))
                {
                    EatToken();
                    if (IsToken(")"))
                    {
                        throw new Exception("syntax error: unexpected ',' at parameters list");
                    }
                }
                result.Add(p);
            }

            return result.ToArray();
        }

        private Expr[] ArgList()
        {
            List<Expr> args = new List<Expr>();
            while (!IsToken(")"))
            {
                Expr exp = ParseExpr();
                args.Add(exp);
                if (IsToken(","))
                {
                    EatToken();
                    if (IsToken(")"))
                    {
                        throw new Exception("syntax error: unexpected ',' at the end of statement");
                    }
                }
            }
            return args.ToArray();
        }

        // 表达式语句
        private Stmt Simple()
        {
            Simple stmt = new Simple();
            stmt.Expr = ParseExpr();
            // FIXME
            //stmt.Args = ArgList();
            return stmt;
        }

        private Expr ParseExpr()
        {
            Expr right = factor();
            Precedence next;
            while ((next = nextOperator()) != null)
            {
                right = doShift(right, next.Level);
            }
            return right;
        }

        private Expr factor()
        {
            if (IsToken("-"))
            {
                EatToken();
                UnaryExpr ue = new UnaryExpr();
                ue.Op = "-";
                ue.Expr = primary();
                if (ue.Expr == Expr.Undefined)
                    throw new Exception("无效的表达式项");
                return ue;
            }
            else
            {
                return primary();
            }
        }

        //primary := ( "(" <expr> ")" | INT | STRING | BOOL | IDENTIFIER ) { <postfix> }
        private Expr primary()
        {
            PrimaryExpr exp = new PrimaryExpr();
            if (IsToken("("))
            {
                EatToken();
                exp.Lhs = ParseExpr();
                Match(")");
            }
            else if (lookahead is IntToken)
            {
                IntLiteral n = new IntLiteral();
                n.Value = int.Parse(lookahead.Text);
                EatToken();
                exp.Lhs = n;
            }
            else if (lookahead is StrToken)
            {
                StrLiteral n = new StrLiteral();
                n.Value = lookahead.Text;
                EatToken();
                exp.Lhs = n;
            }
            else if (lookahead is IdentToken)
            {
                if (IsToken("true"))
                {
                    EatToken();
                    BoolLiteral boolLiteral = new BoolLiteral();
                    boolLiteral.Value = 1;
                    exp.Lhs = boolLiteral;
                }
                else if (IsToken("false"))
                {
                    EatToken();
                    BoolLiteral boolLiteral = new BoolLiteral();
                    boolLiteral.Value = 0;
                    exp.Lhs = boolLiteral;
                }
                else
                {
                    string ident = lookahead.Text;
                    EatToken();
                    Variable var = new Variable();
                    var.Name = ident;
                    exp.Lhs = var;
                }
            }
            else if (IsDelimiter())
            {
                // 空表达式
                exp.Lhs = Expr.Undefined;
            }
            else
            {
                throw new Exception("syntax error: unrecognized expression");
            }

            exp.Postfix = Postfix();
            if (exp.Postfix == null)
                return exp.Lhs;
            return exp;
        }

        private Postfix Postfix()
        {
            if (IsToken("("))
            {
                Match("(");
                CallExpr ce = new CallExpr();
                ce.Args = ArgList();
                ce.Postfix = Postfix();
                Match(")");
                return ce;
            }
            else if (IsToken("."))
            {
                Match(".");
                MemberAccessExpr mae = new MemberAccessExpr();
                mae.Ident = ((IdentToken)Expect(typeof(IdentToken))).Text;
                EatToken();
                mae.Postfix = Postfix();
                return mae;
            }
            else
            {
                return null;
            }
        }

        private Precedence nextOperator()
        {
            string op = lookahead.Text;
            if (operators.ContainsKey(op))
            {
                return operators[op];
            }
            return null;
        }

        private bool rightIsExpr(int level, Precedence nextPrec)
        {
            if (nextPrec.LeftAssoc())
            {
                return level < nextPrec.Level;
            }
            else
            {
                return level <= nextPrec.Level;
            }
        }

        private Expr doShift(Expr left, int level)
        {
            string op = lookahead.Text;
            EatToken();
            Expr right = factor();
            Precedence next;
            while ((next = nextOperator()) != null && rightIsExpr(level, next))
            {
                right = doShift(right, next.Level);
            }
            BinaryExpr exp = new BinaryExpr();
            exp.Left = left;
            exp.Right = right;
            exp.Op = op;
            return exp;
        }

        // TODO 自定义类型的分析
        private Type TypeSpec()
        {
            Type t = null;
            if (lookahead.Text.Equals("int"))
                t = typeof(int);
            else if (lookahead.Text.Equals("string"))
                t = typeof(string);
            else if (lookahead.Text.Equals("bool"))
                t = typeof(bool);
            else
                t = typeof(object);
            EatToken();
            return t;
        }

        private Stmt PrintStmt()
        {
            Print print = new Print();
            print.Expr = ParseExpr();
            return print;
        }

        private Stmt Block()
        {
            Match("{");
            Stmt block = ParseStmt();
            Match("}");
            return block;
        }

        private Stmt IfStmt()
        {
            Expr cond = ParseExpr();

            Stmt thenBranch = Block();

            if (IsToken("else"))
            {
                EatToken();
                Stmt elseBranch = Block();
                IfElseStmt stmt = new IfElseStmt();
                stmt.Cond = cond;
                stmt.Then = thenBranch;
                stmt.Else = elseBranch;
                return stmt;
            }
            else
            {
                IfStmt stmt = new IfStmt();
                stmt.Cond = cond;
                stmt.Then = thenBranch;
                return stmt;
            }
        }

        private Stmt WhileStmt()
        {
            WhileLoop whileLoop = new WhileLoop();
            whileLoop.Cond = ParseExpr();
            whileLoop.Body = Block();
            return whileLoop;
        }
    }
}
