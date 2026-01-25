using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

public class StyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STY_ZeroPage_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x84);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x84, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x42;
        jit.Memory.MemoryBlock[0x10] = 0x00; // Initial value
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x10].ShouldBe((byte)0x42);
        jit.TestHal.YRegister.ShouldBe((byte)0x42); // Y register unchanged

        // No flags should be affected
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STY_ZeroPage_Zero_Value(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x84);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x84, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x00;
        jit.Memory.MemoryBlock[0x20] = 0xFF; // Initial value
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x20].ShouldBe((byte)0x00);
        jit.TestHal.YRegister.ShouldBe((byte)0x00);

        // No flags should be affected even when storing zero
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STY_ZeroPage_Negative_Value(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x84);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x84, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x80; // Negative value
        jit.Memory.MemoryBlock[0x30] = 0x00; // Initial value
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x30].ShouldBe((byte)0x80);
        jit.TestHal.YRegister.ShouldBe((byte)0x80);

        // No flags should be affected even when storing negative value
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STY_ZeroPageX_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x94);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x94, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x55;
        jit.TestHal.XRegister = 0x05;
        jit.Memory.MemoryBlock[0x45] = 0x00; // 0x40 + 0x05 = 0x45
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x45].ShouldBe((byte)0x55);
        jit.TestHal.YRegister.ShouldBe((byte)0x55);

        // No flags should be affected
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STY_ZeroPageX_Wraparound(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x94);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x94, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x77;
        jit.TestHal.XRegister = 0x02;
        jit.Memory.MemoryBlock[0x01] = 0x00; // (0xFF + 0x02) & 0xFF = 0x01
        jit.Memory.MemoryBlock[0x101] = 0x00; // Should NOT be written to
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x01].ShouldBe((byte)0x77);
        jit.Memory.MemoryBlock[0x101].ShouldBe((byte)0x00); // Should remain unchanged
        jit.TestHal.YRegister.ShouldBe((byte)0x77);

        // No flags should be affected
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STY_Absolute_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x8C);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x8C, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x99;
        jit.Memory.MemoryBlock[0x3000] = 0x00; // Initial value
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x3000].ShouldBe((byte)0x99);
        jit.TestHal.YRegister.ShouldBe((byte)0x99);

        // No flags should be affected
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STY_Absolute_HighAddress(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x8C);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x8C, 0x34, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0xAB;
        jit.Memory.MemoryBlock[0x1234] = 0x00; // Initial value
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x1234].ShouldBe((byte)0xAB);
        jit.TestHal.YRegister.ShouldBe((byte)0xAB);

        // No flags should be affected
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STY_Preserves_All_Flags(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x84);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x84, 0x50],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0xFF;

        // Set all flags that should be preserved
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);

        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x50].ShouldBe((byte)0xFF);
        jit.TestHal.YRegister.ShouldBe((byte)0xFF);

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
    public void STY_Overwrite_Memory(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x84);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x84, 0x60],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0xAA;
        jit.Memory.MemoryBlock[0x60] = 0x55; // Existing value to be overwritten
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x60].ShouldBe((byte)0xAA);
        jit.TestHal.YRegister.ShouldBe((byte)0xAA);

        // No flags should be affected
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STY_Does_Not_Affect_A_Or_X_Registers(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x84);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x84, 0x70],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x11;
        jit.TestHal.XRegister = 0x33;
        jit.TestHal.YRegister = 0x22;
        jit.Memory.MemoryBlock[0x70] = 0x00;
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x70].ShouldBe((byte)0x22);
        jit.TestHal.ARegister.ShouldBe((byte)0x11); // Should remain unchanged
        jit.TestHal.XRegister.ShouldBe((byte)0x33); // Should remain unchanged
        jit.TestHal.YRegister.ShouldBe((byte)0x22); // Should remain unchanged

        // No flags should be affected
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STY_ZeroPageX_Maximum_X_Value(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x94);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x94, 0x00],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0xCC;
        jit.TestHal.XRegister = 0xFF;
        jit.Memory.MemoryBlock[0xFF] = 0x00; // 0x00 + 0xFF = 0xFF
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0xFF].ShouldBe((byte)0xCC);
        jit.TestHal.YRegister.ShouldBe((byte)0xCC);

        // No flags should be affected
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STY_Maximum_Value(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x84);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x84, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0xFF; // Maximum value
        jit.Memory.MemoryBlock[0x80] = 0x00; // Initial value
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x80].ShouldBe((byte)0xFF);
        jit.TestHal.YRegister.ShouldBe((byte)0xFF);

        // No flags should be affected even when storing maximum value
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STY_ZeroPageX_Zero_Index(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x94);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x94, 0x90],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0xBB;
        jit.TestHal.XRegister = 0x00; // No offset
        jit.Memory.MemoryBlock[0x90] = 0x00; // 0x90 + 0x00
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x90].ShouldBe((byte)0xBB);
        jit.TestHal.YRegister.ShouldBe((byte)0xBB);

        // No flags should be affected
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STY_Redirects_To_Next_Instruction_On_Recompile(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x84);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x84, 0x10],
            CPUAddress = 0x1234
        };

        var instruction2 = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x84, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, InstructionConverter.Convert(instruction, context));
        jit.AddMethod(0x1234 + 2, InstructionConverter.Convert(instruction2, context));

        var count = 1;
        jit.TestHal.OnMemoryWritten = _ => (count--) > 0;
        jit.TestHal.YRegister = 0x42;
        jit.Memory.MemoryBlock[0x10] = 0x00; // Initial value
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x10].ShouldBe((byte)0x42);
        jit.Memory.MemoryBlock[0x12].ShouldBe((byte)0x42);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void STY_Does_Not_Redirect_When_Recompilation_Not_Needed(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x84);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x84, 0x10],
            CPUAddress = 0x1234
        };

        var instruction2 = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x84, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, InstructionConverter.Convert(instruction, context));
        jit.AddMethod(0x1234 + 2, InstructionConverter.Convert(instruction2, context));

        jit.TestHal.OnMemoryWritten = _ => false;
        jit.TestHal.YRegister = 0x42;
        jit.Memory.MemoryBlock[0x10] = 0x00; // Initial value
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x10].ShouldBe((byte)0x42);
        jit.Memory.MemoryBlock[0x12].ShouldBe((byte)0x0);
    }
}
