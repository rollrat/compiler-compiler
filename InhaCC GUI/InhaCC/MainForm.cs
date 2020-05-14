/*

   Copyright (C) 2019. rollrat All Rights Reserved.

   Author: Jeong HyunJun

*/

using GraphVizWrapper;
using GraphVizWrapper.Commands;
using GraphVizWrapper.Queries;
using ParserGenerator;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InhaCC
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void bREA_Click(object sender, EventArgs e)
        {
            var sr = new SimpleRegex();
            sr.MakeNFA(tbRE.Text);
            pictureBox1.Image = Graph.ToImage(SimpleRegex.PrintGraph(sr.Diagram));
            sr.OptimizeNFA();
            pictureBox2.Image = Graph.ToImage(SimpleRegex.PrintGraph(sr.Diagram));
            sr.NFAtoDFA();
            pictureBox3.Image = Graph.ToImage(SimpleRegex.PrintGraph(sr.Diagram));
            sr.MinimizeDFA();
            pictureBox4.Image = Graph.ToImage(SimpleRegex.PrintGraph(sr.Diagram));
        }

        Scanner scanner;

        private void bLG_Click(object sender, EventArgs e)
        {
            var sg = new ScannerGenerator();

            try
            {
                foreach (var line in rtbLLD.Lines)
                    sg.PushRule(line.Split(new[] { "=>" }, StringSplitOptions.None)[1].Replace("\"", "").Trim(), line.Split(new[] { "=>" }, StringSplitOptions.None)[0].Trim());
                sg.Generate();
                scanner = sg.CreateScannerInstance();
                rtbLS.AppendText("New scanner instance generated!\r\n" + sg.PrintDiagram());
            }
            catch (Exception ex)
            {
                rtbLS.Text = $"Scanner build error!\r\n{ex.Message}\r\n{ex.StackTrace}";
            }
        }

        private void bLT_Click(object sender, EventArgs e)
        {
            if (scanner == null)
            {
                rtbLS.AppendText("Create scanner instance before testing!\r\n");
                return;
            }

            rtbLS.AppendText(" ----------- Start Lexing -----------\r\n");
            scanner.AllocateTarget(rtbLT.Text);

            try
            {
                while (scanner.Valid())
                {
                    var ss = scanner.Next();
                    if (scanner.Error())
                        rtbLS.AppendText("Error!\r\n");
                    rtbLS.AppendText($"{ss.Item1},".PadRight(10) + $" {ss.Item2} - line:{ss.Item3}, column:{ss.Item4}\r\n");
                }
            }
            catch (Exception ex)
            {
                rtbLS.AppendText("Error!\r\nCheck test case!\r\n");
            }
            rtbLS.AppendText(" ------------ End Lexing ------------\r\n");
        }

        private void rtbLS_TextChanged(object sender, EventArgs e)
        {
            rtbLS.SelectionStart = rtbLS.Text.Length;
            rtbLS.ScrollToCaret();
        }

        ShiftReduceParser srparser;

        private void bPGG_Click(object sender, EventArgs e)
        {
            try
            {
                var gen = new ParserGenerator.ParserGenerator();
                var non_terminals = new Dictionary<string, ParserProduction>();
                var terminals = new Dictionary<string, ParserProduction>();

                terminals.Add("''", ParserGenerator.ParserGenerator.EmptyString);

                foreach (var nt in rtbPGNT.Text.Split(','))
                    non_terminals.Add(nt.Trim(), gen.CreateNewProduction(nt.Trim(), false));

                foreach (var t in rtbPGT.Lines)
                {
                    if (t.Trim() == "") continue;

                    var name = t.Split(',')[0];
                    var pp = t.Substring(name.Length + 1).Trim();

                    terminals.Add(pp, gen.CreateNewProduction(name.Trim()));
                }

                var prec = new Dictionary<string, List<Tuple<ParserProduction, int>>>();

                foreach (var pp in rtbPGPR.Lines)
                {
                    if (pp.Trim() == "") continue;

                    var split = pp.Split(new[] { "->" }, StringSplitOptions.None);
                    var left = split[0].Trim();
                    var right = split[1].Split(' ');

                    var prlist = new List<ParserProduction>();
                    bool stay_prec = false;
                    foreach (var ntt in right)
                    {
                        if (string.IsNullOrEmpty(ntt)) continue;
                        if (ntt == "%prec") { stay_prec = true; continue; }
                        if (stay_prec)
                        {
                            if (!prec.ContainsKey(ntt))
                                prec.Add(ntt, new List<Tuple<ParserProduction, int>>());
                            prec[ntt].Add(new Tuple<ParserProduction, int>(non_terminals[left], non_terminals[left].sub_productions.Count));
                            continue;
                        }
                        if (non_terminals.ContainsKey(ntt))
                            prlist.Add(non_terminals[ntt]);
                        else if (terminals.ContainsKey(ntt))
                            prlist.Add(terminals[ntt]);
                        else
                        {
                            rtbPGS.Text = $"Production rule build error!\r\n{ntt} is neither non-terminal nor terminal!\r\nDeclare the token-name!";
                            return;
                        }
                    }
                    non_terminals[left].sub_productions.Add(prlist);
                }

                for (int i = rtbPGC.Lines.Length - 1; i >= 0; i--)
                {
                    var line = rtbPGC.Lines[i].Trim();
                    if (line == "") continue;
                    var tt = line.Split(' ')[0];
                    var rr = line.Substring(tt.Length).Trim().Split(',');

                    var left = true;
                    var items1 = new List<Tuple<ParserProduction, int>>();
                    var items2 = new List<ParserProduction>();

                    if (tt == "%right") left = false;

                    foreach (var ii in rr.Select(x => x.Trim()))
                    {
                        if (string.IsNullOrEmpty(ii)) continue;
                        if (terminals.ContainsKey(ii))
                            items2.Add(terminals[ii]);
                        else if (prec.ContainsKey(ii))
                            items1.AddRange(prec[ii]);
                        else
                        {
                            rtbPGS.Text = $"Conflict rule applying error!\r\n{ii} is neither terminal nor %prec!\r\nDeclare the token-name!";
                            return;
                        }
                    }

                    if (items1.Count > 0)
                        gen.PushConflictSolver(left, items1.ToArray());
                    else
                        gen.PushConflictSolver(left, items2.ToArray());
                }

                rtbPGS.Clear();
                gen.GlobalPrinter.Clear();
                gen.PushStarts(non_terminals[rtbPGNT.Text.Split(',')[0].Trim()]);
                if (rbSLR.Checked == true)
                {
                    gen.Generate();
                }
                else if (rbLALR.Checked == true)
                {
                    gen.GenerateLALR();
                }
                else
                {
                    gen.GenerateLR1();
                }
                gen.PrintStates();
                gen.PrintTable();
                rtbPGS.AppendText(gen.GlobalPrinter.ToString());
                srparser = gen.CreateShiftReduceParserInstance();
            } catch (Exception ex)
            {
                rtbPGS.AppendText("Generate Error!\r\n" + ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private void bPGT_Click(object sender, EventArgs e)
        {
            if (scanner == null)
            {
                rtbPGS.AppendText("Create scanner instance before testing!\r\n");
                return;
            }

            if (srparser == null)
            {
                rtbPGS.AppendText("Create parser instance before testing!\r\n");
                return;
            }

            foreach (var line in rtbPGTEST.Lines)
            {
                rtbPGS.AppendText(" ------ TEST: " + line + "\r\n");

                srparser.Clear();
                Action<string, string> insert = (string x, string y) =>
                {
                    srparser.Insert(x, y);
                    if (srparser.Error())
                        rtbPGS.AppendText("PARSING ERROR" + "\r\n");
                    while (srparser.Reduce())
                    {
                        rtbPGS.AppendText(srparser.Stack() + "\r\n");
                        var l = srparser.LatestReduce();
                        rtbPGS.AppendText(l.Produnction.PadLeft(8) + " => ");
                        rtbPGS.AppendText(string.Join(" ", l.Childs.Select(z => z.Produnction)) + "\r\n");
                        rtbPGS.AppendText(l.Produnction.PadLeft(8) + " => ");
                        rtbPGS.AppendText(string.Join(" ", l.Childs.Select(z => z.Contents)) + "\r\n");
                        srparser.Insert(x, y);
                        if (srparser.Error())
                            rtbPGS.AppendText("PARSING ERROR" + "\r\n");
                    }
                    rtbPGS.AppendText(srparser.Stack() + "\r\n");
                };

                scanner.AllocateTarget(line);

                try
                {
                    while (scanner.Valid())
                    {
                        var ss = scanner.Next();
                        insert(ss.Item1, ss.Item2);
                    }
                    insert("$", "$");

                    var x = srparser.Tree;
                }
                catch (Exception ex)
                {
                    rtbPGS.AppendText("Error!\r\nCheck test case!\r\n");
                }

                rtbPGS.AppendText(" ------ END TEST ------\r\n");
            }
        }

        private void rtbPGS_TextChanged(object sender, EventArgs e)
        {
            rtbPGS.SelectionStart = rtbPGS.Text.Length;
            rtbPGS.ScrollToCaret();
        }
    }
}
