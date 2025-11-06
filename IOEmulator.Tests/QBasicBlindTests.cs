using Neat;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Neat.Test
{
    public class QBasicBlindTests
    {
        private async Task<string> RunScriptAndGetOutput(string script, int timeout = 2000)
        {
            var io = new IOEmulator();
            var sound = new TestSoundDriver();
            var qb = new QBasicApi(io, sound);
            var interp = new QBasicInterpreter(qb);
            interp.SuppressEndPrompt = true;

            var outputBuilder = new StringBuilder();
            qb.PrintHook = (s) => outputBuilder.Append(s);

            var runTask = Task.Run(() => interp.Run(script));
            var completedTask = await Task.WhenAny(runTask, Task.Delay(timeout));

            if (completedTask != runTask)
            {
                // The interpreter thread is still running, so we can't safely clean up.
                // We'll just throw a timeout exception and let the test runner handle it.
                throw new System.TimeoutException($"Test timed out after {timeout}ms.");
            }

            // If runTask completed, it's safe to access the result (and see if it threw an exception)
            await runTask;

            // Preserve leading spaces (e.g., STR$ on positive numbers) but trim trailing newlines
            return outputBuilder.ToString().TrimEnd().Replace("\r\n", "\n");
        }

        private class TestSoundDriver : ISoundDriver
        {
            public void Beep() { }
            public void PlayMusicString(string musicString) { }
            public void PlayTone(int frequencyHz, int durationMs) { }
        }

        [Theory]
        [InlineData("PRINT 2 + 2", "4")]
        [InlineData("PRINT 5 - 3", "2")]
        [InlineData("PRINT 3 * 4", "12")]
        [InlineData("PRINT 10 / 2", "5")]
        [InlineData("PRINT 10 \\ 3", "3")]
        [InlineData("PRINT 10 MOD 3", "1")]
        [InlineData("PRINT -5 + 2", "-3")]
        public async Task Arithmetic_Operations_Should_Produce_Correct_Results(string script, string expected)
        {
            var output = await RunScriptAndGetOutput(script);
            Assert.Equal(expected, output);
        }

        [Theory]
        [InlineData("PRINT 2 + 2 * 2", "6")]
        [InlineData("PRINT (2 + 2) * 2", "8")]
        [InlineData("PRINT 10 / 2 * 5", "25")]
        [InlineData("PRINT 10 / (2 * 5)", "1")]
        [InlineData("PRINT 2 + 3 * 4 - 5 / 5", "13")]
        [InlineData("PRINT 1 + 2 * 3 ^ 2", "19")]
        [InlineData("PRINT (1 + 2) * 3 ^ 2", "27")]
        public async Task Expression_Evaluation_Should_Respect_Operator_Precedence(string script, string expected)
        {
            var output = await RunScriptAndGetOutput(script);
            Assert.Equal(expected, output);
        }

        [Theory]
        [InlineData("PRINT \"Hello\" + \" \" + \"World\"", "Hello World")]
        [InlineData("A$ = \"Hello\": B$ = \"World\": PRINT A$ + B$", "HelloWorld")]
        [InlineData("PRINT LEFT$(\"Hello World\", 5)", "Hello")]
        [InlineData("PRINT RIGHT$(\"Hello World\", 5)", "World")]
        [InlineData("PRINT MID$(\"Hello World\", 7, 5)", "World")]
        [InlineData("PRINT LEN(\"Hello World\")", "11")]
        [InlineData("PRINT STR$(123)", " 123")] // Note: QBasic STR$ adds a leading space for positive numbers
        [InlineData("PRINT LTRIM$(\"   Hello\")", "Hello")]
        [InlineData("PRINT RTRIM$(\"Hello   \")", "Hello")]
        [InlineData("PRINT TRIM$(\"   Hello   \")", "Hello")]
        [InlineData("PRINT CHR$(65)", "A")]
        [InlineData("PRINT ASC(\"A\")", "65")]
        public async Task String_Manipulation_Functions_Should_Work_Correctly(string script, string expected)
        {
            var output = await RunScriptAndGetOutput(script);
            Assert.Equal(expected, output);
        }

        [Theory]
        // IF...THEN...ELSE
        [InlineData("IF 1 = 1 THEN PRINT \"True\" ELSE PRINT \"False\"", "True")]
        [InlineData("IF 1 = 0 THEN PRINT \"True\" ELSE PRINT \"False\"", "False")]
        [InlineData("A = 10: IF A > 5 THEN PRINT \"Greater\"", "Greater")]
        // FOR...NEXT
        [InlineData("FOR I = 1 TO 3: PRINT I;: NEXT I", "123")]
        [InlineData("FOR I = 5 TO 1 STEP -1: PRINT I;: NEXT I", "54321")]
        // DO...LOOP
        [InlineData("I = 1: DO WHILE I <= 3: PRINT I;: I = I + 1: LOOP", "123")]
        [InlineData("I = 1: DO: PRINT I;: I = I + 1: LOOP UNTIL I > 3", "123")]
        public async Task Control_Flow_Statements_Should_Execute_Correctly(string script, string expected)
        {
            var output = await RunScriptAndGetOutput(script);
            Assert.Equal(expected, output.Replace(" ", "").Replace("\n", ""));
        }

        [Fact(Timeout = 5000)]
        public async Task Goto_Statement_Should_Jump_To_Label()
        {
            var script = @"
                GOTO myLabel
                PRINT ""Skip""
            myLabel:
                PRINT ""Jump""
            ";
            var output = await RunScriptAndGetOutput(script);
            Assert.Equal("Jump", output.Trim());
        }

        [Fact(Timeout = 5000)]
        public async Task Data_Read_Restore_Statements_Should_Work_Correctly()
        {
            var script = @"
                DATA 1, ""Hello"", 3.14
                READ A, B$, C
                PRINT A;
                PRINT B$;
                PRINT C;
                RESTORE
                READ D, E$
                PRINT D;
                PRINT E$;
            ";
            var output = await RunScriptAndGetOutput(script);
            Assert.Equal("1Hello3.141Hello", output.Replace(" ", "").Replace("\n", ""));
        }

        [Theory]
        [InlineData("A = 1: SELECT CASE A: CASE 1: PRINT \"One\": CASE 2: PRINT \"Two\": END SELECT", "One")]
        [InlineData("A = 2: SELECT CASE A: CASE 1: PRINT \"One\": CASE 2: PRINT \"Two\": END SELECT", "Two")]
        [InlineData("A = 3: SELECT CASE A: CASE 1: PRINT \"One\": CASE ELSE: PRINT \"Other\": END SELECT", "Other")]
        [InlineData("A$ = \"Hi\": SELECT CASE A$: CASE \"Hi\": PRINT \"Hello\": END SELECT", "Hello")]
        public async Task Select_Case_Statement_Should_Work_Correctly(string script, string expected)
        {
            var output = await RunScriptAndGetOutput(script);
            Assert.Equal(expected, output.Trim());
        }

        [Fact(Timeout = 5000)]
        public async Task Dim_Statement_Should_Declare_Arrays()
        {
            var script = @"
                DIM A(10)
                A(1) = 123
                PRINT A(1)
            ";
            var output = await RunScriptAndGetOutput(script);
            Assert.Equal("123", output.Trim());
        }

        [Fact(Timeout = 5000)]
        public async Task Dim_Statement_Should_Declare_MultiDimensional_Arrays()
        {
            var script = @"
                DIM A(5, 5)
                A(2, 3) = 456
                PRINT A(2, 3)
            ";
            var output = await RunScriptAndGetOutput(script);
            Assert.Equal("456", output.Trim());
        }

        [Fact(Timeout = 5000)]
        public async Task Sub_And_Function_Should_Be_Callable()
        {
            var script = @"
                DECLARE SUB MySub (A)
                DECLARE FUNCTION MyFunc# (B)

                MySub 5
                PRINT MyFunc(10)

                END

                SUB MySub (A)
                    PRINT A;
                END SUB

                FUNCTION MyFunc (B)
                    MyFunc = B * 2
                END FUNCTION
            ";
            var output = await RunScriptAndGetOutput(script);
            Assert.Equal("520", output.Replace(" ", "").Replace("\n", ""));
        }

        [Fact(Timeout = 5000)]
        public async Task Recursive_Function_Should_Work_Correctly()
        {
            var script = @"
                DECLARE FUNCTION Factorial (N)
                PRINT Factorial(5)
                END

                FUNCTION Factorial (N)
                    IF N <= 1 THEN
                        Factorial = 1
                    ELSE
                        Factorial = N * Factorial(N - 1)
                    END IF
                END FUNCTION
            ";
            var output = await RunScriptAndGetOutput(script);
            Assert.Equal("120", output.Trim());
        }

        [Fact(Timeout = 5000)]
        public async Task MultiLine_If_Statement_Should_Work_Correctly()
        {
            var script = @"
                A = 10
                IF A = 10 THEN
                    PRINT ""A is 10""
                ELSEIF A = 5 THEN
                    PRINT ""A is 5""
                ELSE
                    PRINT ""A is something else""
                END IF
            ";
            var output = await RunScriptAndGetOutput(script);
            Assert.Equal("A is 10", output.Trim());
        }

        [Fact(Timeout = 5000)]
        public async Task MultiLine_For_Loop_Should_Work_Correctly()
        {
            var script = @"
                FOR I = 1 TO 3
                    PRINT I
                NEXT I
            ";
            var output = await RunScriptAndGetOutput(script);
            Assert.Equal("1\n2\n3", output.Trim());
        }

        [Fact(Timeout = 5000)]
        public async Task Block_Statements_With_Nested_Logic_Should_Work()
        {
            var script = @"
                FOR I = 1 TO 5
                    IF I MOD 2 = 0 THEN
                        PRINT I; "" is even""
                    ELSE
                        PRINT I; "" is odd""
                    END IF
                NEXT I
            ";
            var expected = "1 is odd\n2 is even\n3 is odd\n4 is even\n5 is odd";
            var output = await RunScriptAndGetOutput(script);
            Assert.Equal(expected, output.Trim());
        }
    }
}
