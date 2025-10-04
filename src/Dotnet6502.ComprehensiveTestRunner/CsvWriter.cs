namespace Dotnet6502.ComprehensiveTestRunner;

public static class CsvWriter
{
    public static void WriteFailures(string filePath, IEnumerable<TestFailure> failures)
    {
        using var writer = new StreamWriter(filePath);

        // Write header
        writer.WriteLine("Mnemonic,AddressingMode,HexBytes,InitialA,InitialX,InitialY,InitialP,InitialS,InitialRam," +
                        "ExpectedA,ExpectedX,ExpectedY,ExpectedP,ExpectedS,ExpectedRam," +
                        "ActualA,ActualX,ActualY,ActualP,ActualS,ActualRam," +
                        "ReadRamAddresses,WrittenRamAddresses,ExpectedCycles,ErrorMessage");

        // Write each failure
        foreach (var failure in failures)
        {
            writer.WriteLine($"{EscapeCsv(failure.Mnemonic)}," +
                           $"{EscapeCsv(failure.AddressingMode)}," +
                           $"{EscapeCsv(failure.HexBytes)}," +
                           $"{failure.InitialA} (0x{failure.InitialA:X2})," +
                           $"{failure.InitialX} (0x{failure.InitialX:X2})," +
                           $"{failure.InitialY} (0x{failure.InitialY:X2})," +
                           $"{failure.InitialP} (0x{failure.InitialP:X2})," +
                           $"{failure.InitialS} (0x{failure.InitialS:X2})," +
                           $"{EscapeCsv(failure.InitialRam)}," +
                           $"{failure.ExpectedA} (0x{failure.ExpectedA:X2})," +
                           $"{failure.ExpectedX} (0x{failure.ExpectedX:X2})," +
                           $"{failure.ExpectedY} (0x{failure.ExpectedY:X2})," +
                           $"{failure.ExpectedP} (0x{failure.ExpectedP:X2})," +
                           $"{failure.ExpectedS} (0x{failure.ExpectedS:X2})," +
                           $"{EscapeCsv(failure.ExpectedRam)}," +
                           $"{failure.ActualA} (0x{failure.ActualA:X2})," +
                           $"{failure.ActualX} (0x{failure.ActualX:X2})," +
                           $"{failure.ActualY} (0x{failure.ActualY:X2})," +
                           $"{failure.ActualP} (0x{failure.ActualP:X2})," +
                           $"{failure.ActualS} (0x{failure.ActualS:X2})," +
                           $"{EscapeCsv(failure.ActualRam)}," +
                           $"{EscapeCsv(failure.ReadRamAddresses)}," +
                           $"{EscapeCsv(failure.WrittenRamAddresses)}," +
                           $"{EscapeCsv(failure.ExpectedCycles)}," +
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
