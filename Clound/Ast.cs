namespace Clound
{
    public abstract class Ast { }

    public abstract class Stmt : Ast 
    { 
        public static readonly Stmt EmptyStmt = new EmptyStmt(); 
    }

    // Null Statement
    public class EmptyStmt : Stmt { }

    // var <ident> = <expr>
    public class DeclareVar : Stmt
    {
        public string Name;
        public Expr Expr;
        public System.Type Type;
    }

    public class Cls : Stmt { }

    // 返回语句
    public class Return : Stmt
    {
        public Expr Expr;
    }

    // print <expr>
    public class Print : Stmt
    {
        public Expr Expr;
    }

    // if <expr> then <stmt> end
    public class IfStmt : Stmt
    {
        public Expr Cond;
        public Stmt Then;
    }

    // if <expr> then <stmt> else <stmt> end
    public class IfElseStmt : Stmt
    {
        public Expr Cond;
        public Stmt Then;
        public Stmt Else;
    }

    // while <expr> do <stmt> end
    public class WhileLoop : Stmt
    {
        public Expr Cond;
        public Stmt Body;
    }

    // for <ident> = <expr> to <expr> do <stmt> end
    public class ForLoop : Stmt
    {
        public string Name;
        public Expr From;
        public Expr To;
        public Stmt Body;
    }

    public class Simple : Stmt
    {
        public Expr Expr;
        public Expr[] Args; // 实参列表
    }

    // 形式参数
    public class Param
    {
        public string Name;
        public System.Type Type;
    }

    // 函数定义
    public class Function : Stmt
    {
        public string Name;    // 函数名（可空）
        public Param[] ParamList;  // 形参列表（参数声明）
        public Stmt Body;       // 函数体
        public System.Type Type;  // 返回类型
    }

    // 语句块
    // <stmt> <NL> <stmt>
    public class Sequence : Stmt
    {
        public Stmt First;
        public Stmt Second;
    }

    // 导入语句
    public class ImportStmt : Stmt
    {
        public string Name;   // 名字空间
        public string Module; // 模块
    }

    // 表达式语句
    public abstract class Expr : Stmt { public static readonly Expr Undefined = new Undefined(); }

    // 空表达式
    public class Undefined : Expr { }

    // <string> := " <string_elem>* "
    public class StrLiteral : Expr
    {
        public string Value;
    }

    // <int> := <digit>+
    public class IntLiteral : Expr
    {
        public int Value;
    }

    public class BoolLiteral : Expr
    {
        public int Value;
    }

    // IDENTIFIER
    public class Variable : Expr
    {
        public string Name;
    }

    // <unary_exp> := <unary_op> <exp>
    public class UnaryExpr : Expr
    {
        public Expr Expr;
        public string Op;
    }

    // <bin_expr> := <expr> <bin_op> <expr>
    public class BinaryExpr : Expr
    {
        public Expr Left;
        public Expr Right;
        public string Op;
    }

    //public class AssignExpr : BinExpr

    // <unary_op> := + | -
    public enum UnaryOp
    {
        Plus,
        Minus
    }

    // <bin_op> := + | - | * | /
    public enum BinOp
    {
        Add,
        Sub,
        Mul,
        Div,
        Mod
    }

    //// 函数调用表达式
    public class PrimaryExpr : Expr
    {
        public Expr Lhs; // factor
        public Postfix Postfix;   // 后缀
    }

    public abstract class Postfix : Expr
    {

    }

    public class CallExpr : Postfix
    {
        public Expr[] Args;    // 实参表
        public Postfix Postfix;   // 后缀
    }

    // 成员访问表达式
    public class MemberAccessExpr : Postfix
    {
        public string Ident;    // 点号后面的成员
        public Postfix Postfix;   // 后缀
    }
}