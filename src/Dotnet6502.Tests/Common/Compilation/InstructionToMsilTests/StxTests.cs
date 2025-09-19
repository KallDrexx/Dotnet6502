using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.Compilation.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 STX (Store X Register) instruction
///
/// STX stores the X register value to memory and:
/// - Does NOT affect any processor status flags
/// - Simply copies the X register value to the specified memory location
/// </summary>
public class StxTests
{
    [Fact]
    public void STX_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x86);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x86, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions)
        {
            NesHal =
            {
                XRegister = 0x42,
                MemoryValues =
                {
                    [0x10] = 0x00 // Initial value
                }
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x10].ShouldBe((byte)0x42);
        testRunner.NesHal.XRegister.ShouldBe((byte)0x42); // X register unchanged

        // No flags should be affected
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_ZeroPage_Zero_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x86);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x86, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions)
        {
            NesHal =
            {
                XRegister = 0x00,
                MemoryValues =
                {
                    [0x20] = 0xFF // Initial value
                }
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x20].ShouldBe((byte)0x00);
        testRunner.NesHal.XRegister.ShouldBe((byte)0x00);

        // No flags should be affected even when storing zero
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_ZeroPage_Negative_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x86);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x86, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions)
        {
            NesHal =
            {
                XRegister = 0x80, // Negative value
                MemoryValues =
                {
                    [0x30] = 0x00 // Initial value
                }
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x30].ShouldBe((byte)0x80);
        testRunner.NesHal.XRegister.ShouldBe((byte)0x80);

        // No flags should be affected even when storing negative value
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_ZeroPageY_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x96);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x96, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions)
        {
            NesHal =
            {
                XRegister = 0x55,
                YRegister = 0x05,
                MemoryValues =
                {
                    [0x45] = 0x00 // 0x40 + 0x05 = 0x45
                }
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x45].ShouldBe((byte)0x55);
        testRunner.NesHal.XRegister.ShouldBe((byte)0x55);

        // No flags should be affected
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_ZeroPageY_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x96);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x96, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions)
        {
            NesHal =
            {
                XRegister = 0x77,
                YRegister = 0x02,
                MemoryValues =
                {
                    [0x01] = 0x00, // (0xFF + 0x02) & 0xFF = 0x01
                    [0x101] = 0x00 // Should NOT be written to
                }
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x01].ShouldBe((byte)0x77);
        testRunner.NesHal.MemoryValues[0x101].ShouldBe((byte)0x00); // Should remain unchanged
        testRunner.NesHal.XRegister.ShouldBe((byte)0x77);

        // No flags should be affected
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x8E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x8E, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions)
        {
            NesHal =
            {
                XRegister = 0x99,
                MemoryValues =
                {
                    [0x3000] = 0x00 // Initial value
                }
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x3000].ShouldBe((byte)0x99);
        testRunner.NesHal.XRegister.ShouldBe((byte)0x99);

        // No flags should be affected
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_Absolute_HighAddress()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x8E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x8E, 0x34, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions)
        {
            NesHal =
            {
                XRegister = 0xAB,
                MemoryValues =
                {
                    [0x1234] = 0x00 // Initial value
                }
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x1234].ShouldBe((byte)0xAB);
        testRunner.NesHal.XRegister.ShouldBe((byte)0xAB);

        // No flags should be affected
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_Preserves_All_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x86);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x86, 0x50],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions)
        {
            NesHal =
            {
                XRegister = 0xFF,
                Flags =
                {
                    // Set all flags that should be preserved
                    [CpuStatusFlags.Zero] = true,
                    [CpuStatusFlags.Negative] = true,
                    [CpuStatusFlags.Carry] = true,
                    [CpuStatusFlags.Overflow] = true,
                    [CpuStatusFlags.Decimal] = true,
                    [CpuStatusFlags.InterruptDisable] = true
                }
            }
        };

        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x50].ShouldBe((byte)0xFF);
        testRunner.NesHal.XRegister.ShouldBe((byte)0xFF);

        // All flags should remain exactly as they were
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
    }

    [Fact]
    public void STX_Overwrite_Memory()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x86);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x86, 0x60],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions)
        {
            NesHal =
            {
                XRegister = 0xAA,
                MemoryValues =
                {
                    [0x60] = 0x55 // Existing value to be overwritten
                }
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x60].ShouldBe((byte)0xAA);
        testRunner.NesHal.XRegister.ShouldBe((byte)0xAA);

        // No flags should be affected
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_Does_Not_Affect_A_Or_Y_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x86);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x86, 0x70],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions)
        {
            NesHal =
            {
                ARegister = 0x11,
                XRegister = 0x22,
                YRegister = 0x33,
                MemoryValues =
                {
                    [0x70] = 0x00
                }
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x70].ShouldBe((byte)0x22);
        testRunner.NesHal.ARegister.ShouldBe((byte)0x11); // Should remain unchanged
        testRunner.NesHal.XRegister.ShouldBe((byte)0x22); // Should remain unchanged
        testRunner.NesHal.YRegister.ShouldBe((byte)0x33); // Should remain unchanged

        // No flags should be affected
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_ZeroPageY_Maximum_Y_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x96);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x96, 0x00],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions)
        {
            NesHal =
            {
                XRegister = 0xCC,
                YRegister = 0xFF,
                MemoryValues =
                {
                    [0xFF] = 0x00 // 0x00 + 0xFF = 0xFF
                }
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0xFF].ShouldBe((byte)0xCC);
        testRunner.NesHal.XRegister.ShouldBe((byte)0xCC);

        // No flags should be affected
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_Maximum_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x86);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x86, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions)
        {
            NesHal =
            {
                XRegister = 0xFF, // Maximum value
                MemoryValues =
                {
                    [0x80] = 0x00 // Initial value
                }
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x80].ShouldBe((byte)0xFF);
        testRunner.NesHal.XRegister.ShouldBe((byte)0xFF);

        // No flags should be affected even when storing maximum value
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }
}