using System;
using System.Linq;
namespace ParserGenerator {
    public class HelloWorld
    {

        public static void Main(string[] args)
        {
            var gen = new ParserGenerator();

            // Non-Terminals
            var exp = gen.CreateNewProduction("exp", false);
            var term = gen.CreateNewProduction("term", false);
            var factor = gen.CreateNewProduction("factor", false);
            var func = gen.CreateNewProduction("func", false);
            var arguments = gen.CreateNewProduction("args", false);
            var args_left = gen.CreateNewProduction("args_left", false);

            // Terminals
            var plus = gen.CreateNewProduction("plus");         // +
            var minus = gen.CreateNewProduction("minus");       // -
            var multiple = gen.CreateNewProduction("multiple"); // *
            var divide = gen.CreateNewProduction("divide");     // /
            var id = gen.CreateNewProduction("id");             // [_$a-zA-Z][_$a-zA-Z0-9]*
            var op_open = gen.CreateNewProduction("op_open");   // (
            var op_close = gen.CreateNewProduction("op_close"); // )
            var num = gen.CreateNewProduction("num");           // [0-9]+
            var split = gen.CreateNewProduction("split");       // ,

            exp |= exp + plus + term;
            exp |= exp + minus + term;
            exp |= term;
            term |= term + multiple + factor;
            term |= term + divide + factor;
            term |= factor;
            factor |= op_open + exp + op_close;
            factor |= num;
            factor |= id;
            factor |= func;
            func |= id + op_open + arguments + op_close;
            arguments |= args_left + id ;
            arguments |= ParserGenerator.EmptyString;
            args_left |= args_left + id + split;
            args_left |= ParserGenerator.EmptyString;

            gen.PushStarts(exp);
            gen.Generate();
            gen.PrintStates();
            var slr = gen.CreateShiftReduceParserInstance();

            // 2*4+5$

            Action<string, string> insert = (string x, string y) =>
            {
                slr.Insert(x, y);
                while (slr.Reduce())
                {
                    var l = slr.LatestReduce();
                    Console.Write(l.Produnction.PadLeft(8) + " => ");
                    Console.WriteLine(string.Join(" ", l.Childs.Select(z => z.Produnction)));
                    Console.Write(l.Produnction.PadLeft(8) + " => ");
                    Console.WriteLine(string.Join(" ", l.Childs.Select(z => z.Contents)));
                    slr.Insert(x, y);
                }
            };

            var sg2 = new ScannerGenerator();
            sg2.PushRule("", "[\\r\\n ]");
            sg2.PushRule("plus", "\\+");
            sg2.PushRule("minus", "-");
            sg2.PushRule("multiple", "\\*");
            sg2.PushRule("divide", "\\/");
            sg2.PushRule("op_open", "\\(");
            sg2.PushRule("op_close", "\\)");
            sg2.PushRule("split", ",");
            sg2.PushRule("id", "[a-z][a-z0-9]*");
            sg2.PushRule("num", "[0-9]+");
            sg2.Generate();
            sg2.CreateScannerInstance();

            var scanner2 = sg2.CreateScannerInstance();
            scanner2.AllocateTarget("2+6*(6+4*7-2)+sin(a,b)+cos()*pi");

            while (scanner2.Valid())
            {
                var ss = scanner2.Next();
                insert(ss.Item1,ss.Item2);
            }
            insert("$", "$");
        }
    }}
