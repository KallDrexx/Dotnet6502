using Dotnet6502.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common;

public class ExecutableMethodCacheTests
{
    [Fact]
    public void Can_Cache_Function()
    {
        var function = CreateTestFunction(0x1234);
        var method = CreateTestMethod();
        var cache = new ExecutableMethodCache();

        cache.AddExecutableMethod(method, function, []);
        var result = cache.GetMethodForAddress(0x1234);

        result.ShouldNotBeNull();
        result.ShouldBe(method);
    }

    [Fact]
    public void Reporting_Memory_Change_On_Entry_Address_Invalidates_Cached_Method()
    {
        var function = CreateTestFunction(0x1234, 0x2345);
        var method = CreateTestMethod();
        var cache = new ExecutableMethodCache();

        cache.AddExecutableMethod(method, function, []);
        cache.MemoryChanged(0x1234);
        var result = cache.GetMethodForAddress(0x1234);

        result.ShouldBeNull();
    }

    [Fact]
    public void Reporting_Memory_Change_On_Non_Entry_Address_Invalidates_Cached_Method()
    {
        var function = CreateTestFunction(0x1234, 0x2345);
        var method = CreateTestMethod();
        var cache = new ExecutableMethodCache();

        cache.AddExecutableMethod(method, function, []);
        cache.MemoryChanged(0x2345);
        var result = cache.GetMethodForAddress(0x1234);

        result.ShouldBeNull();
    }

    [Fact]
    public void Reporting_Memory_Change_On_Non_Entry_Address_Not_Invalidated_When_Excluded()
    {
        var function = CreateTestFunction(0x1234, 0x2345);
        var method = CreateTestMethod();
        var cache = new ExecutableMethodCache();

        cache.AddExecutableMethod(method, function, [0x2345]);
        cache.MemoryChanged(0x2345);
        var result = cache.GetMethodForAddress(0x1234);

        result.ShouldNotBeNull();
    }

    [Fact]
    public void Reporting_Memory_Change_On_Irrelevant_Page_Does_Not_Invalidates_Cached_Method()
    {
        var function = CreateTestFunction(0x1234, 0x2345);
        var method = CreateTestMethod();
        var cache = new ExecutableMethodCache();

        cache.AddExecutableMethod(method, function, []);
        cache.MemoryChanged(0x0135);
        var result = cache.GetMethodForAddress(0x1234);

        result.ShouldNotBeNull();
        result.ShouldBe(method);
    }

    [Fact]
    public void Cached_Methods_Not_Accessed_Get_Evicted_When_Max_Reached()
    {
        var cache = new ExecutableMethodCache();
        var testFunction = CreateTestFunction(0x1234);
        var method = CreateTestMethod();

        cache.AddExecutableMethod(method, testFunction, []);

        for (var x = 0; x < ExecutableMethodCache.MaxCachedMethodCount - 1; x++)
        {
            var address = 0x3344 + x;
            cache.AddExecutableMethod(CreateTestMethod(), CreateTestFunction((ushort)address), []);
        }

        cache.AddExecutableMethod(CreateTestMethod(), CreateTestFunction(0x5555), []);
        cache.GetMethodForAddress(0x1234).ShouldBeNull();
    }

    [Fact]
    public void Cached_Methods_Not_Evicted_If_Recently_Used_When_Max_Reached()
    {
        var cache = new ExecutableMethodCache();
        var testFunction = CreateTestFunction(0x1234);
        var method = CreateTestMethod();

        cache.AddExecutableMethod(method, testFunction, []);

        for (var x = 0; x < ExecutableMethodCache.MaxCachedMethodCount - 1; x++)
        {
            var address = 0x3344 + x;
            cache.AddExecutableMethod(CreateTestMethod(), CreateTestFunction((ushort)address), []);
        }

        cache.GetMethodForAddress(0x1234).ShouldNotBeNull();
        cache.AddExecutableMethod(CreateTestMethod(), CreateTestFunction(0x5555), []);
        cache.GetMethodForAddress(0x1234).ShouldNotBeNull();
    }

    private static DecompiledFunction CreateTestFunction(params ushort[] instructionAddresses)
    {
        if (instructionAddresses.Length == 0)
        {
            throw new ArgumentException("No instruction addresses provided");
        }

        if (instructionAddresses.Distinct().Count() != instructionAddresses.Length)
        {
            throw new ArgumentException("Duplicate instruction addresses provided");
        }

        var instructions = new List<DisassembledInstruction>();
        foreach (var address in instructionAddresses)
        {
            instructions.Add(new DisassembledInstruction
            {
                Info = InstructionSet.GetInstruction(0xEA),
                CPUAddress = address,
                Bytes = [0xEA]
            });
        }

        return new DecompiledFunction(instructionAddresses[0], instructions, new HashSet<ushort>());
    }

    private static ExecutableMethod CreateTestMethod()
    {
        return _ => 0;
    }
}