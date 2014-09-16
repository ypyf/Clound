using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace Clound
{
    abstract class ASTree : IEnumerable<ASTree>
    {
        public abstract IEnumerator<ASTree> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        protected abstract ASTree child(int i);
        public abstract string Location();
        public abstract int Count { get; }
    }

    // 非叶子结点
    class ASTList : ASTree
    {
        protected List<ASTree> children;

        public ASTList(List<ASTree> children)
        {
            this.children = children;
        }

        protected override ASTree child(int i)
        {
            return children[i];
        }

        public override int Count 
        { 
            get 
            { 
                return children.Count; 
            } 
        }

        public override string Location()
        {
            foreach (var t in children)
            {
                string s = t.Location();
                if (s != null)
                    return s;
            }
            return null;
        }

        public override IEnumerator<ASTree> GetEnumerator()
        {
            return children.GetEnumerator();
        }
    }

    // 叶子节点
    class AstLeaf : ASTree
    {
        protected Token token;

        public Token Token
        {
            get { return token; }
        }

        private static List<ASTree> empty = new List<ASTree>();

        public AstLeaf(Token token)
        {
            this.token = token;
        }

        protected override ASTree child(int i)
        {
            throw new IndexOutOfRangeException();
        }

        public override int Count { get { return 0; } }

        public override string Location()
        {
            return "location: ...";
        }

        public override IEnumerator<ASTree> GetEnumerator()
        {
            return empty.GetEnumerator();
        }

        public override string ToString()
        {
            return token.Text;
        }
    }

    class IntegerLiteral : AstLeaf
    {
        public IntegerLiteral(Token t)
            :base(t)
        {
        }

        public int Value { get { return int.Parse(Token.Text); } }
    }

    class StringLiteral : AstLeaf
    {
        public StringLiteral(Token t)
            :base(t)
        {
        }

        public string Value { get { return Token.Text; } }
    }

    class Identifier : AstLeaf
    {
        public Identifier(Token t)
            :base(t)
        {
        }

        public string Name { get { return Token.Text; } }
    }

    class UnaryExpression : ASTList
    {
        public UnaryExpression(List<ASTree> expr) : base(expr) { }

        public ASTree Oprand { get { return child(0); } }

        public string Operator { get { return ((AstLeaf)child(1)).Token.Text; } }
    }

    class BinExpression : ASTList
    {
        public BinExpression(List<ASTree> expr) : base(expr) { }

        public ASTree Left { get { return child(0); } }

        public string Operator { get { return ((AstLeaf)child(1)).Token.Text; } }

        public ASTree Right { get { return child(2); } }
    }

    class PrimaryExpression : ASTList
    {
        protected PrimaryExpression(List<ASTree> expr) : base(expr) { }

        public static ASTree Create(List<ASTree> expr)
        {
            // where expr is identifier, literal...
            if (expr.Count == 1)
            {
                return expr[0];
            }
            else
            {
                return new PrimaryExpression(expr);
            }
        }
    }

    class NullStmt : ASTList
    {
        private static readonly NullStmt instance = new NullStmt();
        private NullStmt() : base(null) { }
        public static NullStmt Instance
        {
            get
            {
                return instance;
            }
        }
    }

    class BlockStmt : ASTList
    {
        public BlockStmt(List<ASTree> block) : base(block) { }
    }

    class IfStatement : ASTList
    {
        public IfStatement(List<ASTree> stmt) : base(stmt) { }

        public ASTree Condition
        {
            get { return child(0); }
        }

        public ASTree ThenBlock
        {
            get { return child(1); }
        }

        public ASTree ElseBlock
        {
            get { return children.Count > 2 ? child(2) : null; }
        }
    }

    class WhileStatement : ASTList
    {
        public WhileStatement(List<ASTree> stmt) : base(stmt) { }

        public ASTree Condition
        {
            get { return child(0); }
        }

        public ASTree Body
        {
            get { return child(0); }
        }
    }
}
