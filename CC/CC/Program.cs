/*

   Copyright (C) 2020. rollrat All Rights Reserved.

   Author: Jeong HyunJun

*/

using System;
using System.Collections.Generic;
using System.Text;

namespace CC
{
    class Program
    {
        static void Main(string[] args)
        {
            var sg = new ScannerGenerator();

            sg.PushRule("", @"[\t\r\n ]");
            sg.PushRule("comma", "\\,");
            sg.PushRule("v_pair", "\\:");
            sg.PushRule("object_starts", "\\{");
            sg.PushRule("object_ends", "\\}");
            sg.PushRule("array_starts", "\\[");
            sg.PushRule("array_ends", "\\]");
            sg.PushRule("v_true", "true");
            sg.PushRule("v_false", "false");
            sg.PushRule("v_null", "null");

            var vv = new List<StringBuilder>(5);
            vv.Add(new StringBuilder()); // 6
            vv.Add(new StringBuilder()); // 5
            vv.Add(new StringBuilder()); // 4
            vv.Add(new StringBuilder()); // 3
            vv.Add(new StringBuilder()); // 2

            for (int cc = 0xc0; cc < 0xff; cc++)
            {
                if ((cc & 0xfc) == 0xfc)
                {
                    vv[0].Append("\\x" + cc.ToString("X2"));
                }
                else if ((cc & 0xf8) == 0xf8)
                {
                    vv[1].Append("\\x" + cc.ToString("X2"));
                }
                else if ((cc & 0xf0) == 0xf0)
                {
                    vv[2].Append("\\x" + cc.ToString("X2"));
                }
                else if ((cc & 0xe0) == 0xe0)
                {
                    vv[3].Append("\\x" + cc.ToString("X2"));
                }
                else if ((cc & 0xc0) == 0xc0)
                {
                    vv[4].Append("\\x" + cc.ToString("X2"));
                }
            }

            var bb = new StringBuilder();
            for (int i = 0, j = 5; i < 5; i++, j--)
            {
                bb.Append("[" + vv[i].ToString() + "]" + new string('.', j));

                if (i != 4)
                    bb.Append("|");
            }

            //SimpleRegex sr = new SimpleRegex();
            //sr.MakeNFA($"\"({bb.ToString()}|[^\\\"]|\\\")*\"");
            //sr.OptimizeNFA();
            //sr.NFAtoDFA();
            //sr.MinimizeDFA();
            //Console.Instance.WriteLine(sr.PrintDiagram());

            //sg.PushRule("v_string", $"\"({bb.ToString()}|[^\\\"]|\\\")*\"");
            sg.PushRule("v_string", $"\"([^\\\"]|\\\")*\"");
            sg.PushRule("v_number", @"-?[0-9]+(\.[0-9]+)?([Ee][\+\-]?[0-9]+)?");

            sg.Generate();
            Console.WriteLine(sg.PrintDiagram());

            var s3 = sg.CreateScannerInstance();
            Console.WriteLine(s3.ToCSCode("json_lexer"));

            var gen = new ParserGenerator();

            var JSON = gen.CreateNewProduction("JSON", false);
            var ARRAY = gen.CreateNewProduction("ARRAY", false);
            var OBJECT = gen.CreateNewProduction("OBJECT", false);
            var MEMBERS = gen.CreateNewProduction("MEMBERS", false);
            var PAIR = gen.CreateNewProduction("PAIR", false);
            var ELEMENTS = gen.CreateNewProduction("ELEMENTS", false);
            var VALUE = gen.CreateNewProduction("VALUE", false);

            var object_starts = gen.CreateNewProduction("object_starts");
            var object_ends = gen.CreateNewProduction("object_ends");
            var comma = gen.CreateNewProduction("comma");
            var v_pair = gen.CreateNewProduction("v_pair");
            var array_starts = gen.CreateNewProduction("array_starts");
            var array_ends = gen.CreateNewProduction("array_ends");
            var v_true = gen.CreateNewProduction("v_true");
            var v_false = gen.CreateNewProduction("v_false");
            var v_null = gen.CreateNewProduction("v_null");
            var v_string = gen.CreateNewProduction("v_string");
            var v_number = gen.CreateNewProduction("v_number");

            JSON |= OBJECT;
            JSON |= ARRAY;
            OBJECT |= object_starts + object_ends;
            OBJECT |= object_starts + MEMBERS + object_ends;
            MEMBERS |= PAIR;
            MEMBERS |= PAIR + comma + MEMBERS;
            PAIR |= v_string + v_pair + VALUE;
            ARRAY |= array_starts + array_ends;
            ARRAY |= array_starts + ELEMENTS + array_ends;
            ELEMENTS |= VALUE;
            ELEMENTS |= VALUE + comma + ELEMENTS;
            VALUE |= v_string;
            VALUE |= v_number;
            VALUE |= OBJECT;
            VALUE |= ARRAY;
            VALUE |= v_true;
            VALUE |= v_false;
            VALUE |= v_null;

            gen.PushStarts(JSON);
            gen.PrintProductionRules();
            gen.GenerateLALR2();
            gen.PrintStates();
            gen.PrintTable();

            Console.WriteLine(gen.GlobalPrinter.ToString());
            Console.WriteLine(gen.CreateExtendedShiftReduceParserInstance().ToCSCode("json_parser"));
        }
    }
}
