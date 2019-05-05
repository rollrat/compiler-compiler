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
            // Augement stats node
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
            
            print_hs(FIRST, "FIRST");
            print_hs(FOLLOW, "FOLLOW");
            // --------------------------------------------------------

            // (state_index, (production_rule_index, sub_productions_pos, dot_position)
            var states = new Dictionary<int, List<Tuple<int, int, int>>>();
            // (state_specify, state_index)
            var state_index = new Dictionary<string, int>();
            // (state_index, (reduce_what, state_index))
            var shift_info = new Dictionary<int, List<Tuple<int, int>>>();
            // (state_index, (shift_what, production_rule_index, sub_productions_pos))
            var reduce_info = new Dictionary<int, List<Tuple<int, int, int>>>();
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
    }

    /// <summary>
    /// Shift-Reduce Parser for LR(1)
    /// </summary>
    public class ShiftReduceParser
    {
        public void Test()
        {
            var gen = new ParserGenerator();

            // Non-Terminals
            var exp = gen.CreateNewProduction("exp", false);
            var term = gen.CreateNewProduction("term", false);
            var factor = gen.CreateNewProduction("factor", false);

            // Terminals
            var plus = gen.CreateNewProduction("plus");
            var minus = gen.CreateNewProduction("minus");
            var multiple = gen.CreateNewProduction("multiple");
            var divide = gen.CreateNewProduction("divide");
            var id = gen.CreateNewProduction("id");
            var op_open = gen.CreateNewProduction("op_open");
            var op_close = gen.CreateNewProduction("op_close");

            exp |= exp + plus + term;
            //exp |= exp + minus + term;
            exp |= term;
            term |= term + multiple + factor;
            //term |= term + divide + factor;
            term |= factor;
            factor |= id;
            factor |= op_open + exp + op_close;

            gen.PushStarts(exp);
            gen.Generate();
        }
    }

}