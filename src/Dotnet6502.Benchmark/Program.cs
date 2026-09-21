
using Dotnet6502.Benchmark;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Nes;

var options = CommandLineHandler.Parse(args);
if (options == null)
{
    return 1;
}

var jitCustomizer = new NesJitCustomizer();
var interpreter = new Ir6502Interpreter();
jitCustomizer.AddInstructions(interpreter);



return 0;

