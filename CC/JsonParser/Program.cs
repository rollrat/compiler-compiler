/*

   Copyright (C) 2020. rollrat All Rights Reserved.

   Author: Jeong HyunJun

*/

using System;
using System.IO;
using System.Linq;
using System.Text;

namespace json_parser
{
    class Program
    {
        static JSonLexer lexer;
        static JSonParser parser;
        static ParsingTree tree;

        static JValue parse_json_file(string txt)
        {
            lexer.AllocateTarget(txt);
            Action<string, string, int, int> insert = (string x, string y, int a, int b) =>
            {
                parser.Insert(x, y);
                if (parser.Error()) throw new Exception($"[COMPILER] Parser error! L:{a}, C:{b}");
                while (parser.Reduce())
                {
                    var l = parser.LatestReduce();
                    l.Action(l);
                    parser.Insert(x, y);
                    if (parser.Error()) throw new Exception($"[COMPILER] Parser error! L:{a}, C:{b}");
                }
            };

            while (lexer.Valid())
            {
                var tk = lexer.Next();
                insert(tk.Item1, tk.Item2, tk.Item3, tk.Item4);
            }

            if (parser.Error()) throw new Exception();
            insert("$", "$", -1, -1);

            tree = parser.Tree;
            var root = tree.root.UserContents as JValue;

            var build = new StringBuilder();
            root.print(build, true);

            return root;
        }

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("./json-parser-generator <filename> <output> [-f|--format] [-t|--tree]");
                return;
            }

            lexer = new JSonLexer();
            parser = new JSonParser();

            var txt = File.ReadAllText(args[0]);
            var val = parse_json_file(txt);

            var build = new StringBuilder();

            if (args.Contains("-t") || args.Contains("--tree"))
            {
                tree.Print(build);
            }
            else
            {
                if (args.Contains("--format") || args.Contains("-f"))
                    val.print(build, true);
                else
                    val.print(build);
            }
            File.WriteAllText(args[1], build.ToString());

            return;
        }
    }
}
