using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 LDY (Load Y Register) instruction
///
/// LDY loads a value from memory or immediate into the Y register and affects:
/// - Zero flag: Set if result is 0
/// - Negative flag: Set if bit 7 is set
/// - Does NOT affect Carry or Overflow flags
/// </summary>
public class LdyTests
{
    [Fact]
    public void LDY_Immediate_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA0, 0x42],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x42);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDY_Immediate_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA0, 0x00],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0xFF;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDY_Immediate_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA0, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x80);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDY_Immediate_HighValue()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA0, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDY_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA4);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA4, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x00;
        jit.MemoryMap.MemoryBlock[0x10] = 0x33;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x33);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDY_ZeroPage_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA4);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA4, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0xFF;
        jit.MemoryMap.MemoryBlock[0x20] = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDY_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB4);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB4, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x00;
        jit.TestHal.XRegister = 0x05;
        jit.MemoryMap.MemoryBlock[0x35] = 0x55; // 0x30 + 0x05
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x55);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDY_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB4);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB4, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x00;
        jit.TestHal.XRegister = 0x02;
        jit.MemoryMap.MemoryBlock[0x01] = 0x77; // (0xFF + 0x02) & 0xFF = 0x01
        jit.MemoryMap.MemoryBlock[0x101] = 0x88; // Should NOT be accessed
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x77);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDY_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xAC);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xAC, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x00;
        jit.MemoryMap.MemoryBlock[0x3000] = 0x99;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x99);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDY_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBC);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBC, 0x00, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x00;
        jit.TestHal.XRegister = 0x10;
        jit.MemoryMap.MemoryBlock[0x4010] = 0x22;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x22);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDY_AbsoluteX_PageCrossing()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBC);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBC, 0xFF, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x00;
        jit.TestHal.XRegister = 0x02;
        jit.MemoryMap.MemoryBlock[0x2101] = 0x88; // 0x20FF + 0x02 = 0x2101
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x88);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDY_Preserves_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA0, 0x7F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x00;

        // Set some flags that should be preserved
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);

        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x7F);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();

        // These flags should be preserved
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeTrue();
    }

    [Fact]
    public void LDY_Does_Not_Affect_A_Or_X_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA0, 0x42],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x11;
        jit.TestHal.XRegister = 0x22;
        jit.TestHal.YRegister = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x42);
        jit.TestHal.ARegister.ShouldBe((byte)0x11); // Should remain unchanged
        jit.TestHal.XRegister.ShouldBe((byte)0x22); // Should remain unchanged
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDY_ZeroPageX_Negative_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB4);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB4, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x00;
        jit.TestHal.XRegister = 0x08;
        jit.MemoryMap.MemoryBlock[0x48] = 0xC0; // 0x40 + 0x08, negative value
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0xC0);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDY_Absolute_Zero_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xAC);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xAC, 0x34, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0xFF;
        jit.MemoryMap.MemoryBlock[0x1234] = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void LDY_AbsoluteX_Zero_Index()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBC);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBC, 0x50, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x00;
        jit.TestHal.XRegister = 0x00; // No offset
        jit.MemoryMap.MemoryBlock[0x3050] = 0x66; // 0x3050 + 0x00
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x66);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }
}