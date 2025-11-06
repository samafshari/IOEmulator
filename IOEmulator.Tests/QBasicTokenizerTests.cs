using System;
using System.Threading.Tasks;
using Xunit;

namespace Neat.Test;

/// <summary>
/// Tests for tokenization, arithmetic expressions, and core language features
/// to ensure the QBASIC interpreter is bulletproof.
/// </summary>
public class QBasicTokenizerTests
{
    [Fact(Timeout = 5000)]
    public async Task Tokenizer_Separates_Operators_From_Numbers()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 10+5
PSET A, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(15, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Tokenizer_Separates_Operators_From_Identifiers()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
X = 10
Y = 5
A = X+Y
PSET A, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(15, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Tokenizer_Handles_Multiple_Operators_In_Sequence()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 20-5+10
PSET A, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(25, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Tokenizer_Handles_Multiplication_And_Division()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 4*5
B = 20/2
PSET A, 10, 15
PSET B, 11, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            Assert.NotEqual(bg, io.ReadPixelAt(20, 10));
            Assert.NotEqual(bg, io.ReadPixelAt(10, 11));
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Arithmetic_Precedence_Multiplication_Before_Addition()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 2+3*4
PSET A, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(14, 10); // 2 + (3*4) = 14
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Arithmetic_Parentheses_Override_Precedence()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
CLS
    YY = (2+3)*4
    PSET YY, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
                var px = io.ReadPixelAt(20, 10); // (2+3)*4 = 20
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Arithmetic_Nested_Parentheses()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            interp.SpeedFactor = 100.0;
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = ((10+5)*2)/3
PSET 10, A, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(10, 10); // ((10+5)*2)/3 = 30/3 = 10
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Arithmetic_Unary_Plus()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = +10
PSET A, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(10, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Arithmetic_Unary_Minus()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 20 + -5
PSET A, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(15, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Arithmetic_Division_By_Zero_Returns_Zero()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 10/0
PSET 10, 10, 15
";
            // Should not crash
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(10, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Expression_In_Function_Call_PC()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
PSET 10, 10, 14
X = 5
Y = 5
IF PC(X+5, Y+5) = 14 THEN PSET 0, 0, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(0, 0);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Expression_In_Function_Call_PX()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
PSET 10, 10, 14
X = 5
Y = 5
IF PX(X+5, Y+5) = 1 THEN PSET 0, 0, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(0, 0);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Comparison_Operators_All_Work()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 10
B = 5
IF A = 10 THEN PSET 0, 0, 15
IF A < 20 THEN PSET 1, 0, 15
IF A > 5 THEN PSET 2, 0, 15
IF A <= 10 THEN PSET 3, 0, 15
IF A >= 10 THEN PSET 4, 0, 15
IF A <> 5 THEN PSET 5, 0, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            Assert.NotEqual(bg, io.ReadPixelAt(0, 0));
            Assert.NotEqual(bg, io.ReadPixelAt(1, 0));
            Assert.NotEqual(bg, io.ReadPixelAt(2, 0));
            Assert.NotEqual(bg, io.ReadPixelAt(3, 0));
            Assert.NotEqual(bg, io.ReadPixelAt(4, 0));
            Assert.NotEqual(bg, io.ReadPixelAt(5, 0));
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Comparison_With_Expressions()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 10
B = 5
IF A+B = 15 THEN PSET 0, 0, 15
IF A*2 > B+10 THEN PSET 1, 0, 15
IF A-B < A THEN PSET 2, 0, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            Assert.NotEqual(bg, io.ReadPixelAt(0, 0));
            Assert.NotEqual(bg, io.ReadPixelAt(1, 0));
            Assert.NotEqual(bg, io.ReadPixelAt(2, 0));
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Multiple_Statements_On_One_Line_With_Colons()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 10 : B = 20 : C = A + B
PSET C, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(30, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task IF_THEN_With_Multiple_Actions_Separated_By_Colons()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
IF 1 = 1 THEN A = 10 : B = 20 : PSET A, 10, 15 : PSET B, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            Assert.NotEqual(bg, io.ReadPixelAt(10, 10));
            Assert.NotEqual(bg, io.ReadPixelAt(20, 10));
        });
    }

    [Fact(Timeout = 5000)]
    public async Task IF_THEN_ELSE_With_Multiple_Actions()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
IF 1 = 0 THEN PSET 0, 0, 15 ELSE A = 10 : PSET A, 10, 15
";
            interp.Run(src);
            var bgIndex = io.BackgroundColorIndex;
            var px0 = io.ReadPixelAt(0, 0);
            var px10 = io.ReadPixelAt(10, 10);
            Assert.Equal(bgIndex, px0); // Should NOT be set (THEN branch not taken)
            Assert.NotEqual(bgIndex, px10); // Should be set (ELSE branch taken)
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Nested_IF_In_THEN_Clause()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 10
B = 20
IF A = 10 THEN IF B = 20 THEN PSET 10, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(10, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Complex_Expression_With_All_Operators()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            interp.SpeedFactor = 100.0;
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = (10+5)*2-20/4+3
PSET 10, A, 15
";
            interp.Run(src);
            // (10+5)*2-20/4+3 = 15*2-5+3 = 30-5+3 = 28
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(10, 28);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task PSET_With_All_Coordinates_As_Expressions()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
X = 5
Y = 5
C = 7
PSET X*2, Y*2, C+8
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(10, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task LINE_With_All_Coordinates_As_Expressions()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
X1 = 5
Y1 = 5
X2 = 10
Y2 = 10
LINE X1*2, Y1*2, X2*2, Y2*2, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            // Line should draw from (10,10) to (20,20)
            var px = io.ReadPixelAt(15, 15);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Variable_Names_Can_Contain_Numbers()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
X1 = 10
Y2 = 20
VAR123 = X1 + Y2
PSET VAR123, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(30, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Variable_Names_With_Underscores()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
MY_VAR = 10
OTHER_VAR = 5
RESULT = MY_VAR + OTHER_VAR
PSET RESULT, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(15, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Zero_Values_In_Expressions()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 0
B = 10
C = A + B
PSET C, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(10, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Negative_Results_In_Expressions()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 5 - 10
B = 25
C = A + B
PSET C, 10, 15
";
            interp.Run(src);
            // A = -5, C = -5 + 25 = 20
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(20, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Expression_In_IF_Condition_Both_Sides()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 10
B = 5
C = 3
IF A+B = B*C THEN PSET 10, 10, 15
";
            interp.Run(src);
            // A+B = 15, B*C = 15
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(10, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Chained_Assignments()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 5
B = A
C = B
D = C
PSET D*2, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(10, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Assignment_With_Complex_RHS()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            interp.SpeedFactor = 100.0;
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = (10+5)*(2+3)-20/4
PSET 10, A, 15
";
            interp.Run(src);
            // (10+5)*(2+3)-20/4 = 15*5-5 = 75-5 = 70
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(10, 70);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Whitespace_Around_Operators_Is_Optional()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A=10+5
B =A*2
C= B-10
PSET C,10,15
";
            interp.Run(src);
            // A=15, B=30, C=20
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(20, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task GOTO_To_Label_With_Complex_Expression_After()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 5
GOTO SKIP
A = 0
SKIP:
B = A * 2
PSET B, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(10, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Multiple_Nested_Parentheses_In_Expression()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = (((10)))
B = ((5+5))
C = (A+(B-5))
PSET C, 10, 15
";
            interp.Run(src);
            // A=10, B=10, C=10+(10-5)=15
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(15, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Expression_With_Spaces_In_Middle()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 10 + 5 * 2 - 3
PSET A, 10, 15
";
            interp.Run(src);
            // 10 + 5*2 - 3 = 10 + 10 - 3 = 17
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(17, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task String_Variables_With_Numbers()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A$ = ""123""
B = VAL(A$)
PSET B, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(123, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Arithmetic_Complex_Nested_Expressions_With_Multiple_Operators()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = ((10 + 5) * 2 - 3) / 2 + 7
PSET A, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            // ((10 + 5) * 2 - 3) / 2 + 7 = ((15 * 2) - 3) / 2 + 7 = (30 - 3) / 2 + 7 = 27 / 2 + 7 = 13 + 7 = 20
            var px = io.ReadPixelAt(20, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Arithmetic_Order_Of_Operations_With_Unary_Operators()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = -5 + +3 * 4 - -2 / 2
PSET A, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            // -5 + (+3 * 4) - (-2 / 2) = -5 + 12 - (-1) = -5 + 12 + 1 = 8
            var px = io.ReadPixelAt(8, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Arithmetic_Deeply_Nested_Parentheses()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = (((((10 + 5) * 2) - 3) / 2) + 7)
PSET A, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            // (((((10 + 5) * 2) - 3) / 2) + 7) = (((15 * 2) - 3) / 2) + 7 = ((30 - 3) / 2) + 7 = (27 / 2) + 7 = 13 + 7 = 20
            var px = io.ReadPixelAt(20, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Tokenization_Handles_Special_Characters_In_Expressions()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 10+5*2-3/2+7
PSET A, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            // 10 + 5*2 - 3/2 + 7 = 10 + 10 - 1 + 7 = 26
            var px = io.ReadPixelAt(26, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Conditions_Complex_Compound_Conditions_With_AND_Logic()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
X = 5
Y = 10
IF X < Y AND X > 0 THEN PSET 10, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(10, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Conditions_Complex_Compound_Conditions_With_OR_Logic()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
X = 15
Y = 10
IF X > Y OR X < 0 THEN PSET 20, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(20, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Conditions_Nested_IF_With_Complex_Conditions()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 5
B = 10
C = 15
IF A < B THEN IF B < C THEN PSET 30, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(30, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Conditions_IF_ELSE_With_Complex_Expressions()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
X = 5
Y = 3
IF (X * 2) > (Y + 10) THEN PSET 40, 10, 15 ELSE PSET 50, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            // (5 * 2) = 10, (3 + 10) = 13, 10 > 13 is false, so ELSE branch
            var px = io.ReadPixelAt(50, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Jumps_GOTO_With_Label()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
GOTO SKIP
PSET 10, 10, 15
SKIP:
PSET 60, 10, 15
";
            interp.Run(src);
            var bgIndex = io.BackgroundColorIndex;
            // Should skip the first PSET and only execute the second
            var px1 = io.ReadPixelAt(10, 10);
            var px2 = io.ReadPixelAt(60, 10);
            Assert.Equal(bgIndex, px1); // First pixel should not be set
            Assert.NotEqual(bgIndex, px2); // Second pixel should be set
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Jumps_GOTO_With_Conditional_Jump()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
X = 5
IF X > 3 THEN GOTO TARGET
PSET 10, 10, 15
TARGET:
PSET 70, 10, 15
";
            interp.Run(src);
            var bgIndex = io.BackgroundColorIndex;
            // Should jump to TARGET and skip the first PSET
            var px1 = io.ReadPixelAt(10, 10);
            var px2 = io.ReadPixelAt(70, 10);
            Assert.Equal(bgIndex, px1); // First pixel should not be set
            Assert.NotEqual(bgIndex, px2); // Second pixel should be set
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Variables_Long_Variable_Names_With_Numbers()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
MYVAR123 = 80
PSET MYVAR123, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(80, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Variables_Variable_Reassignment()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
X = 10
PSET X, 10, 15
X = 20
PSET X, 20, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px1 = io.ReadPixelAt(10, 10);
            var px2 = io.ReadPixelAt(20, 20);
            Assert.NotEqual(bg, px1);
            Assert.NotEqual(bg, px2);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Expressions_In_Graphics_Commands_With_Variables()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
X = 10
Y = 20
WIDTH = 30
HEIGHT = 40
LINE (X, Y)-(X + WIDTH, Y + HEIGHT), 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            // Check some pixels along the line
            var px1 = io.ReadPixelAt(10, 20);
            var px2 = io.ReadPixelAt(40, 60);
            Assert.NotEqual(bg, px1);
            Assert.NotEqual(bg, px2);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Complex_Arithmetic_In_Function_Calls()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
PSET 10, 10, 15
RESULT = PC((10 + 5) * 2 - 3, 10)
IF RESULT = 15 THEN PSET 90, 10, 15
";
            interp.Run(src);
            var bgIndex = io.BackgroundColorIndex;
            // PC should return the index at ((10 + 5) * 2 - 3, 10) = (27, 10)
            // Since we set (10,10) to 15, PC(27,10) should return 0 (background)
            // So the condition should be false and no pixel at (90,10)
            var px = io.ReadPixelAt(90, 10);
            Assert.Equal(bgIndex, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Tokenization_Handles_Whitespace_Around_Operators()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A=10   +   5*2   -   3/2   +   7
PSET A, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            // Same calculation as before: 10 + 5*2 - 3/2 + 7 = 26
            var px = io.ReadPixelAt(26, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Conditions_With_All_Comparison_Operators_In_Sequence()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 10
B = 5
C = 15
IF A > B AND B < C AND A <> B AND A >= 10 AND B <= 5 THEN PSET 100, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(100, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Arithmetic_With_Zero_And_Negative_Results()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
A = 10 - 10 + 5 - 5
B = 3 - 5
IF A = 0 AND B = -2 THEN PSET 110, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            var px = io.ReadPixelAt(110, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Complex_IF_THEN_ELSE_With_Nested_Conditions()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
X = 5
Y = 10
Z = 15
IF X < Y THEN IF Y < Z THEN PSET 120, 10, 15 ELSE PSET 130, 10, 15 ELSE PSET 140, 10, 15
";
            interp.Run(src);
            var bgIndex = io.BackgroundColorIndex;
            // X < Y is true, Y < Z is true, so should execute PSET 120, 10, 15
            var px1 = io.ReadPixelAt(120, 10);
            var px2 = io.ReadPixelAt(130, 10);
            var px3 = io.ReadPixelAt(140, 10);
            Assert.NotEqual(bgIndex, px1);
            Assert.Equal(bgIndex, px2);
            Assert.Equal(bgIndex, px3);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task GOTO_With_Backward_Jump()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
COUNTER = 0
START:
COUNTER = COUNTER + 1
IF COUNTER < 3 THEN GOTO START
PSET COUNTER * 10, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            // Should loop 3 times, COUNTER = 3, so pixel at 30, 10
            var px = io.ReadPixelAt(30, 10);
            Assert.NotEqual(bg, px);
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Arithmetic_With_Function_Calls_In_Expressions()
    {
        await Task.Run(() =>
        {
            var io = new IOEmulator();
            var qb = new QBasicApi(io);
            var interp = new QBasicInterpreter(qb);
            string src = @"SCREEN 13
COLOR 15,0
CLS
PSET 10, 10, 15
PSET 20, 20, 12
COLOR1 = PC(10, 10)
COLOR2 = PC(20, 20)
RESULT = COLOR1 + COLOR2
PSET RESULT, 10, 15
";
            interp.Run(src);
            var bg = io.GetColor(io.BackgroundColorIndex);
            // PC(10,10) should return 15, PC(20,20) should return 12, sum = 27
            var px = io.ReadPixelAt(27, 10);
            Assert.NotEqual(bg, px);
        });
    }
}
