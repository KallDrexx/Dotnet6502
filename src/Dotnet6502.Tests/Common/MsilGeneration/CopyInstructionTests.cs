using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
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

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)23);
    }

    [Fact]
    public void Can_Copy_Constant_To_XIndex()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Constant(42),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)42);
    }

    [Fact]
    public void Can_Copy_Constant_To_YIndex()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Constant(99),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)99);
    }

    [Fact]
    public void Can_Copy_Constant_To_Memory()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Constant(77),
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x1000), null, false));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x1000).ShouldBe((byte)77);
    }

    [Fact]
    public void Can_Copy_Constant_To_Variable()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Constant(88),
            new Ir6502.Variable(0));

        var instruction2 = new Ir6502.Copy(
            new Ir6502.Variable(0),
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x3434), null, false));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction, instruction2]);
        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x3434).ShouldBe((byte)88);
    }

    [Fact]
    public void Can_Copy_Constant_To_Flag()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Constant(1),
            new Ir6502.Flag(Ir6502.FlagName.Carry));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBe(true);
    }

    [Fact]
    public void Can_Copy_Constant_To_StackPointer()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Constant(0xFD),
            new Ir6502.StackPointer());

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.StackPointer.ShouldBe((byte)0xFD);
    }

    [Fact]
    public void Can_Copy_Accumulator_To_XIndex()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = TestJitCompiler.Create();
        jit.TestHal.ARegister = 55;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)55);
    }

    [Fact]
    public void Can_Copy_Accumulator_To_YIndex()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var jit = TestJitCompiler.Create();
        jit.TestHal.ARegister = 66;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)66);
    }

    [Fact]
    public void Can_Copy_XIndex_To_Accumulator()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.XIndex),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.TestHal.XRegister = 33;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)33);
    }

    [Fact]
    public void Can_Copy_XIndex_To_YIndex()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.XIndex),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var jit = TestJitCompiler.Create();
        jit.TestHal.XRegister = 44;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)44);
    }

    [Fact]
    public void Can_Copy_YIndex_To_Accumulator()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.YIndex),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.TestHal.YRegister = 77;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)77);
    }

    [Fact]
    public void Can_Copy_YIndex_To_XIndex()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.YIndex),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = TestJitCompiler.Create();
        jit.TestHal.YRegister = 88;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)88);
    }

    [Fact]
    public void Can_Copy_Register_To_Itself()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.TestHal.ARegister = 123;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)123);
    }

    [Fact]
    public void Can_Copy_Memory_To_Register()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x2000), null, false),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.WriteMemory(0x2000, 156);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)156);
    }

    [Fact]
    public void Can_Copy_Memory_With_Offset_To_Register_Via_16_Bit_Address()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x00FF), Ir6502.RegisterName.XIndex, false),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.XRegister = 10;

        jit.TestHal.WriteMemory(0x0109, 156);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)156);
    }

    [Fact]
    public void Can_Copy_Memory_With_Offset_To_Register_Via_8_Bit_Address()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x00FF), Ir6502.RegisterName.XIndex, true),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.XRegister = 10;

        jit.TestHal.WriteMemory(0x0009, 156);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)156);
    }

    [Fact]
    public void Can_Copy_Register_To_Memory()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.XIndex),
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x3000), null, false));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.XRegister = 199;
        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x3000).ShouldBe((byte)199);
    }

    [Fact]
    public void Can_Copy_Register_To_Memory_With_Zero_Page_Y_Indexing()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.XIndex),
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x0000), Ir6502.RegisterName.YIndex, true));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.XRegister = 199;
        jit.TestHal.YRegister = 0xFF;
        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x00FF).ShouldBe((byte)199);
    }

    [Fact]
    public void Can_Copy_Memory_With_Register_Offset()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x4000), Ir6502.RegisterName.XIndex, false),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.XRegister = 5;
        jit.TestHal.WriteMemory(0x4005, 211);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)211);
    }

    [Fact]
    public void Can_Copy_To_Memory_With_Register_Offset_As_16_Bit_Address()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.YIndex),
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x00FF), Ir6502.RegisterName.XIndex, false));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.XRegister = 10;
        jit.TestHal.YRegister = 222;
        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x0109).ShouldBe((byte)222);
    }

    [Fact]
    public void Can_Copy_To_Memory_With_Register_Offset_As_8_Bit_Address()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.YIndex),
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x00FF), Ir6502.RegisterName.XIndex, true));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.XRegister = 10;
        jit.TestHal.YRegister = 222;
        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x0009).ShouldBe((byte)222);
    }

    [Fact]
    public void Can_Copy_Memory_To_Memory()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x6000), null, false),
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x7000), null, false));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.WriteMemory(0x6000, 133);
        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x7000).ShouldBe((byte)133);
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

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [copyToVar, copyFromVar]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)144);
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

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [copyToVar, copyFromVar]);
        jit.TestHal.XRegister = 177;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)177);
    }

    [Fact]
    public void Can_Copy_Variable_To_Memory()
    {
        var copyToVar = new Ir6502.Copy(
            new Ir6502.Constant(255),
            new Ir6502.Variable(2));
        var copyFromVar = new Ir6502.Copy(
            new Ir6502.Variable(2),
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x8000), null, false));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [copyToVar, copyFromVar]);
        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x8000).ShouldBe((byte)255);
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

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [copyToVar1, copyVar1ToVar2, copyVar2ToReg]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)99);
    }

    [Fact]
    public void Can_Copy_Flag_To_Register()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Flag(Ir6502.FlagName.Zero),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Copy_Register_To_Flag()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.Flag(Ir6502.FlagName.Negative));

        var jit = TestJitCompiler.Create();
        jit.TestHal.ARegister = 1;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBe(true);
    }

    [Fact]
    public void Can_Copy_Zero_To_Flag()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Constant(0),
            new Ir6502.Flag(Ir6502.FlagName.Overflow));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBe(false);
    }

    [Fact]
    public void Can_Copy_Flag_To_Memory()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Flag(Ir6502.FlagName.Carry),
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x9000), null, false));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x9000).ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Copy_Memory_To_Flag()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0xA000), null, false),
            new Ir6502.Flag(Ir6502.FlagName.InterruptDisable));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.WriteMemory(0xA000, 1);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBe(true);
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

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [copyCarryToVar, copyVarToZero]);
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBe(true);
    }

    [Fact]
    public void Can_Copy_AllFlags_To_Register()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.AllFlags(),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.TestHal.ProcessorStatus = 0x85;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xA5);
    }

    [Fact]
    public void Can_Copy_Register_To_AllFlags()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.AllFlags());

        var jit = TestJitCompiler.Create();
        jit.TestHal.ARegister = 0x85;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ProcessorStatus.ShouldBe((byte)0xA5);
    }

    [Fact]
    public void Can_Copy_AllFlags_To_Memory()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.AllFlags(),
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0xB000), null, false));

        var jit = TestJitCompiler.Create();
        jit.TestHal.ProcessorStatus = 0x85;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0xB000).ShouldBe((byte)0xA5);
    }

    [Fact]
    public void Can_Copy_Memory_To_AllFlags()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0xC000), null, false),
            new Ir6502.AllFlags());

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.WriteMemory(0xC000, 0x85);
        jit.RunMethod(0x1234);

        jit.TestHal.ProcessorStatus.ShouldBe((byte)0xA5);
    }

    [Fact]
    public void Can_Copy_StackPointer_To_Register()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.StackPointer(),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = TestJitCompiler.Create();
        jit.TestHal.StackPointer = 0xF8;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0xF8);
    }

    [Fact]
    public void Can_Copy_Register_To_StackPointer()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.YIndex),
            new Ir6502.StackPointer());

        var jit = TestJitCompiler.Create();
        jit.TestHal.YRegister = 0xE5;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.StackPointer.ShouldBe((byte)0xE5);
    }

    [Fact]
    public void Can_Copy_StackPointer_To_Memory()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.StackPointer(),
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0xD000), null, false));

        var jit = TestJitCompiler.Create();
        jit.TestHal.StackPointer = 0xCC;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0xD000).ShouldBe((byte)0xCC);
    }

    [Fact]
    public void Can_Copy_Memory_To_StackPointer()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0xE000), null, false),
            new Ir6502.StackPointer());

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.WriteMemory(0xE000, 0xAA);
        jit.RunMethod(0x1234);

        jit.TestHal.StackPointer.ShouldBe((byte)0xAA);
    }

    [Fact]
    public void Can_Copy_IndexedIndirect_To_Register()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.IndirectMemory(0x20, true, false), // Indexed-Indirect: (zp,X)
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.TestHal.XRegister = 5;
        jit.AddMethod(0x1234, [instruction]);

        // Set up the address lookup: ($20 + X) = $25 contains the target address $3000
        jit.TestHal.WriteMemory(0x25, 0x00); // Low byte of target address
        jit.TestHal.WriteMemory(0x26, 0x30); // High byte of target address

        // Set the value at the target address
        jit.TestHal.WriteMemory(0x3000, 0x42);

        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x42);
    }

    [Fact]
    public void Can_Copy_IndirectIndexed_To_Register()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.IndirectMemory(0x30, false, true), // Indirect-Indexed: (zp),Y
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.TestHal.YRegister = 10;
        jit.AddMethod(0x1234, [instruction]);

        // Set up the base address: $30 contains the base address $2000
        jit.TestHal.WriteMemory(0x30, 0x00); // Low byte of base address
        jit.TestHal.WriteMemory(0x31, 0x20); // High byte of base address

        // Set the value at the final address ($2000 + Y) = $200A
        jit.TestHal.WriteMemory(0x200A, 0x84);

        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x84);
    }

    [Fact]
    public void Can_Copy_Register_To_IndexedIndirect()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.IndirectMemory(0x20, true, false)); // Indexed-Indirect: (zp,X)

        var jit = TestJitCompiler.Create();
        jit.TestHal.XRegister = 5;
        jit.TestHal.ARegister = 0x42;
        jit.AddMethod(0x1234, [instruction]);

        // Set up the address lookup: ($20 + X) = $25 contains the target address $3000
        jit.TestHal.WriteMemory(0x25, 0x00); // Low byte of target address
        jit.TestHal.WriteMemory(0x26, 0x30); // High byte of target address

        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x3000).ShouldBe((byte)0x42);
    }

    [Fact]
    public void Can_Copy_Register_To_IndirectIndexed()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.IndirectMemory(0x30, false, true)); // Indirect-Indexed: (zp),Y

        var jit = TestJitCompiler.Create();
        jit.TestHal.YRegister = 10;
        jit.TestHal.ARegister = 0x84;
        jit.AddMethod(0x1234, [instruction]);

        // Set up the base address: $30 contains the base address $2000
        jit.TestHal.WriteMemory(0x30, 0x00); // Low byte of base address
        jit.TestHal.WriteMemory(0x31, 0x20); // High byte of base address

        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x200A).ShouldBe((byte)0x84);
    }

    [Fact]
    public void Can_Copy_Memory_With_Dynamic_Location_To_Register()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Memory(new Ir6502.DynamicMemoryLocation(0x2000), null, false),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.WriteMemory(0x2000, 0xCD);
        jit.TestHal.WriteMemory(0x2001, 0xAB);
        jit.TestHal.WriteMemory(0xABCD, 156);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)156);
    }

    [Fact]
    public void Can_Copy_Memory_With_Dynamic_Location_With_Single_Byte_Address_To_Register()
    {
        var instruction = new Ir6502.Copy(
            new Ir6502.Memory(new Ir6502.DynamicMemoryLocation(0x2000, true), null, false),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.WriteMemory(0x2000, 0xCD);
        jit.TestHal.WriteMemory(0x2001, 0xAB);
        jit.TestHal.WriteMemory(0x00CD, 156);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)156);
    }
}