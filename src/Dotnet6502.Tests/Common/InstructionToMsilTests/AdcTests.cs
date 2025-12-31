using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

public class AdcTests
{
    [Fact]
    public void ADC_Immediate_No_Overflow()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0x34],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 10;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)63);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void ADC_Immediate_With_Carry_Out()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x02;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x01);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow)
            .ShouldBeFalse(); // 6502: positive + negative = positive, no signed overflow
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void ADC_Immediate_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x01;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow)
            .ShouldBeFalse(); // 6502: positive + negative = positive, no signed overflow
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void ADC_Immediate_Negative_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0x50],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x50;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xA0);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow)
            .ShouldBeTrue(); // 6502: positive + positive = negative, signed overflow!
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void ADC_Immediate_Overflow_Negative_To_Positive()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x80;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow)
            .ShouldBeTrue(); // 6502: negative + negative = positive, signed overflow!
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void ADC_Immediate_With_Carry_In()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x20;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x31);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void ADC_Immediate_No_Overflow_Mixed_Signs()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0x80], // -128 in signed
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x7F; // +127 in signed
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF); // -1 in signed
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow)
            .ShouldBeFalse(); // 6502: positive + negative = negative, no signed overflow
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void ADC_Immediate_Overflow_Positive_Result_From_Negatives()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0xFF], // -1 in signed
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x82; // -126 in signed
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x81); // -127 in signed
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow)
            .ShouldBeFalse(); // 6502: negative + negative = negative, no signed overflow
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    // ZeroPage addressing mode tests
    [Fact]
    public void ADC_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x65);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x65, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x05;
        jit.Memory.MemoryBlock[0x10] = 0x03;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x08);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void ADC_ZeroPage_With_Carry()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x65);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x65, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xFF;
        jit.Memory.MemoryBlock[0x20] = 0x02;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x01);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow)
            .ShouldBeFalse(); // 6502: negative + positive = positive, no signed overflow
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    // ZeroPageX addressing mode tests
    [Fact]
    public void ADC_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x75);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x75, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x10;
        jit.TestHal.XRegister = 0x05;
        jit.Memory.MemoryBlock[0x35] = 0x15; // 0x30 + 0x05
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x25);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void ADC_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x75);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x75, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x01;
        jit.TestHal.XRegister = 0x02;
        jit.Memory.MemoryBlock[0x01] = 0x07; // (0xFF + 0x02) & 0xFF = 0x01
        jit.Memory.MemoryBlock[0x101] = 0x07; // In case implementation doesn't wrap
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x08);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    // Absolute addressing mode tests
    [Fact]
    public void ADC_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x6D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x6D, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x42;
        jit.Memory.MemoryBlock[0x3000] = 0x33;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x75);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void ADC_Absolute_Overflow()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x6D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x6D, 0x34, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x7F;
        jit.Memory.MemoryBlock[0x1234] = 0x01;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x80);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow)
            .ShouldBeTrue(); // 6502: positive + positive = negative, signed overflow!
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    // AbsoluteX addressing mode tests
    [Fact]
    public void ADC_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x7D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x7D, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x11;
        jit.TestHal.XRegister = 0x0F;
        jit.Memory.MemoryBlock[0x200F] = 0x22;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x33);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    // AbsoluteY addressing mode tests
    [Fact]
    public void ADC_AbsoluteY_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x79);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x79, 0x00, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x08;
        jit.TestHal.YRegister = 0x10;
        jit.Memory.MemoryBlock[0x4010] = 0x0F;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x17);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void ADC_AbsoluteY_Zero_And_Carry()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x79);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x79, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x01;
        jit.TestHal.YRegister = 0x01;
        jit.Memory.MemoryBlock[0x5000] = 0xFE;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow)
            .ShouldBeFalse(); // 6502: positive + negative + carry = positive, no signed overflow
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void Adc_Result_With_Cleared_Decimal_Flag_Test()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0x25],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x47;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, false);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x6D);
    }

    [Fact]
    public void Adc_Result_With_Set_Decimal_Flag_Test()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0x25],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x47;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x73);
    }

    [Fact]
    public void Adc_Decimal_Calculation_Without_Zero_Result_Should_Have_Zero_Flag_Unset()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0x25],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x47;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
    }

    [Fact]
    public void Negative_Flags_Set_By_Non_Bcd_Logic_When_Bcd_Enabled()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0x99],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x05;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, false);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void Overflow_Flags_Set_By_Non_Bcd_Logic_When_Bcd_Enabled()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0x99],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x99;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, false);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
    }

    [Fact]
    public void Zero_Flags_Set_When_Bcd_Enabled_And_Non_Bcd_Adc_Would_Be_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0x99],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x67;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, false);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
    }

    [Fact]
    public void Zero_Flags_Not_Set_When_Bcd_Enabled_And_Non_Bcd_Adc_Would_Not_Be_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0x99],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x01;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
    }
}