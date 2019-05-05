# compiler-compiler
Compiler Compiler based on CSharp

## Scanner Generator / Lexical Analyzer Generator

A tool for analyzing regular expressions using `(`,`)`,`+`,`*`,`?`,`|` as tokens and generating Scanner tables.

There is a Delimiter problem, and it is currently being resolved.

### Example

``` cs
var sg = new ScannerGenerator();
sg.PushRule("if", "if");
sg.PushRule("for", "for");
sg.PushRule("else", "else");
sg.PushRule("id", "(a|b|c|d|e|f|g|h|i|j|k|l|m|n|o|p|q|r|s|t|u|v|w|x|y|z)(a|b|c|d|e|f|g|h|i|j|k|l|m|n|o|p|q|r|s|t|u|v|w|x|y|z|0|1|2|3|4|5|6|7|8|9)*");
sg.PushRule("num", "(0|1|2|3|4|5|6|7|8|9)+");
sg.Generate();
sg.PrintDiagram();
sg.CreateScannerInstance();
 
var scanner = sg.CreateScannerInstance();
scanner.AllocateTarget("asdf a1321 if else 0415 0456a 9999");
while (scanner.Valid())
{
    var ss = scanner.Next();
    Console.WriteLine($"{ss.Item1}, {ss.Item2}");
}
```

```
id, asdf
plus, +
id, a1321
if, if
else, else
num, 0415
, abse+9999  // Scan error from delimiter problem
```

## Parser Generator / SLR Generator

I will also create LR(1) and LALR parsers.

### Example

``` cs
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
exp |= term;
term |= term + multiple + factor;
term |= factor;
factor |= id;
factor |= op_open + exp + op_close;

gen.PushStarts(exp);
gen.Generate();
```

```
I0 => SHIFT{(exp,I1),(term,I2),(factor,I3),(id,I4),(op_open,I5)}
I1 => SHIFT{(plus,I6)}
      REDUCE{($,accept,0)}
I2 => SHIFT{(multiple,I7)}
      REDUCE{(plus,exp,1),(op_close,exp,1),($,exp,1)}
I3 => REDUCE{(multiple,term,1),(plus,term,1),(op_close,term,1),($,term,1)}
I4 => REDUCE{(multiple,factor,0),(plus,factor,0),(op_close,factor,0),($,factor,0)}
I5 => SHIFT{(exp,I8),(term,I2),(factor,I3),(id,I4),(op_open,I5)}
I6 => SHIFT{(term,I9),(factor,I3),(id,I4),(op_open,I5)}
I7 => SHIFT{(factor,I10),(id,I4)}
I8 => SHIFT{(op_close,I11),(plus,I6)}
I9 => SHIFT{(multiple,I7)}
      REDUCE{(plus,exp,0),(op_close,exp,0),($,exp,0)}
I10 => REDUCE{(multiple,term,0),(plus,term,0),(op_close,term,0),($,term,0)}
I11 => REDUCE{(multiple,factor,1),(plus,factor,1),(op_close,factor,1),($,factor,1)}
```
