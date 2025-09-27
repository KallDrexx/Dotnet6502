using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 AND (Logical AND) instruction
///
/// AND performs a bitwise AND between the accumulator and a memory value or immediate value.
/// The result is stored in the accumulator.
///
/// Flags affected:
/// - Zero: Set if result is 0
/// - Negative: Set if bit 7 of result is set
/// - Carry and Overflow flags are NOT affected
/// </summary>
public class AndTests
{
    [Fact]
    public void AND_Immediate_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x29);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x29, 0x0F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions)
        {
            TestHal =
            {
                ARegister = 0xFF,
                Flags =
                {
                    [CpuStatusFlags.Carry] = true,
                    [CpuStatusFlags.Overflow] = true
                }
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x0F);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
    }

    [Fact]
    public void AND_Immediate_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x29);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x29, 0x0F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions)
        {
            TestHal =
            {
                ARegister = 0xF0,
                Flags =
                {
                    [CpuStatusFlags.Carry] = true
                }
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
    }

    [Fact]
    public void AND_Immediate_Negative_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x29);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x29, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions)
        {
            TestHal =
            {
                ARegister = 0xFF,
                Flags =
                {
                    [CpuStatusFlags.Overflow] = true
                }
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x80);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
    }

    [Fact]
    public void AND_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x25);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x25, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions)
        {
            TestHal =
            {
                ARegister = 0x55,
                MemoryValues =
                {
                    [0x10] = 0xAA
                }
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void AND_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x35);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x35, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions)
        {
            TestHal =
            {
                ARegister = 0x33,
                XRegister = 0x05,
                MemoryValues =
                {
                    [0x35] = 0x0F
                }
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x03);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void AND_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x35);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x35, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0xFF;
        testRunner.TestHal.XRegister = 0x02;
        testRunner.TestHal.MemoryValues[0x01] = 0x7F;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x7F);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void AND_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x2D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x2D, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0xC3;
        testRunner.TestHal.MemoryValues[0x3000] = 0x81;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x81);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void AND_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x3D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x3D, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x42;
        testRunner.TestHal.XRegister = 0x0F;
        testRunner.TestHal.MemoryValues[0x200F] = 0x3C;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void AND_AbsoluteY_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x39);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x39, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0xF0;
        testRunner.TestHal.YRegister = 0x01;
        testRunner.TestHal.MemoryValues[0x5000] = 0xE1;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0xE0);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void AND_Pattern_Mask()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x29);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x29, 0x55],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0xFF;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x55);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void AND_All_Addressing_Modes()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x29);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x29, 0x7F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x80;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }
}