namespace Dotnet6502.ComprehensiveTestRunner;

public class TestCase
{
    public required string Name { get; set; }
    public required ValueSet Initial { get; set; }
    public required ValueSet Final { get; set; }

    public class ValueSet
    {
        public ushort Pc { get; set; }
        public byte S { get; set; }
        public byte A { get; set; }
        public byte X { get; set; }
        public byte Y { get; set; }
        public byte P { get; set; }
        public ushort[][] Ram { get; set; } = [];
    }
}
