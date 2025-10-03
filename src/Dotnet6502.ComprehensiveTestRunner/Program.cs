using Dotnet6502.ComprehensiveTestRunner;

if (args.Length != 2)
{
    Console.WriteLine("Usage: Dotnet6502.ComprehensiveTestRunner <mnemonic> <output-csv-file>");
    Console.WriteLine("Example: Dotnet6502.ComprehensiveTestRunner LDA failures.csv");
    return 1;
}

var mnemonic = args[0];
var outputFile = args[1];
var absoluteOutputPath = Path.GetFullPath(outputFile);

Console.WriteLine($"Running comprehensive tests for mnemonic: {mnemonic}");
Console.WriteLine($"Output file: {absoluteOutputPath}");
Console.WriteLine();

var startTime = DateTime.Now;

try
{
    var (totalTests, failures) = await TestRunner.RunTestsForMnemonic(mnemonic);

    var elapsed = DateTime.Now - startTime;

    Console.WriteLine();
    Console.WriteLine($"=== Test Summary ===");
    Console.WriteLine($"Total tests: {totalTests}");
    Console.WriteLine($"Failures: {failures.Count}");
    Console.WriteLine($"Pass rate: {(totalTests > 0 ? (totalTests - failures.Count) * 100.0 / totalTests : 0):F2}%");
    Console.WriteLine($"Execution time: {elapsed.TotalSeconds:F2}s");

    if (failures.Count > 0)
    {
        CsvWriter.WriteFailures(absoluteOutputPath, failures);
        Console.WriteLine($"Failures written to: {absoluteOutputPath}");
        return 1;
    }
    else
    {
        Console.WriteLine("All tests passed!");
        return 0;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    return 1;
}
