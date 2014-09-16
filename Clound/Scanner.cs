using Collections = System.Collections.Generic;
using IO = System.IO;
using Text = System.Text;

namespace Clound
{
    public sealed class Scanner
    {
        private IO.BinaryReader inputStream;

        public Scanner(IO.BinaryReader inputStream)
        {
            this.inputStream = inputStream;
        }

        public Token Scan()
        {
            while (inputStream.PeekChar() != -1)
            {
                char ch = (char)inputStream.PeekChar();

                // Scan individual tokens
                if (IsWhiteSpace(ch))
                {
                    // eat the current char and skip ahead!
                    inputStream.Read();
                }
                else if ('/' == ch &&
                        '/' == inputStream.PeekChar())
                {
                    do
                    {
                        inputStream.Read();
                    } while (inputStream.PeekChar() != '\r' &&
                           inputStream.PeekChar() != '\n' &&
                           inputStream.PeekChar() != -1);
                }
                else if (char.IsLetter(ch) || ch == '_')
                {
                    // keyword or identifier

                    Text.StringBuilder accum = new Text.StringBuilder();

                    while (char.IsLetter(ch) || ch == '_')
                    {
                        accum.Append(ch);
                        inputStream.Read();

                        if (inputStream.PeekChar() == -1)
                        {
                            break;
                        }
                        else
                        {
                            ch = (char)inputStream.PeekChar();
                        }
                    }

                    return new IdentToken(accum.ToString());
                }
                else if (ch == '"') // string literal
                {
                    Text.StringBuilder accum = new Text.StringBuilder();

                    inputStream.Read(); // skip the '"'

                    if (inputStream.PeekChar() == -1)
                    {
                        throw new System.Exception("unterminated string literal");
                    }

                    while ((ch = (char)inputStream.PeekChar()) != '"')
                    {
                        accum.Append(ch);
                        inputStream.Read();

                        if (inputStream.PeekChar() == -1)
                        {
                            throw new System.Exception("unterminated string literal");
                        }
                    }

                    // skip the terminating "
                    inputStream.Read();

                    return new StrToken(accum.ToString());
                }
                else if (char.IsDigit(ch))
                {
                    // numeric literal

                    Text.StringBuilder accum = new Text.StringBuilder();

                    while (char.IsDigit(ch))
                    {
                        accum.Append(ch);
                        inputStream.Read();

                        if (inputStream.PeekChar() == -1)
                        {
                            break;
                        }
                        else
                        {
                            ch = (char)inputStream.PeekChar();
                        }
                    }
                    return new IntToken(accum.ToString());
                }
                // 运算符
                else if (ch == '<')
                {
                    inputStream.Read();
                    if (inputStream.PeekChar() == '=')
                    {
                        inputStream.Read();
                        return new Token("<=");
                    }

                    return new Token(ch);
                }
                else if (ch == '>')
                {
                    inputStream.Read();
                    if (inputStream.PeekChar() == '=')
                    {
                        inputStream.Read();
                        return new Token(">=");
                    }
                    return new Token(ch);
                }
                else if (ch == '=')
                {
                    inputStream.Read();
                    if (inputStream.PeekChar() == '=')
                    {
                        inputStream.Read();
                        return new Token("==");
                    }
                    return new Token(ch);
                }
                else if (ch == '!')
                {
                    inputStream.Read();
                    if (inputStream.PeekChar() == '=')
                    {
                        inputStream.Read();
                        return new Token("!=");
                    }
                    return new Token(ch);
                }
                else if (ch == '+')
                {
                    inputStream.Read();
                    if (inputStream.PeekChar() == '+')
                    {
                        inputStream.Read();
                        return new Token("++");
                    }
                    return new Token(ch);
                }
                else if (ch == '-')
                {
                    inputStream.Read();
                    if (inputStream.PeekChar() == '-')
                    {
                        inputStream.Read();
                        return new Token("--");
                    }
                    return new Token(ch);
                }
                else if (ch == '\r')
                {
                    // 继续扫描下一个Token
                    inputStream.Read();
                }
                else if (ch == '\n')
                {
                    inputStream.Read();
                    return Token.EOL;
                }
                else
                {
                    inputStream.Read();
                    // 其他字符直接作为OpToken返回
                    return new Token(ch);
                }
            }
            return Token.EOF;
        }

        private static bool IsWhiteSpace(char chr)
        {
            return (chr == ' ' || chr == '\t' || chr == '\f');
        }

        //public Token Peek(int n)
        //{
        //    return null;
        //}
    }
}
