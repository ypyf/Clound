using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Clound
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("{0}: no input files", System.AppDomain.CurrentDomain.FriendlyName);
                return;
            }

            try
            {
                string sourceFileName = args[0];
                Console.WriteLine("开始编译 {0}。", sourceFileName);
                Scanner scanner = null;
                FileInfo fi = new FileInfo(sourceFileName);
                BinaryReader reader = new BinaryReader(fi.OpenRead(), Encoding.Default);
                scanner = new Scanner(reader);
                Parser parser = new Parser(scanner);
                string module = Path.GetFileNameWithoutExtension(sourceFileName);
                CodeGen codeGen = new CodeGen(parser.Result, module);
                Console.WriteLine("编译成功。");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(e.StackTrace);
            }
        }
    }
}
