/*

   Copyright (C) 2020. rollrat All Rights Reserved.

   Author: Jeong HyunJun

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace json_parser
{
    public class ParserAction
    {
        public Action<ParsingTree.ParsingTreeNode> SemanticAction;
        public static ParserAction Create(Action<ParsingTree.ParsingTreeNode> action)
            => new ParserAction { SemanticAction = action };
    }

    public class ParsingTree
    {
        public class ParsingTreeNode
        {
            public string Production;
            public string Contents;
            public object UserContents;
            public int ProductionRuleIndex;
            public ParsingTreeNode Parent;
            public List<ParsingTreeNode> Childs;
            public Action<ParsingTreeNode> Action;

            public static ParsingTreeNode NewNode()
                => new ParsingTreeNode { Parent = null, Childs = new List<ParsingTreeNode>() };
            public static ParsingTreeNode NewNode(string production)
                => new ParsingTreeNode { Parent = null, Childs = new List<ParsingTreeNode>(), Production = production };
            public static ParsingTreeNode NewNode(string production, string contents)
                => new ParsingTreeNode { Parent = null, Childs = new List<ParsingTreeNode>(), Production = production, Contents = contents };
        }

        public ParsingTreeNode root;

        public ParsingTree(ParsingTreeNode root)
        {
            this.root = root;
        }

        static void inner_print(StringBuilder builder, ParsingTreeNode node, string indent, bool last)
        {
            builder.Append(indent);
            if (last)
            {
                builder.Append("+-");
                indent += "  ";
            }
            else
            {
                builder.Append("|-");
                indent += "| ";
            }

            if (node.Childs.Count == 0)
            {
                builder.Append(node.Production + " " + node.Contents + "\r\n");
            }
            else
            {
                builder.Append(node.Production + "\r\n");
            }
            for (int i = 0; i < node.Childs.Count; i++)
                inner_print(builder, node.Childs[i], indent, i == node.Childs.Count - 1);
        }

        public void Print(StringBuilder builder)
        {
            inner_print(builder, root, "", true);
        }
    }

    public class JSonParser
    {
        static Dictionary<string, int> symbol_table = new Dictionary<string, int>()
        {
            {            "S'",   0 },
            {          "JSON",   1 },
            {         "ARRAY",   2 },
            {        "OBJECT",   3 },
            {       "MEMBERS",   4 },
            {          "PAIR",   5 },
            {      "ELEMENTS",   6 },
            {         "VALUE",   7 },
            { "object_starts",   8 },
            {   "object_ends",   9 },
            {         "comma",  10 },
            {        "v_pair",  11 },
            {  "array_starts",  12 },
            {    "array_ends",  13 },
            {        "v_true",  14 },
            {       "v_false",  15 },
            {        "v_null",  16 },
            {      "v_string",  17 },
            {      "v_number",  18 },
            {             "$",  19 },
        };

        static int[][] table = new int[][] {
            new int[] {   0,   1,   3,   2,   0,   0,   0,   0,   4,   0,   0,   0,   5,   0,   0,   0,   0,   0,   0,   0 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,  28 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,  -1 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,  -2 },
            new int[] {   0,   0,   0,   0,   7,   8,   0,   0,   0,   6,   0,   0,   0,   0,   0,   0,   0,   9,   0,   0 },
            new int[] {   0,   0,  16,  15,   0,   0,  11,  12,   4,   0,   0,   0,   5,  10,  17,  18,  19,  13,  14,   0 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0,  -5,  -5,   0,   0,  -5,   0,   0,   0,   0,   0,  -5 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0,  20,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0,  -7,  21,   0,   0,   0,   0,   0,   0,   0,   0,   0 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,  22,   0,   0,   0,   0,   0,   0,   0,   0 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0,  -3,  -3,   0,   0,  -3,   0,   0,   0,   0,   0,  -3 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,  23,   0,   0,   0,   0,   0,   0 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,  24,   0,   0, -10,   0,   0,   0,   0,   0,   0 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0, -12, -12,   0,   0, -12,   0,   0,   0,   0,   0,   0 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0, -13, -13,   0,   0, -13,   0,   0,   0,   0,   0,   0 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0, -14, -14,   0,   0, -14,   0,   0,   0,   0,   0,   0 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0, -15, -15,   0,   0, -15,   0,   0,   0,   0,   0,   0 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0, -16, -16,   0,   0, -16,   0,   0,   0,   0,   0,   0 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0, -17, -17,   0,   0, -17,   0,   0,   0,   0,   0,   0 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0, -18, -18,   0,   0, -18,   0,   0,   0,   0,   0,   0 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0,  -6,  -6,   0,   0,  -6,   0,   0,   0,   0,   0,  -6 },
            new int[] {   0,   0,   0,   0,  25,   8,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   9,   0,   0 },
            new int[] {   0,   0,  16,  15,   0,   0,   0,  26,   4,   0,   0,   0,   5,   0,  17,  18,  19,  13,  14,   0 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0,  -4,  -4,   0,   0,  -4,   0,   0,   0,   0,   0,  -4 },
            new int[] {   0,   0,  16,  15,   0,   0,  27,  12,   4,   0,   0,   0,   5,   0,  17,  18,  19,  13,  14,   0 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0,  -8,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0,  -9,  -9,   0,   0,   0,   0,   0,   0,   0,   0,   0 },
            new int[] {   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, -11,   0,   0,   0,   0,   0,   0 },
        };

        static int[] production = new int[] {
           1,   1,   1,   2,   3,   2,   3,   1,   3,   3,   1,   3,   1,   1,   1,   1,   1,   1,   1
        };

        static int[] group_table = new int[] {
           0,   1,   1,   2,   2,   3,   3,   4,   4,   5,   6,   6,   7,   7,   7,   7,   7,   7,   7
        };

        static int accept = 28;

        public JSonParser()
        {
            var ll = new List<ParserAction>();

            ll.Add(ParserAction.Create(x => x.UserContents = x.Childs[0].UserContents));
            ll.Add(ParserAction.Create(x => x.UserContents = x.Childs[0].UserContents));
            ll.Add(ParserAction.Create(x => x.UserContents = new JArray()));
            ll.Add(ParserAction.Create(x => x.UserContents = x.Childs[1].UserContents));
            ll.Add(ParserAction.Create(x => x.UserContents = new JObject()));
            ll.Add(ParserAction.Create(x => x.UserContents = x.Childs[1].UserContents));
            ll.Add(ParserAction.Create(x =>
            {
                var jo = new JObject();
                jo.keyvalue.Add(new KeyValuePair<string, JValue>(x.Childs[0].Childs[0].Contents, x.Childs[0].Childs[2].UserContents as JValue));
                x.UserContents = jo;
            }));
            ll.Add(ParserAction.Create(x =>
            {
                var jo = x.Childs[2].UserContents as JObject;
                jo.keyvalue.Insert(0, new KeyValuePair<string, JValue>(x.Childs[0].Childs[0].Contents, x.Childs[0].Childs[2].UserContents as JValue));
                x.UserContents = jo;
            }));
            ll.Add(ParserAction.Create(x => { }));
            ll.Add(ParserAction.Create(x =>
            {
                var ja = new JArray();
                ja.array.Add(x.Childs[0].UserContents as JValue);
                x.UserContents = ja;
            }));
            ll.Add(ParserAction.Create(x =>
            {
                var ja = x.Childs[2].UserContents as JArray;
                ja.array.Insert(0, x.Childs[0].UserContents as JValue);
                x.UserContents = ja;
            }));
            ll.Add(ParserAction.Create(x => x.UserContents = new JString { str = x.Contents }));
            ll.Add(ParserAction.Create(x => x.UserContents = new JNumeric { numstr = x.Contents }));
            ll.Add(ParserAction.Create(x => x.UserContents = x.Childs[0].UserContents));
            ll.Add(ParserAction.Create(x => x.UserContents = x.Childs[0].UserContents));
            ll.Add(ParserAction.Create(x => x.UserContents = new JState { type = JToken.v_true }));
            ll.Add(ParserAction.Create(x => x.UserContents = new JState { type = JToken.v_false }));
            ll.Add(ParserAction.Create(x => x.UserContents = new JState { type = JToken.v_null }));

            actions = ll;

            var l = symbol_table.ToList().Select(x => new Tuple<int, string>(x.Value, x.Key)).ToList();
            l.Sort();
            l.ForEach(x => symbol_index_name.Add(x.Item2));

        }

        List<string> symbol_index_name = new List<string>();
        Stack<int> state_stack = new Stack<int>();
        Stack<ParsingTree.ParsingTreeNode> treenode_stack = new Stack<ParsingTree.ParsingTreeNode>();
        List<ParserAction> actions;

        bool latest_error;
        bool latest_reduce;
        public bool Accept() => state_stack.Count == 0;
        public bool Error() => latest_error;
        public bool Reduce() => latest_reduce;

        public void Clear()
        {
            latest_error = latest_reduce = false;
            state_stack.Clear();
            treenode_stack.Clear();
        }

        public ParsingTree Tree => new ParsingTree(treenode_stack.Peek());

        public string Stack() => string.Join(" ", new Stack<int>(state_stack));

        public void Insert(string token_name, string contents) => Insert(symbol_table[token_name], contents);
        public void Insert(int index, string contents)
        {
            if (state_stack.Count == 0)
            {
                state_stack.Push(0);
                latest_error = false;
            }
            latest_reduce = false;

            int code = table[state_stack.Peek()][index];

            if (code == accept)
            {
                // Nothing
            }
            else if (code > 0)
            {
                // Shift
                state_stack.Push(table[state_stack.Peek()][index]);
                treenode_stack.Push(ParsingTree.ParsingTreeNode.NewNode(symbol_index_name[index], contents));
            }
            else if (code < 0)
            {
                // Reduce
                reduce(index);
                latest_reduce = true;
            }
            else
            {
                // Panic mode
                state_stack.Clear();
                treenode_stack.Clear();
                latest_error = true;
            }
        }

        public ParsingTree.ParsingTreeNode LatestReduce() => treenode_stack.Peek();
        private void reduce(int index)
        {
            var reduce_production = -table[state_stack.Peek()][index];
            var reduce_treenodes = new List<ParsingTree.ParsingTreeNode>();

            // Reduce Stack
            for (int i = 0; i < production[reduce_production]; i++)
            {
                state_stack.Pop();
                reduce_treenodes.Insert(0, treenode_stack.Pop());
            }

            state_stack.Push(table[state_stack.Peek()][group_table[reduce_production]]);

            var reduction_parent = ParsingTree.ParsingTreeNode.NewNode(symbol_index_name[group_table[reduce_production]]);
            reduction_parent.ProductionRuleIndex = reduce_production - 1;
            reduce_treenodes.ForEach(x => x.Parent = reduction_parent);
            reduction_parent.Contents = string.Join("", reduce_treenodes.Select(x => x.Contents));
            reduction_parent.Childs = reduce_treenodes;
            treenode_stack.Push(reduction_parent);
            if (actions.Count != 0)
                reduction_parent.Action = actions[reduction_parent.ProductionRuleIndex].SemanticAction;
        }
    }
}
