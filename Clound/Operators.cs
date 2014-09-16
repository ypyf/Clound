using System;
using System.Collections.Generic;
using System.Text;

namespace Clound
{
    // 运算符的优先级
    public class Precedence
    {
        int level;

        public int Level
        {
            get { return level; }
        }

        object assoc;

        public object Assoc
        {
            get { return assoc; }
        }

        public Precedence(int level, object assoc)
        {
            this.level = level;
            this.assoc = assoc;
        }

        public bool LeftAssoc()
        {
            return (assoc == Operators.Left);
        }

        public bool RightAssoc()
        {
            return (assoc == Operators.Right);
        }
    }

    // 操作符表
    class Operators : Dictionary<string, Precedence>
    {
        public static readonly object Left = new object();
        public static readonly object Right = new object();

        public void Add(string op, int level, object assoc)
        {
            Add(op, new Precedence(level, assoc));
        }
    }
}
