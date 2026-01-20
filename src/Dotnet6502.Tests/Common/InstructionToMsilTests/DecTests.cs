using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

public class DecTests
{
    [Fact]
    public void DEC_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC6, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.Memory.MemoryBlock[0x10] = 0x05;
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x10].ShouldBe((byte)0x04);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void DEC_ZeroPage_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC6, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.Memory.MemoryBlock[0x20] = 0x01;
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x20].ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void DEC_ZeroPage_Wraparound_To_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC6, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.Memory.MemoryBlock[0x30] = 0x00; // 0x00 - 1 = 0xFF
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x30].ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void DEC_ZeroPage_Negative_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC6, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.Memory.MemoryBlock[0x40] = 0x81; // 0x81 - 1 = 0x80 (negative)
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x40].ShouldBe((byte)0x80);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void DEC_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD6, 0x50],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x05;
        jit.Memory.MemoryBlock[0x55] = 0x10; // 0x50 + 0x05
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x55].ShouldBe((byte)0x0F);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void DEC_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD6, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x02;
        jit.Memory.MemoryBlock[0x01] = 0x42; // (0xFF + 0x02) & 0xFF = 0x01
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x01].ShouldBe((byte)0x41);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void DEC_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xCE);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xCE, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.Memory.MemoryBlock[0x3000] = 0x99;
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x3000].ShouldBe((byte)0x98);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue(); // 0x98 has bit 7 set
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void DEC_Absolute_Positive_To_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xCE);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xCE, 0x34, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.Memory.MemoryBlock[0x1234] = 0x01;
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x1234].ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void DEC_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xDE);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xDE, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x0F;
        jit.Memory.MemoryBlock[0x200F] = 0x33;
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x200F].ShouldBe((byte)0x32);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void DEC_AbsoluteX_Zero_To_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xDE);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xDE, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x01;
        jit.Memory.MemoryBlock[0x5000] = 0x00; // 0x00 - 1 = 0xFF
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x5000].ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue(); // 0xFF has bit 7 set
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void DEC_Redirects_When_Recompile_Requested()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC6);
        var instruction1 = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC6, 0x10],
            CPUAddress = 0x1234,
        };

        var instruction2 = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC6, 0x11],
        };
        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, InstructionConverter.Convert(instruction1, context));
        jit.AddMethod(0x1234 + 2, InstructionConverter.Convert(instruction2, context));

        var count = 1;
        jit.TestHal.OnMemoryWritten = _ => (count--) > 0;
        jit.Memory.MemoryBlock[0x10] = 0x05;
        jit.Memory.MemoryBlock[0x11] = 0x08;
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x10].ShouldBe((byte)0x04);
        jit.Memory.MemoryBlock[0x11].ShouldBe((byte)0x07);
    }
}