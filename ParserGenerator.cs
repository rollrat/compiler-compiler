/*

   Copyright (C) 2019. rollrat All Rights Reserved.

   Author: Jeong HyunJun

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParserGenerator
{
    public class ParserProduction
    {
        public int index;
        public string production_name;
        public bool isterminal;
        public List<ParserProduction> contents = new List<ParserProduction>();
        public List<List<ParserProduction>> sub_productions = new List<List<ParserProduction>>();

        public static ParserProduction operator+(ParserProduction p1, ParserProduction p2)
        {
            p1.contents.Add(p2);
            return p1;
        }

        public static ParserProduction operator|(ParserProduction p1, ParserProduction p2)
        {
            p2.contents.Insert(0, p2);
            p1.sub_productions.Add(new List<ParserProduction> (p2.contents));
            p2.contents.Clear();
            return p1;
        }

#if false
        public static ParserProduction operator +(ParserProduction p1, string p2)
        {
            p1.contents.Add(new ParserProduction { isterminal = true, token_specific = p2 });
            return p1;
        }

        public static ParserProduction operator|(ParserProduction p1, string p2)
        {
            p1.sub_productions.Add(new List<ParserProduction> { p1, new ParserProduction { isterminal = true, token_specific = p2 } });
            return p1;
        }
#endif
    }
    
    /// <summary>
    /// LR Parser Generator
    /// </summary>
    public class ParserGenerator
    {
        List<ParserProduction> production_rules;

        public readonly static ParserProduction EmptyString = new ParserProduction { index = -2 };

        public ParserGenerator()
        {
            production_rules = new List<ParserProduction>();
            production_rules.Add(new ParserProduction { index = 0 });
        }
        
        public ParserProduction CreateNewProduction(string name = "", bool is_terminal = true)
        {
            var pp = new ParserProduction { index = production_rules.Count, production_name = name, isterminal = is_terminal };
            production_rules.Add(pp);
            return pp;
        }

        public void PushStarts(ParserProduction pp)
        {
            // Augment stats node
            production_rules[0].sub_productions.Add(new List<ParserProduction> { pp });
        }

        private string t2s(Tuple<int, int, int> t)
        {
            return $"{t.Item1},{t.Item2},{t.Item3}";
        }

        private string l2s(List<Tuple<int,int,int>> h)
        {
            var list = h.ToList();
            list.Sort();
            return string.Join(",", list.Select(x => $"({x.Item1},{x.Item2},{x.Item3})"));
        }

        private void print_hs(List<HashSet<int>> lhs, string prefix)
        {
            for (int i = 0; i < lhs.Count; i++)
                if (lhs[i].Count > 0)
                    Console.WriteLine(
                        $"{prefix}({production_rules[i].production_name})={{{string.Join(",", lhs[i].ToList().Select(x => x == -1 ? "$" : production_rules[x].production_name))}}}");
        }

        int number_of_states = -1;
        Dictionary<int, List<Tuple<int, int>>> shift_info;
        Dictionary<int, List<Tuple<int, int, int>>> reduce_info;

        /// <summary>
        /// Generate SimpleLR Table
        /// </summary>
        public void Generate()
        {
            // --------------- Collect FIRST,FOLLOW Set ---------------
            var FIRST = new List<HashSet<int>>();
            foreach (var rule in production_rules)
                FIRST.Add(first_terminals(rule.index));

            var FOLLOW = new List<HashSet<int>>();
            for (int i = 0; i < production_rules.Count; i++)
                FOLLOW.Add(new HashSet<int>());
            FOLLOW[0].Add(-1); // -1: Sentinel

            // 1. B -> a A b, Add FIRST(b) to FOLLOW(A)
            for (int i = 0; i < production_rules.Count; i++)
                if (!production_rules[i].isterminal)
                    foreach (var rule in production_rules[i].sub_productions)
                        for (int j = 0; j < rule.Count - 1; j++)
                            if (rule[j].isterminal == false || rule[j + 1].isterminal)
                                foreach (var r in FIRST[rule[j+1].index])
                                    FOLLOW[rule[j].index].Add(r);

            // 2. B -> a A b and empty -> FIRST(b), Add FOLLOW(B) to FOLLOW(A)
            for (int i = 0; i < production_rules.Count; i++)
                if (!production_rules[i].isterminal)
                    foreach (var rule in production_rules[i].sub_productions)
                        if (rule.Count > 2 && rule[rule.Count - 2].isterminal == false && FIRST[rule.Last().index].Contains(EmptyString.index))
                            foreach (var r in FOLLOW[i])
                                FOLLOW[rule[rule.Count - 2].index].Add(r);

            // 3. B -> a A, Add FOLLOW(B) to FOLLOW(A)
            for (int i = 0; i < production_rules.Count; i++)
                if (!production_rules[i].isterminal)
                    foreach (var rule in production_rules[i].sub_productions)
                        if (rule.Last().isterminal == false)
                            foreach (var r in FOLLOW[i])
                                FOLLOW[rule.Last().index].Add(r);

#if false
            print_hs(FIRST, "FIRST");
            print_hs(FOLLOW, "FOLLOW");
#endif
            // --------------------------------------------------------

            // (state_index, (production_rule_index, sub_productions_pos, dot_position)
            var states = new Dictionary<int, List<Tuple<int, int, int>>>();
            // (state_specify, state_index)
            var state_index = new Dictionary<string, int>();
            // (state_index, (reduce_what, state_index))
            shift_info = new Dictionary<int, List<Tuple<int, int>>>();
            // (state_index, (shift_what, production_rule_index, sub_productions_pos))
            reduce_info = new Dictionary<int, List<Tuple<int, int, int>>>();
            var index_count = 0;

            // -------------------- Put first eater -------------------
            var first_l = first_nonterminals(0);
            state_index.Add(l2s(first_l), 0);
            states.Add(0, first_l);
            // --------------------------------------------------------

            // Create all LR states
            // (states)
            var q = new Queue<int>();
            q.Enqueue(index_count++);
            while (q.Count != 0)
            {
                var p = q.Dequeue();

                // Collect goto
                // (state_index, (production_rule_index, sub_productions_pos, dot_position))
                var gotos = new Dictionary<int, List<Tuple<int, int, int>>>();
                foreach (var transition in states[p])
                    if (production_rules[transition.Item1].sub_productions[transition.Item2].Count > transition.Item3)
                    {
                        var pi = production_rules[transition.Item1].sub_productions[transition.Item2][transition.Item3].index;
                        if (!gotos.ContainsKey(pi))
                            gotos.Add(pi, new List<Tuple<int, int, int>>());
                        gotos[pi].Add(new Tuple<int, int, int>(transition.Item1, transition.Item2, transition.Item3 + 1));
                    }

                // Populate empty-string closure
                foreach (var goto_unit in gotos)
                {
                    var set = new HashSet<string>();
                    // Push exists transitions
                    foreach (var psd in goto_unit.Value)
                        set.Add(t2s(psd));
                    // Find all transitions
                    var new_trans = new List<Tuple<int, int, int>>();
                    foreach (var psd in goto_unit.Value)
                    {
                        if (production_rules[psd.Item1].sub_productions[psd.Item2].Count == psd.Item3) continue;
                        if (production_rules[psd.Item1].sub_productions[psd.Item2][psd.Item3].isterminal) continue;
                        var first_nt = first_nonterminals(production_rules[psd.Item1].sub_productions[psd.Item2][psd.Item3].index);
                        foreach (var nts in first_nt)
                            if (!set.Contains(t2s(nts)))
                            {
                                new_trans.Add(nts);
                                set.Add(t2s(nts));
                            }
                    }
                    goto_unit.Value.AddRange(new_trans);
                }
                
                // Build shift transitions ignore terminal, non-terminal
                foreach (var pp in gotos)
                {
                    var hash = l2s(pp.Value);
                    if (!state_index.ContainsKey(hash))
                    {
                        states.Add(index_count, pp.Value);
                        state_index.Add(hash, index_count);
                        q.Enqueue(index_count++);
                    }
                    var index = state_index[hash];

                    if (!shift_info.ContainsKey(p))
                        shift_info.Add(p, new List<Tuple<int, int>>());
                    shift_info[p].Add(new Tuple<int, int>(pp.Key, index));
                }

                // Check require reduce and build reduce transitions
                foreach (var transition in states[p])
                    if (production_rules[transition.Item1].sub_productions[transition.Item2].Count == transition.Item3)
                    {
                        if (!reduce_info.ContainsKey(p))
                            reduce_info.Add(p, new List<Tuple<int, int, int>>());
                        foreach (var term in FOLLOW[transition.Item1])
                            reduce_info[p].Add(new Tuple<int, int, int>(term, transition.Item1, transition.Item2));
                    }
            }

#if false
            // Print result
            for (int i = 0; i < index_count; i++)
            {
                var builder = new StringBuilder();
                var x = $"I{i} => ";
                builder.Append(x);
                if (shift_info.ContainsKey(i))
                {
                    builder.Append("SHIFT{" + string.Join(",", shift_info[i].Select(y => $"({production_rules[y.Item1].production_name},I{y.Item2})")) + "}");
                    if (reduce_info.ContainsKey(i))
                       builder.Append("\r\n" + "".PadLeft(x.Length) + "REDUCE{" + string.Join(",", reduce_info[i].Select(y => $"({(y.Item1 == -1 ? "$" : production_rules[y.Item1].production_name)},{(y.Item2 == 0 ? "accept" : production_rules[y.Item2].production_name)},{y.Item3})")) + "}");
                }
                else if (reduce_info.ContainsKey(i))
                    builder.Append("REDUCE{" + string.Join(",", reduce_info[i].Select(y => $"({(y.Item1 == -1 ? "$" : production_rules[y.Item1].production_name)},{(y.Item2 == 0 ? "accept" : production_rules[y.Item2].production_name)},{y.Item3})")) + "}");
                Console.WriteLine(builder.ToString());
            }
#endif

            number_of_states = states.Count;
        }

        /// <summary>
        /// Calculate FIRST only Terminals
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private HashSet<int> first_terminals(int index)
        {
            var result = new HashSet<int>();
            var q = new Queue<int>();
            var visit = new List<bool>();
            visit.AddRange(Enumerable.Repeat(false, production_rules.Count));
            q.Enqueue(index);

            while (q.Count != 0)
            {
                var p = q.Dequeue();
                if (visit[p]) continue;
                visit[p] = true;

                if (p < 0 || production_rules[p].isterminal)
                    result.Add(p);
                else
                    production_rules[p].sub_productions.ForEach(x => q.Enqueue(x[0].index));
            }

            return result;
        }

        /// <summary>
        /// Calculate FIRST only Non-Terminals
        /// </summary>
        /// <param name="production_rule_index"></param>
        /// <returns></returns>
        private List<Tuple<int, int, int>> first_nonterminals(int production_rule_index)
        {
            // (production_rule_index, sub_productions_pos, dot_position)
            var first_l = new List<Tuple<int, int, int>>();
            // (production_rule_index, sub_productions_pos)
            var first_q = new Queue<Tuple<int, int>>();
            // (production_rule_index, (sub_productions_pos))
            var first_visit = new Dictionary<int, HashSet<int>>();
            first_q.Enqueue(new Tuple<int, int>(production_rule_index, 0));
            for (int j = 0; j < production_rules[production_rule_index].sub_productions.Count; j++)
                first_q.Enqueue(new Tuple<int, int>(production_rule_index, j));
            // Get all of starts node FIRST non-terminals
            while (first_q.Count != 0)
            {
                var t = first_q.Dequeue();
                if (first_visit.ContainsKey(t.Item1) && first_visit[t.Item1].Contains(t.Item2)) continue;
                if (!first_visit.ContainsKey(t.Item1)) first_visit.Add(t.Item1, new HashSet<int>());
                first_visit[t.Item1].Add(t.Item2);
                first_l.Add(new Tuple<int, int, int>(t.Item1, t.Item2, 0));
                for (int i = 0; i < production_rules[t.Item1].sub_productions.Count; i++)
                {
                    var sub_pr = production_rules[t.Item1].sub_productions[i];
                    if (sub_pr[0].isterminal == false)
                        for (int j = 0; j < production_rules[sub_pr[0].index].sub_productions.Count; j++)
                            first_q.Enqueue(new Tuple<int, int>(sub_pr[0].index, j));
                }
            }
            return first_l;
        }

        public ShiftReduceParser CreateShiftReduceParserInstance()
        {
            var symbol_table = new Dictionary<string, int>();
            var jump_table = new int[number_of_states][];
            var goto_table = new int[number_of_states][];
            var grammar = new List<List<int>>();
            var grammar_group = new List<int>();
            var production_mapping = new List<List<int>>();
            var pm_count = 0;
            
            foreach (var pr in production_rules)
            {
                var ll = new List<List<int>>();
                var pm = new List<int>();
                foreach (var sub_pr in pr.sub_productions)
                {
                    ll.Add(sub_pr.Select(x => x.index).ToList());
                    pm.Add(pm_count++);
                    grammar_group.Add(production_mapping.Count);
                }
                grammar.AddRange(ll);
                production_mapping.Add(pm);
            }

            for (int i = 0; i < number_of_states; i++)
            {
                // Last elements is sentinel
                jump_table[i] = new int[production_rules.Count + 1];
                goto_table[i] = new int[production_rules.Count + 1];
            }

            foreach (var pr in production_rules)
                symbol_table.Add(pr.production_name ?? "^", pr.index);
            symbol_table.Add("$", production_rules.Count);

            foreach (var shift in shift_info)
                foreach (var elem in shift.Value)
                {
                    jump_table[shift.Key][elem.Item1] = 1;
                    goto_table[shift.Key][elem.Item1] = elem.Item2;
                }

            foreach (var reduce in reduce_info)
                foreach (var elem in reduce.Value)
                {
                    var index = elem.Item1;
                    if (index == -1) index = production_rules.Count;
                    if (elem.Item2 == 0)
                        jump_table[reduce.Key][index] = 3;
                    else
                    {
                        jump_table[reduce.Key][index] = 2;
                        goto_table[reduce.Key][index] = production_mapping[elem.Item2][elem.Item3];
                    }
                }

            return new ShiftReduceParser(symbol_table, jump_table, goto_table, grammar_group.ToArray(), grammar.Select(x => x.ToArray()).ToArray());
        }
    }

    /// <summary>
    /// Shift-Reduce Parser for LR(1)
    /// </summary>
    public class ShiftReduceParser
    {
        Dictionary<string, int> symbol_name_index = new Dictionary<string, int>();
        List<string> symbol_index_name = new List<string>();
        Stack<int> state_stack = new Stack<int>();
        Stack<int> production_stack = new Stack<int>();
        Stack<string> contents_stack = new Stack<string>();

        // 3       1      2       0
        // Accept? Shift? Reduce? Error?
        int[][] jump_table;
        int[][] goto_table;
        int[][] production;
        int[] group_table;

        public ShiftReduceParser(Dictionary<string, int> symbol_table, int[][] jump_table, int[][] goto_table, int[] group_table, int[][] production)
        {
            symbol_name_index = symbol_table;
            this.jump_table = jump_table;
            this.goto_table = goto_table;
            this.production = production;
            this.group_table = group_table;
            var l = symbol_table.ToList().Select(x => new Tuple<int, string>(x.Value, x.Key)).ToList();
            l.Sort();
            l.ForEach(x => symbol_index_name.Add(x.Item2));
        }
        
        bool latest_error;
        bool latest_reduce;
        public bool Accept() => state_stack.Count == 0;
        public bool Error() => latest_error;
        public bool Reduce() => latest_reduce;
        
        public void Insert(string token_name, string contents) => Insert(symbol_name_index[token_name], contents);
        public void Insert(int index, string contents)
        {
            if (state_stack.Count == 0)
            {
                state_stack.Push(0);
                latest_error = false;
            }
            latest_reduce = false;

            switch (jump_table[state_stack.Peek()][index])
            {
                case 0:
                    // Panic mode
                    state_stack.Clear();
                    production_stack.Clear();
                    contents_stack.Clear();
                    latest_error = true;
                    break;

                case 1:
                    // Shift
                    state_stack.Push(goto_table[state_stack.Peek()][index]);
                    production_stack.Push(index);
                    contents_stack.Push(contents);
                    break;

                case 2:
                    // Reduce
                    reduce(index);
                    latest_reduce = true;
                    break;

                case 3:
                    // Nothing
                    break;
            }
        }

        List<Tuple<int,string>> reduce_nodes = new List<Tuple<int, string>>();
        public List<Tuple<string,string>> LastestReduce() => reduce_nodes.Select(x => new Tuple<string,string>( symbol_index_name[x.Item1], x.Item2)).ToList();
        private void reduce(int index)
        {
            var reduce_production = goto_table[state_stack.Peek()][index];

            // Reduce Stack
            reduce_nodes.Clear();
            for (int i = 0; i < production[reduce_production].Length; i++)
            {
                state_stack.Pop();
                reduce_nodes.Insert(0, new Tuple<int, string> (production_stack.Pop(), contents_stack.Pop()));
            }
            reduce_nodes.Insert(0, new Tuple<int, string>(group_table[reduce_production], ""));

            state_stack.Push(goto_table[state_stack.Peek()][group_table[reduce_production]]);
            production_stack.Push(group_table[reduce_production]);
            contents_stack.Push(string.Join("", reduce_nodes.Select(x => x.Item2)));
        }
    }
}
