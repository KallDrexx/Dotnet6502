using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 SBC (Subtract with Carry) instruction
///
/// IMPORTANT: These tests expect CORRECT 6502 overflow flag behavior.
/// Correct 6502 behavior: Overflow = (result^A) & (result^~M) & 0x80 != 0
///
/// 6502 SBC Formula: A = A - M - (1 - C)
/// 6502 SBC Rules:
/// - Carry flag is SET (1) when no borrow occurs (result >= 0)
/// - Carry flag is CLEARED (0) when borrow occurs (result < 0)
/// - Overflow occurs when subtracting opposite-sign numbers produces wrong-sign result
/// - Positive - Negative = Negative → Overflow
/// - Negative - Positive = Positive → Overflow
/// - Positive - Positive → Never overflow (same signs)
/// - Negative - Negative → Never overflow (same signs)
/// </summary>
public class SbcTests
{
    [Fact]
    public void SBC_Immediate_No_Borrow()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE9, 0x05],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x10;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true; // No borrow
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x0B); // 0x10 - 0x05 - 0 = 0x0B
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue(); // No borrow
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void SBC_Immediate_With_Borrow()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE9, 0x05],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x10;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false; // Borrow
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x0A); // 0x10 - 0x05 - 1 = 0x0A
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue(); // No borrow in result
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void SBC_Immediate_Result_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE9, 0x05],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x05;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true; // No borrow
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x00); // 0x05 - 0x05 - 0 = 0x00
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue(); // No borrow
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void SBC_Immediate_Borrow_From_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE9, 0x01],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true; // No initial borrow
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0xFF); // 0x00 - 0x01 - 0 = 0xFF (borrow)
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse(); // Borrow occurred
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse(); // positive - positive = negative, no overflow in 6502
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void SBC_Immediate_Negative_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE9, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x10;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true; // No initial borrow
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0xF0); // 0x10 - 0x20 - 0 = 0xF0 (borrow)
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse(); // Borrow occurred
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void SBC_Immediate_Overflow_Positive_Minus_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE9, 0x80], // -128 in signed
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x7F; // +127 in signed
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true; // No initial borrow
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0xFF); // 0x7F - 0x80 - 0 = 0xFF (-1 in signed)
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse(); // Borrow occurred
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue(); // 6502: positive - negative = negative, signed overflow!
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void SBC_Immediate_Overflow_Negative_Minus_Positive()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE9, 0x01], // +1 in signed
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x80; // -128 in signed
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true; // No initial borrow
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x7F); // 0x80 - 0x01 - 0 = 0x7F (+127 in signed)
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue(); // No borrow
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue(); // 6502: negative - positive = positive, signed overflow!
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void SBC_Immediate_No_Overflow_Same_Signs_Positive()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE9, 0x10], // +16 in signed
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x50; // +80 in signed
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true; // No initial borrow
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x40); // 0x50 - 0x10 - 0 = 0x40 (+64 in signed)
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue(); // No borrow
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse(); // 6502: positive - positive = positive, no signed overflow
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void SBC_Immediate_No_Overflow_Same_Signs_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE9, 0x90], // -112 in signed
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0xA0; // -96 in signed
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true; // No initial borrow
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x10); // 0xA0 - 0x90 - 0 = 0x10 (+16 in signed)
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue(); // No borrow
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse(); // 6502: negative - negative = positive, no signed overflow
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void SBC_Immediate_With_Previous_Borrow()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE9, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x20;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false; // Previous borrow
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x0F); // 0x20 - 0x10 - 1 = 0x0F
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue(); // No borrow in result
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    // ZeroPage addressing mode tests
    [Fact]
    public void SBC_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE5);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE5, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x20;
        testRunner.TestHal.MemoryValues[0x10] = 0x08;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x18); // 0x20 - 0x08 - 0 = 0x18
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void SBC_ZeroPage_With_Borrow()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE5);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE5, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x05;
        testRunner.TestHal.MemoryValues[0x20] = 0x10;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0xF5); // 0x05 - 0x10 - 0 = 0xF5 (borrow)
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse(); // Borrow occurred
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    // ZeroPageX addressing mode tests
    [Fact]
    public void SBC_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xF5);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xF5, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x50;
        testRunner.TestHal.XRegister = 0x05;
        testRunner.TestHal.MemoryValues[0x35] = 0x25; // 0x30 + 0x05
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x2B); // 0x50 - 0x25 - 0 = 0x2B
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void SBC_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xF5);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xF5, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x10;
        testRunner.TestHal.XRegister = 0x02;
        testRunner.TestHal.MemoryValues[0x01] = 0x05; // (0xFF + 0x02) & 0xFF = 0x01
        testRunner.TestHal.MemoryValues[0x101] = 0x05; // In case implementation doesn't wrap
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x0B); // 0x10 - 0x05 - 0 = 0x0B
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    // Absolute addressing mode tests
    [Fact]
    public void SBC_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xED);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xED, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x40;
        testRunner.TestHal.MemoryValues[0x3000] = 0x20;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x20); // 0x40 - 0x20 - 0 = 0x20
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void SBC_Absolute_Overflow()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xED);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xED, 0x34, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x80; // -128 in signed
        testRunner.TestHal.MemoryValues[0x1234] = 0x7F; // +127 in signed
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x01); // 0x80 - 0x7F - 0 = 0x01
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue(); // 6502: negative - positive = positive, signed overflow!
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    // AbsoluteX addressing mode tests
    [Fact]
    public void SBC_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xFD);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xFD, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x30;
        testRunner.TestHal.XRegister = 0x0F;
        testRunner.TestHal.MemoryValues[0x200F] = 0x18;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x18); // 0x30 - 0x18 - 0 = 0x18
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    // AbsoluteY addressing mode tests
    [Fact]
    public void SBC_AbsoluteY_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xF9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xF9, 0x00, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x25;
        testRunner.TestHal.YRegister = 0x10;
        testRunner.TestHal.MemoryValues[0x4010] = 0x12;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x13); // 0x25 - 0x12 - 0 = 0x13
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void SBC_AbsoluteY_Zero_Result_With_Borrow()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xF9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xF9, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x01;
        testRunner.TestHal.YRegister = 0x01;
        testRunner.TestHal.MemoryValues[0x5000] = 0x01;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false; // Previous borrow
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0xFF); // 0x01 - 0x01 - 1 = 0xFF (borrow)
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse(); // Borrow occurred
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }
}