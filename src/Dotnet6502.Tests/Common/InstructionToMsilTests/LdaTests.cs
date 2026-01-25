using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 LDA (Load Accumulator) instruction
///
/// LDA loads a value from memory or immediate into the accumulator and affects:
/// - Zero flag: Set if result is 0
/// - Negative flag: Set if bit 7 is set
/// - Does NOT affect Carry or Overflow flags
/// </summary>
public class LdaTests
{
    [Fact]
    public void LDA_Immediate_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA9, 0x42],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_Immediate_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA9, 0x00],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xFF;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_Immediate_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA9, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x80);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_Immediate_HighValue()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA9, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA5);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA5, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.Memory.MemoryBlock[0x10] = 0x33;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x33);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_ZeroPage_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA5);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA5, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xFF;
        jit.Memory.MemoryBlock[0x20] = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB5);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB5, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.TestHal.XRegister = 0x05;
        jit.Memory.MemoryBlock[0x35] = 0x55; // 0x30 + 0x05
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x55);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB5);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB5, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.TestHal.XRegister = 0x02;
        jit.Memory.MemoryBlock[0x01] = 0x77; // (0xFF + 0x02) & 0xFF = 0x01
        jit.Memory.MemoryBlock[0x101] = 0x88; // Should NOT be accessed
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x77);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xAD);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xAD, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.Memory.MemoryBlock[0x3000] = 0x99;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x99);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBD);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBD, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.TestHal.XRegister = 0x0F;
        jit.Memory.MemoryBlock[0x200F] = 0x11;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x11);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_AbsoluteY_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB9, 0x00, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.TestHal.YRegister = 0x10;
        jit.Memory.MemoryBlock[0x4010] = 0x22;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x22);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_IndexedIndirect_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA1);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA1, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.TestHal.XRegister = 0x05;

        // Set up the address lookup: ($20 + X) = $25 contains the target address $3000
        jit.Memory.MemoryBlock[0x25] = 0x00; // Low byte of target address
        jit.Memory.MemoryBlock[0x26] = 0x30; // High byte of target address

        // Set the value at the target address
        jit.Memory.MemoryBlock[0x3000] = 0x42;

        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_IndexedIndirect_255()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA1);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA1, 0xFA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.TestHal.XRegister = 0x05;

        jit.Memory.MemoryBlock[0xFF] = 0x00; // Low byte of target address
        jit.Memory.MemoryBlock[0x00] = 0x30; // High byte of target address
        jit.Memory.MemoryBlock[0x3000] = 0x42;

        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_IndexedIndirect_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA1);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA1, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xFF;
        jit.TestHal.XRegister = 0x03;

        // Set up the address lookup: ($10 + X) = $13 contains the target address $2500
        jit.Memory.MemoryBlock[0x13] = 0x00; // Low byte of target address
        jit.Memory.MemoryBlock[0x14] = 0x25; // High byte of target address

        // Set the value at the target address to 0 to test zero flag
        jit.Memory.MemoryBlock[0x2500] = 0x00;

        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_IndexedIndirect_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA1);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA1, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.TestHal.XRegister = 0x08;

        // Set up the address lookup: ($30 + X) = $38 contains the target address $4000
        jit.Memory.MemoryBlock[0x38] = 0x00; // Low byte of target address
        jit.Memory.MemoryBlock[0x39] = 0x40; // High byte of target address

        // Set the value at the target address to 0x90 to test negative flag
        jit.Memory.MemoryBlock[0x4000] = 0x90;

        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x90);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_IndexedIndirect_ZeroPageWrapAround()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA1);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA1, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.TestHal.XRegister = 0x02;

        // Test zero page wraparound: ($FF + 0x02) wraps to $01,$02 (not $101,$102)
        jit.Memory.MemoryBlock[0x01] = 0x50; // Low byte of target address
        jit.Memory.MemoryBlock[0x02] = 0x15; // High byte of target address

        // Set the value at the target address
        jit.Memory.MemoryBlock[0x1550] = 0x77;

        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x77);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_IndirectIndexed_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB1);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB1, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.TestHal.YRegister = 0x10;

        // Set up the base address: $30 contains the base address $2000
        jit.Memory.MemoryBlock[0x30] = 0x00; // Low byte of base address
        jit.Memory.MemoryBlock[0x31] = 0x20; // High byte of base address

        // Set the value at the final address ($2000 + Y) = $2010
        jit.Memory.MemoryBlock[0x2010] = 0x84;

        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x84);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_IndirectIndexed_255()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB1);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB1, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.TestHal.YRegister = 0x10;

        // Set up the base address: $30 contains the base address $2000
        jit.Memory.MemoryBlock[0xFF] = 0x00; // Low byte of base address
        jit.Memory.MemoryBlock[0x00] = 0x20; // High byte of base address

        // Set the value at the final address ($2000 + Y) = $2010
        jit.Memory.MemoryBlock[0x2010] = 0x84;

        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x84);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_IndirectIndexed_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB1);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB1, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xFF;
        jit.TestHal.YRegister = 0x05;

        // Set up the base address: $40 contains the base address $3000
        jit.Memory.MemoryBlock[0x40] = 0x00; // Low byte of base address
        jit.Memory.MemoryBlock[0x41] = 0x30; // High byte of base address

        // Set the value at the final address ($3000 + Y) = $3005 to 0
        jit.Memory.MemoryBlock[0x3005] = 0x00;

        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_IndirectIndexed_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB1);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB1, 0x50],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.TestHal.YRegister = 0x20;

        // Set up the base address: $50 contains the base address $4000
        jit.Memory.MemoryBlock[0x50] = 0x00; // Low byte of base address
        jit.Memory.MemoryBlock[0x51] = 0x40; // High byte of base address

        // Set the value at the final address ($4000 + Y) = $4020 to 0xFF
        jit.Memory.MemoryBlock[0x4020] = 0xFF;

        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_IndirectIndexed_PageBoundary()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB1);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB1, 0x60],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.TestHal.YRegister = 0xFF;

        // Set up the base address: $60 contains the base address $20FF
        jit.Memory.MemoryBlock[0x60] = 0xFF; // Low byte of base address
        jit.Memory.MemoryBlock[0x61] = 0x20; // High byte of base address

        // Final address ($20FF + Y) = $21FE crosses page boundary
        jit.Memory.MemoryBlock[0x21FE] = 0x33;

        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x33);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDA_Preserves_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA9, 0x7F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;

        // Set some flags that should be preserved
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);

        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x7F);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();

        // These flags should be preserved
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeTrue();
    }
}