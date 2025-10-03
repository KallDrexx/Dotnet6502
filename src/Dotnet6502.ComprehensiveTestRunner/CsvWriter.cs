namespace Dotnet6502.ComprehensiveTestRunner;

public static class CsvWriter
{
    public static void WriteFailures(string filePath, IEnumerable<TestFailure> failures)
    {
        using var writer = new StreamWriter(filePath);

        // Write header
        writer.WriteLine("Mnemonic,HexBytes,InitialA,InitialX,InitialY,InitialP,InitialS,InitialPc,InitialRam," +
                        "ExpectedA,ExpectedX,ExpectedY,ExpectedP,ExpectedS,ExpectedPc,ExpectedRam," +
                        "ActualA,ActualX,ActualY,ActualP,ActualS,ActualPc,ActualRam," +
                        "ReadRamAddresses,ErrorMessage");

        // Write each failure
        foreach (var failure in failures)
        {
            writer.WriteLine($"{EscapeCsv(failure.Mnemonic)}," +
                           $"{EscapeCsv(failure.HexBytes)}," +
                           $"{failure.InitialA}," +
                           $"{failure.InitialX}," +
                           $"{failure.InitialY}," +
                           $"{failure.InitialP}," +
                           $"{failure.InitialS}," +
                           $"{failure.InitialPc}," +
                           $"{EscapeCsv(failure.InitialRam)}," +
                           $"{failure.ExpectedA}," +
                           $"{failure.ExpectedX}," +
                           $"{failure.ExpectedY}," +
                           $"{failure.ExpectedP}," +
                           $"{failure.ExpectedS}," +
                           $"{failure.ExpectedPc}," +
                           $"{EscapeCsv(failure.ExpectedRam)}," +
                           $"{failure.ActualA}," +
                           $"{failure.ActualX}," +
                           $"{failure.ActualY}," +
                           $"{failure.ActualP}," +
                           $"{failure.ActualS}," +
                           $"{failure.ActualPc}," +
                           $"{EscapeCsv(failure.ActualRam)}," +
                           $"{EscapeCsv(failure.ReadRamAddresses)}," +
                           $"{EscapeCsv(failure.ErrorMessage)}");
        }
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
