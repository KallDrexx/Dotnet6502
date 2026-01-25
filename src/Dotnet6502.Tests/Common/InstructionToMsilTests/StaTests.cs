using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

public class StaTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STA_ZeroPage_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x85);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x85, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x42;
        jit.Memory.MemoryBlock[0x10] = 0x00; // Initial value
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x10].ShouldBe((byte)0x42);
        jit.TestHal.ARegister.ShouldBe((byte)0x42); // Accumulator unchanged

        // No flags should be affected
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STA_ZeroPage_Zero_Value(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x85);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x85, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.Memory.MemoryBlock[0x20] = 0xFF; // Initial value
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x20].ShouldBe((byte)0x00);
        jit.TestHal.ARegister.ShouldBe((byte)0x00);

        // No flags should be affected even when storing zero
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STA_ZeroPage_Negative_Value(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x85);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x85, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x80; // Negative value
        jit.Memory.MemoryBlock[0x30] = 0x00; // Initial value
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x30].ShouldBe((byte)0x80);
        jit.TestHal.ARegister.ShouldBe((byte)0x80);

        // No flags should be affected even when storing negative value
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STA_ZeroPageX_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x95);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x95, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x55;
        jit.TestHal.XRegister = 0x05;
        jit.Memory.MemoryBlock[0x45] = 0x00; // 0x40 + 0x05 = 0x45
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x45].ShouldBe((byte)0x55);
        jit.TestHal.ARegister.ShouldBe((byte)0x55);

        // No flags should be affected
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STA_ZeroPageX_Wraparound(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x95);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x95, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x77;
        jit.TestHal.XRegister = 0x02;
        jit.Memory.MemoryBlock[0x01] = 0x00; // (0xFF + 0x02) & 0xFF = 0x01
        jit.Memory.MemoryBlock[0x101] = 0x00; // Should NOT be written to
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x01].ShouldBe((byte)0x77);
        jit.Memory.MemoryBlock[0x101].ShouldBe((byte)0x00); // Should remain unchanged
        jit.TestHal.ARegister.ShouldBe((byte)0x77);

        // No flags should be affected
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STA_Absolute_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x8D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x8D, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x99;
        jit.Memory.MemoryBlock[0x3000] = 0x00; // Initial value
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x3000].ShouldBe((byte)0x99);
        jit.TestHal.ARegister.ShouldBe((byte)0x99);

        // No flags should be affected
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STA_AbsoluteX_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x9D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x9D, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x11;
        jit.TestHal.XRegister = 0x0F;
        jit.Memory.MemoryBlock[0x200F] = 0x00; // 0x2000 + 0x0F = 0x200F
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x200F].ShouldBe((byte)0x11);
        jit.TestHal.ARegister.ShouldBe((byte)0x11);

        // No flags should be affected
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STA_AbsoluteY_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x99);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x99, 0x00, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x22;
        jit.TestHal.YRegister = 0x10;
        jit.Memory.MemoryBlock[0x4010] = 0x00; // 0x4000 + 0x10 = 0x4010
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x4010].ShouldBe((byte)0x22);
        jit.TestHal.ARegister.ShouldBe((byte)0x22);

        // No flags should be affected
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STA_Preserves_All_Flags(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x85);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x85, 0x50],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xFF;

        // Set all flags that should be preserved
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);

        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x50].ShouldBe((byte)0xFF);
        jit.TestHal.ARegister.ShouldBe((byte)0xFF);

        // All flags should remain exactly as they were
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STA_Overwrite_Memory(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x85);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x85, 0x60],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xAA;
        jit.Memory.MemoryBlock[0x60] = 0x55; // Existing value to be overwritten
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x60].ShouldBe((byte)0xAA);
        jit.TestHal.ARegister.ShouldBe((byte)0xAA);

        // No flags should be affected
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STA_IndexedIndirect_Test(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x81);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x81, 0x51],
        };

        var context = new InstructionConverter.Context(new Dictionary<ushort, string>());
        var irInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, irInstructions);
        jit.TestHal.ARegister = 0x11;
        jit.TestHal.XRegister = 0xAE;
        jit.Memory.MemoryBlock[0xFF] = 0x74;
        jit.Memory.MemoryBlock[0x00] = 0xBB;
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0xBB74].ShouldBe((byte)0x11);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STA_Redirects_To_Next_Address_On_Recompile(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x85);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x85, 0x10],
            CPUAddress = 0x1234,
        };

        var instruction2 = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x85, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, InstructionConverter.Convert(instruction, context));
        jit.AddMethod(0x1234 + 2, InstructionConverter.Convert(instruction2, context));

        var count = 1;
        jit.TestHal.OnMemoryWritten = _ => (count--) > 0;
        jit.TestHal.ARegister = 0x42;
        jit.Memory.MemoryBlock[0x10] = 0x00; // Initial value
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x10].ShouldBe((byte)0x42);
        jit.Memory.MemoryBlock[0x12].ShouldBe((byte)0x42);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STA_Does_Not_Redirect_When_Recompilation_Not_Needed(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x85);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x85, 0x10],
            CPUAddress = 0x1234,
        };

        var instruction2 = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x85, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, InstructionConverter.Convert(instruction, context));
        jit.AddMethod(0x1234 + 2, InstructionConverter.Convert(instruction2, context));

        jit.TestHal.OnMemoryWritten = _ => false;
        jit.TestHal.ARegister = 0x42;
        jit.Memory.MemoryBlock[0x10] = 0x00; // Initial value
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x10].ShouldBe((byte)0x42);
        jit.Memory.MemoryBlock[0x12].ShouldBe((byte)0x0);
    }
}
