using Dotnet6502.Common;
using Shouldly;

namespace Dotnet6502.Tests.Common.MsilGeneration;

public class CopyInstructionTests
{
    [Fact]
    public void Can_Copy_Constant_To_Accumulator()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Constant(23),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)23);
    }

    [Fact]
    public void Can_Copy_Constant_To_XIndex()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Constant(42),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)42);
    }

    [Fact]
    public void Can_Copy_Constant_To_YIndex()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Constant(99),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.TestHal.YRegister.ShouldBe((byte)99);
    }

    [Fact]
    public void Can_Copy_Constant_To_Memory()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Constant(77),
            new Ir6502.Memory(0x1000, null, false));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.TestHal.ReadMemory(0x1000).ShouldBe((byte)77);
    }

    [Fact]
    public void Can_Copy_Constant_To_Variable()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Constant(88),
            new Ir6502.Variable(0));

        var instruction2 = new Ir6502.Copy(
            new Ir6502.Variable(0),
            new Ir6502.Memory(0x3434, null, false));

        var testRunner = new InstructionTestRunner([instruction, instruction2]);
        testRunner.RunTestMethod();

        testRunner.TestHal.ReadMemory(0x3434).ShouldBe((byte)88);
    }

    [Fact]
    public void Can_Copy_Constant_To_Flag()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Constant(1),
            new Ir6502.Flag(Ir6502.FlagName.Carry));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBe(true);
    }

    [Fact]
    public void Can_Copy_Constant_To_StackPointer()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Constant(0xFD),
            new Ir6502.StackPointer());

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.TestHal.StackPointer.ShouldBe((byte)0xFD);
    }

    [Fact]
    public void Can_Copy_Accumulator_To_XIndex()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                ARegister = 55
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)55);
    }

    [Fact]
    public void Can_Copy_Accumulator_To_YIndex()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                ARegister = 66
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.YRegister.ShouldBe((byte)66);
    }

    [Fact]
    public void Can_Copy_XIndex_To_Accumulator()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.XIndex),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                XRegister = 33
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)33);
    }

    [Fact]
    public void Can_Copy_XIndex_To_YIndex()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.XIndex),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                XRegister = 44
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.YRegister.ShouldBe((byte)44);
    }

    [Fact]
    public void Can_Copy_YIndex_To_Accumulator()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.YIndex),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                YRegister = 77
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)77);
    }

    [Fact]
    public void Can_Copy_YIndex_To_XIndex()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.YIndex),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                YRegister = 88
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)88);
    }

    [Fact]
    public void Can_Copy_Register_To_Itself()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                ARegister = 123
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)123);
    }

    [Fact]
    public void Can_Copy_Memory_To_Register()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Memory(0x2000, null, false),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.WriteMemory(0x2000, 156);
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)156);
    }

    [Fact]
    public void Can_Copy_Memory_With_Offset_To_Register_Via_16_Bit_Address()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Memory(0x00FF, Ir6502.RegisterName.XIndex, false),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                XRegister = 10,
            }
        };

        testRunner.TestHal.WriteMemory(0x0109, 156);
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)156);
    }

    [Fact]
    public void Can_Copy_Memory_With_Offset_To_Register_Via_8_Bit_Address()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Memory(0x00FF, Ir6502.RegisterName.XIndex, true),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                XRegister = 10,
            }
        };

        testRunner.TestHal.WriteMemory(0x0009, 156);
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)156);
    }

    [Fact]
    public void Can_Copy_Register_To_Memory()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.XIndex),
            new Ir6502.Memory(0x3000, null, false));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                XRegister = 199
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.ReadMemory(0x3000).ShouldBe((byte)199);
    }

    [Fact]
    public void Can_Copy_Register_To_Memory_With_Zero_Page_Y_Indexing()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.XIndex),
            new Ir6502.Memory(0x0000, Ir6502.RegisterName.YIndex, true));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                XRegister = 199,
                YRegister = 0xFF,
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.ReadMemory(0x00FF).ShouldBe((byte)199);
    }

    [Fact]
    public void Can_Copy_Memory_With_Register_Offset()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Memory(0x4000, Ir6502.RegisterName.XIndex, false),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                XRegister = 5
            }
        };
        testRunner.TestHal.WriteMemory(0x4005, 211);
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)211);
    }

    [Fact]
    public void Can_Copy_To_Memory_With_Register_Offset_As_16_Bit_Address()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.YIndex),
            new Ir6502.Memory(0x00FF, Ir6502.RegisterName.XIndex, false));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                XRegister = 10,
                YRegister = 222
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.ReadMemory(0x0109).ShouldBe((byte)222);
    }

    [Fact]
    public void Can_Copy_To_Memory_With_Register_Offset_As_8_Bit_Address()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.YIndex),
            new Ir6502.Memory(0x00FF, Ir6502.RegisterName.XIndex, true));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                XRegister = 10,
                YRegister = 222
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.ReadMemory(0x0009).ShouldBe((byte)222);
    }

    [Fact]
    public void Can_Copy_Memory_To_Memory()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Memory(0x6000, null, false),
            new Ir6502.Memory(0x7000, null, false));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.WriteMemory(0x6000, 133);
        testRunner.RunTestMethod();

        testRunner.TestHal.ReadMemory(0x7000).ShouldBe((byte)133);
    }

    [Fact]
    public void Can_Copy_Variable_To_Register()
    {
        var copyToVar = new Ir6502.Copy(
            new Ir6502.Constant(144),
            new Ir6502.Variable(0));
        var copyFromVar = new Ir6502.Copy(
            new Ir6502.Variable(0),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([copyToVar, copyFromVar]);
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)144);
    }

    [Fact]
    public void Can_Copy_Register_To_Variable()
    {
        var copyToVar = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.XIndex),
            new Ir6502.Variable(1));
        var copyFromVar = new Ir6502.Copy(
            new Ir6502.Variable(1),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([copyToVar, copyFromVar])
        {
            TestHal =
            {
                XRegister = 177
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.YRegister.ShouldBe((byte)177);
    }

    [Fact]
    public void Can_Copy_Variable_To_Memory()
    {
        var copyToVar = new Ir6502.Copy(
            new Ir6502.Constant(255),
            new Ir6502.Variable(2));
        var copyFromVar = new Ir6502.Copy(
            new Ir6502.Variable(2),
            new Ir6502.Memory(0x8000, null, false));

        var testRunner = new InstructionTestRunner([copyToVar, copyFromVar]);
        testRunner.RunTestMethod();

        testRunner.TestHal.ReadMemory(0x8000).ShouldBe((byte)255);
    }

    [Fact]
    public void Can_Copy_Variable_To_Variable()
    {
        var copyToVar1 = new Ir6502.Copy(
            new Ir6502.Constant(99),
            new Ir6502.Variable(0));
        var copyVar1ToVar2 = new Ir6502.Copy(
            new Ir6502.Variable(0),
            new Ir6502.Variable(1));
        var copyVar2ToReg = new Ir6502.Copy(
            new Ir6502.Variable(1),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([copyToVar1, copyVar1ToVar2, copyVar2ToReg]);
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)99);
    }

    [Fact]
    public void Can_Copy_Flag_To_Register()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Flag(Ir6502.FlagName.Zero),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Copy_Register_To_Flag()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.Flag(Ir6502.FlagName.Negative));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                ARegister = 1
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBe(true);
    }

    [Fact]
    public void Can_Copy_Zero_To_Flag()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Constant(0),
            new Ir6502.Flag(Ir6502.FlagName.Overflow));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBe(false);
    }

    [Fact]
    public void Can_Copy_Flag_To_Memory()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Flag(Ir6502.FlagName.Carry),
            new Ir6502.Memory(0x9000, null, false));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        testRunner.RunTestMethod();

        testRunner.TestHal.ReadMemory(0x9000).ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Copy_Memory_To_Flag()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Memory(0xA000, null, false),
            new Ir6502.Flag(Ir6502.FlagName.InterruptDisable));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.WriteMemory(0xA000, 1);
        testRunner.RunTestMethod();

        testRunner.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBe(true);
    }

    [Fact]
    public void Can_Copy_Flag_To_Flag()
    {
        var copyCarryToVar = new Ir6502.Copy(
            new Ir6502.Flag(Ir6502.FlagName.Carry),
            new Ir6502.Variable(0));
        var copyVarToZero = new Ir6502.Copy(
            new Ir6502.Variable(0),
            new Ir6502.Flag(Ir6502.FlagName.Zero));

        var testRunner = new InstructionTestRunner([copyCarryToVar, copyVarToZero]);
        testRunner.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        testRunner.RunTestMethod();

        testRunner.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBe(true);
    }

    [Fact]
    public void Can_Copy_AllFlags_To_Register()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.AllFlags(),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                ProcessorStatus = 0xC3
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0xC3);
    }

    [Fact]
    public void Can_Copy_Register_To_AllFlags()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.AllFlags());

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                ARegister = 0x81
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.ProcessorStatus.ShouldBe((byte)0x81);
    }

    [Fact]
    public void Can_Copy_AllFlags_To_Memory()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.AllFlags(),
            new Ir6502.Memory(0xB000, null, false));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                ProcessorStatus = 0x5A
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.ReadMemory(0xB000).ShouldBe((byte)0x5A);
    }

    [Fact]
    public void Can_Copy_Memory_To_AllFlags()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Memory(0xC000, null, false),
            new Ir6502.AllFlags());

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.WriteMemory(0xC000, 0x3C);
        testRunner.RunTestMethod();

        testRunner.TestHal.ProcessorStatus.ShouldBe((byte)0x3C);
    }

    [Fact]
    public void Can_Copy_StackPointer_To_Register()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.StackPointer(),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                StackPointer = 0xF8
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0xF8);
    }

    [Fact]
    public void Can_Copy_Register_To_StackPointer()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.YIndex),
            new Ir6502.StackPointer());

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                YRegister = 0xE5
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.StackPointer.ShouldBe((byte)0xE5);
    }

    [Fact]
    public void Can_Copy_StackPointer_To_Memory()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.StackPointer(),
            new Ir6502.Memory(0xD000, null, false));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                StackPointer = 0xCC
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.ReadMemory(0xD000).ShouldBe((byte)0xCC);
    }

    [Fact]
    public void Can_Copy_Memory_To_StackPointer()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Memory(0xE000, null, false),
            new Ir6502.StackPointer());

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.WriteMemory(0xE000, 0xAA);
        testRunner.RunTestMethod();

        testRunner.TestHal.StackPointer.ShouldBe((byte)0xAA);
    }

    [Fact]
    public void Can_Copy_IndexedIndirect_To_Register()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.IndirectMemory(0x20, false), // Indexed-Indirect: (zp,X)
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal = { XRegister = 5 }
        };

        // Set up the address lookup: ($20 + X) = $25 contains the target address $3000
        testRunner.TestHal.WriteMemory(0x25, 0x00); // Low byte of target address
        testRunner.TestHal.WriteMemory(0x26, 0x30); // High byte of target address

        // Set the value at the target address
        testRunner.TestHal.WriteMemory(0x3000, 0x42);

        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x42);
    }

    [Fact]
    public void Can_Copy_IndirectIndexed_To_Register()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.IndirectMemory(0x30, true), // Indirect-Indexed: (zp),Y
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal = { YRegister = 10 }
        };

        // Set up the base address: $30 contains the base address $2000
        testRunner.TestHal.WriteMemory(0x30, 0x00); // Low byte of base address
        testRunner.TestHal.WriteMemory(0x31, 0x20); // High byte of base address

        // Set the value at the final address ($2000 + Y) = $200A
        testRunner.TestHal.WriteMemory(0x200A, 0x84);

        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x84);
    }

    [Fact]
    public void Can_Copy_Register_To_IndexedIndirect()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.IndirectMemory(0x20, false)); // Indexed-Indirect: (zp,X)

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal = {
                XRegister = 5,
                ARegister = 0x42
            }
        };

        // Set up the address lookup: ($20 + X) = $25 contains the target address $3000
        testRunner.TestHal.WriteMemory(0x25, 0x00); // Low byte of target address
        testRunner.TestHal.WriteMemory(0x26, 0x30); // High byte of target address

        testRunner.RunTestMethod();

        testRunner.TestHal.ReadMemory(0x3000).ShouldBe((byte)0x42);
    }

    [Fact]
    public void Can_Copy_Register_To_IndirectIndexed()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.IndirectMemory(0x30, true)); // Indirect-Indexed: (zp),Y

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal = {
                YRegister = 10,
                ARegister = 0x84
            }
        };

        // Set up the base address: $30 contains the base address $2000
        testRunner.TestHal.WriteMemory(0x30, 0x00); // Low byte of base address
        testRunner.TestHal.WriteMemory(0x31, 0x20); // High byte of base address

        testRunner.RunTestMethod();

        testRunner.TestHal.ReadMemory(0x200A).ShouldBe((byte)0x84);
    }
}