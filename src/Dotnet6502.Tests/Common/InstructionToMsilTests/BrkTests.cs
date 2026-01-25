using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

public class BrkTests
{
    [Fact]
    public void Brk_Invokes_Function_Call_To_Address_Pointed_To_By_Fffe()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x00);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x00], // BRK
        };

        var context = new InstructionConverter.Context(new Dictionary<ushort, string>(), []);
        var allInstructions = InstructionConverter.Convert(instruction, context);

        var jit = TestJitCompiler.Create();
        jit.Memory.MemoryBlock[0xFFFE] = 0x56;
        jit.Memory.MemoryBlock[0xFFFF] = 0x34;
        jit.AddMethod(0x1234, allInstructions);

        // Add a callable function at the irq address
        var callableInstruction = new Ir6502.Copy(
            new Ir6502.Constant(99),
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x4000), null, false));
        jit.AddMethod(0x3456, [callableInstruction]); // IRQ address

        jit.RunMethod(0x1234);

        // Verify the function was actually invoked
        jit.TestHal.ReadMemory(0x4000).ShouldBe((byte)99);
    }

    [Fact]
    public void Brk_Pushes_Next_Address_And_Processor_Flags_With_Unused_As_Ones_To_Stack()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x00);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x00], // BRK
            CPUAddress = 0x2345,
        };

        var context = new InstructionConverter.Context(new Dictionary<ushort, string>(), []);
        var allInstructions = InstructionConverter.Convert(instruction, context);

        var jit = TestJitCompiler.Create();
        jit.Memory.MemoryBlock[0xFFFE] = 0x56;
        jit.Memory.MemoryBlock[0xFFFF] = 0x34;
        jit.TestHal.ProcessorStatus = 0b11001111;
        jit.AddMethod(0x1234, allInstructions);

        // Add a callable function at the irq address
        var callableInstruction = new Ir6502.Copy(
            new Ir6502.Constant(99),
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x4000), null, false));
        jit.AddMethod(0x3456, [callableInstruction]); // IRQ address

        jit.RunMethod(0x1234);
        jit.TestHal.PopFromStack().ShouldBe((byte)0b11111111);
        jit.TestHal.PopFromStack().ShouldBe((byte)0x47); // low address byte
        jit.TestHal.PopFromStack().ShouldBe((byte)0x23); // high address byte
    }

    [Fact]
    public void Brk_Sets_Interrupt_Disable_To_True()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x00);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x00], // BRK
            CPUAddress = 0x2345,
        };

        var context = new InstructionConverter.Context(new Dictionary<ushort, string>(), []);
        var allInstructions = InstructionConverter.Convert(instruction, context);

        var jit = TestJitCompiler.Create();
        jit.Memory.MemoryBlock[0xFFFE] = 0x56;
        jit.Memory.MemoryBlock[0xFFFF] = 0x34;
        jit.TestHal.ProcessorStatus = 0;
        jit.AddMethod(0x1234, allInstructions);

        // Add a callable function at the irq address
        var callableInstruction = new Ir6502.Copy(
            new Ir6502.Constant(99),
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x4000), null, false));
        jit.AddMethod(0x3456, [callableInstruction]); // IRQ address

        jit.RunMethod(0x1234);
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBe(true);
    }

    [Fact]
    public void Brk_Sets_B_Disable_To_True_For_Value_Pushed_To_Stack()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x00);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x00], // BRK
            CPUAddress = 0x2345,
        };

        var context = new InstructionConverter.Context(new Dictionary<ushort, string>(), []);
        var allInstructions = InstructionConverter.Convert(instruction, context);

        var jit = TestJitCompiler.Create();
        jit.Memory.MemoryBlock[0xFFFE] = 0x56;
        jit.Memory.MemoryBlock[0xFFFF] = 0x34;
        jit.TestHal.ProcessorStatus = 0;
        jit.AddMethod(0x1234, allInstructions);

        // Add a callable function at the irq address
        var callableInstruction = new Ir6502.Copy(
            new Ir6502.Constant(99),
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x4000), null, false));
        jit.AddMethod(0x3456, [callableInstruction]); // IRQ address

        jit.RunMethod(0x1234);
        jit.TestHal.PopFromStack().ShouldBe((byte)0b00110000);
    }
}