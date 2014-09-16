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

    // �������
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
        public Expr[] Args; // ʵ���б�
    }

    // ��ʽ����
    public class Param
    {
        public string Name;
        public System.Type Type;
    }

    // ��������
    public class Function : Stmt
    {
        public string Name;    // ���������ɿգ�
        public Param[] ParamList;  // �β��б�����������
        public Stmt Body;       // ������
        public System.Type Type;  // ��������
    }

    // ����
    // <stmt> <NL> <stmt>
    public class Sequence : Stmt
    {
        public Stmt First;
        public Stmt Second;
    }

    // �������
    public class ImportStmt : Stmt
    {
        public string Name;   // ���ֿռ�
        public string Module; // ģ��
    }

    // ���ʽ���
    public abstract class Expr : Stmt { public static readonly Expr Undefined = new Undefined(); }

    // �ձ��ʽ
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

    //// �������ñ��ʽ
    public class PrimaryExpr : Expr
    {
        public Expr Lhs; // factor
        public Postfix Postfix;   // ��׺
    }

    public abstract class Postfix : Expr
    {

    }

    public class CallExpr : Postfix
    {
        public Expr[] Args;    // ʵ�α�
        public Postfix Postfix;   // ��׺
    }

    // ��Ա���ʱ��ʽ
    public class MemberAccessExpr : Postfix
    {
        public string Ident;    // ��ź���ĳ�Ա
        public Postfix Postfix;   // ��׺
    }
}