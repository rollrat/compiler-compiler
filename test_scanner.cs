using System;

namespace ParserGenerator
{
    public class HelloWorld
    {

        public static void Main(string[] args)
        {
            var sg = new ScannerGenerator();
            sg.PushRule("", "[\\r\\n ]");
            sg.PushRule("if", "if");
            sg.PushRule("for", "for");
            sg.PushRule("else", "else");
            sg.PushRule("id", "[a-z][a-z0-9]*");
            sg.PushRule("num", "[0-9]+");
            sg.Generate();
            sg.PrintDiagram();

            var scanner = sg.CreateScannerInstance();
            scanner.AllocateTarget("+/a1321    if else 0415abse+9999");
            while (scanner.Valid())
            {
                var ss = scanner.Next();
                Console.WriteLine($"{ss.Item1}, {ss.Item2}");
            }
        }
    }
}
