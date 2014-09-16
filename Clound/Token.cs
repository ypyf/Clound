namespace Clound
{
    public class Token
    {
        //public static readonly Token ADD = new Token("+");
        //public static readonly Token SUB = new Token("-");
        //public static readonly Token MUL = new Token("*");
        //public static readonly Token DIV = new Token("/");
        //public static readonly Token MOD = new Token("%");
        //public static readonly Token EQUAL = new Token("=");
        //public static readonly Token EQUAL = new Token("!");
        //public static readonly Token EQUAL = new Token("<");
        //public static readonly Token EQUAL = new Token(">");
        //public static readonly Token DOT = new Token(".");
        //public static readonly Token COLON = new Token(":");
        //public static readonly Token COMMA = new Token(",");
        //public static readonly Token SEMI = new Token(";");
        //public static readonly Token INC = new Token("++");
        //public static readonly Token DEC = new Token("--");
        //public static readonly Token EQUAL = new Token("==");
        //public static readonly Token NOT_EQUAL = new Token("!=");
        //public static readonly Token LESS_EQUAL = new Token("<=");
        //public static readonly Token GREATER_EQUAL = new Token(">=");
        public static readonly Token EOL = new Token("\n");
        public static readonly Token EOF = new Token("");

        private string text;

        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        public Token(string text)
        {
            this.text = text;
        }

        public Token(char ch)
        {
            this.text = ch.ToString();
        }
    }

    class IntToken : Token
    {
        public IntToken(string text)
            : base(text)
        {
        }
    }

    class StrToken : Token
    {
        public StrToken(string text)
            : base(text)
        {
        }
    }

    class IdentToken : Token
    {
        public IdentToken(string text)
            : base(text)
        {
        }
    }
}
