# compiler-compiler
Compiler Compiler based on CSharp

## Scanner Generator / Lexical Analyzer Generator

A tool for analyzing regular expressions using `(`,`)`,`+`,`*`,`?`,`|`,`[`,`]` as tokens and generating Scanner tables.

### Example

``` cs
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
```

```
plus, +
id, a1321 // Ignore '/'
if, if
else, else
num, 0415
id, abse
plus, +
num, 9999
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
factor |= op_open + exp + op_close;
factor |= id;

gen.PushStarts(exp);
gen.Generate();
gen.PrintStates();
```

```
I0 => SHIFT{(exp,I1),(term,I2),(factor,I3),(op_open,I4),(id,I5)}
I1 => SHIFT{(plus,I6)}
      REDUCE{($,accept,0)}
I2 => SHIFT{(multiple,I7)}
      REDUCE{(plus,exp,1),(op_close,exp,1),($,exp,1)}
I3 => REDUCE{(multiple,term,1),(plus,term,1),(op_close,term,1),($,term,1)}
I4 => SHIFT{(exp,I8),(term,I2),(factor,I3),(op_open,I4),(id,I5)}
I5 => REDUCE{(multiple,factor,1),(plus,factor,1),(op_close,factor,1),($,factor,1)}
I6 => SHIFT{(term,I9),(factor,I3),(op_open,I4),(id,I5)}
I7 => SHIFT{(factor,I10),(op_open,I4),(id,I5)}
I8 => SHIFT{(op_close,I11),(plus,I6)}
I9 => SHIFT{(multiple,I7)}
      REDUCE{(plus,exp,0),(op_close,exp,0),($,exp,0)}
I10 => REDUCE{(multiple,term,0),(plus,term,0),(op_close,term,0),($,term,0)}
I11 => REDUCE{(multiple,factor,0),(plus,factor,0),(op_close,factor,0),($,factor,0)}
```

## Shift Reduce Parser

``` cs
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
arguments |= id;
arguments |= arguments + split + id;
arguments |= ParserGenerator.EmptyString;

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
        Console.Instance.Write(l.Produnction.PadLeft(8) + " => ");
        Console.Instance.WriteLine(string.Join(" ", l.Childs.Select(z => z.Produnction)));
        Console.Instance.Write(l.Produnction.PadLeft(8) + " => ");
        Console.Instance.WriteLine(string.Join(" ", l.Childs.Select(z => z.Contents)));
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
```

```
  factor => num
  factor => 2
    term => factor
    term => 2
     exp => term
     exp => 2
  factor => num
  factor => 6
    term => factor
    term => 6
  factor => num
  factor => 6
    term => factor
    term => 6
     exp => term
     exp => 6
  factor => num
  factor => 4
    term => factor
    term => 4
  factor => num
  factor => 7
    term => term multiple factor
    term => 4 * 7
     exp => exp plus term
     exp => 6 + 4*7
  factor => num
  factor => 2
    term => factor
    term => 2
     exp => exp minus term
     exp => 6+4*7 - 2
  factor => op_open exp op_close
  factor => ( 6+4*7-2 )
    term => term multiple factor
    term => 6 * (6+4*7-2)
     exp => exp plus term
     exp => 2 + 6*(6+4*7-2)
    args => id
    args => a
    args => args split id
    args => a , b
    args => args split id
    args => a,b , c
    args => args split id
    args => a,b,c , d
    func => id op_open args op_close
    func => sin ( a,b,c,d )
  factor => func
  factor => sin(a,b,c,d)
    term => factor
    term => sin(a,b,c,d)
     exp => exp plus term
     exp => 2+6*(6+4*7-2) + sin(a,b,c,d)
    func => id op_open op_close
    func => cos ( )
  factor => func
  factor => cos()
    term => factor
    term => cos()
  factor => id
  factor => pi
    term => term multiple factor
    term => cos() * pi
     exp => exp plus term
     exp => 2+6*(6+4*7-2)+sin(a,b,c,d) + cos()*pi
```
