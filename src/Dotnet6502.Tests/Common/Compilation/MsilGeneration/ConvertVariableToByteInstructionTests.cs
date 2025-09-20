using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Shouldly;

namespace Dotnet6502.Tests.Common.Compilation.MsilGeneration;

public class ConvertVariableToByteInstructionTests
{
    [Fact]
    public void Can_Convert_Variable_With_Small_Value()
    {
        var setupVar = new Ir6502.Copy(
            new Ir6502.Constant(42),
            new Ir6502.Variable(0));
        var convertInstruction = new Ir6502.ConvertVariableToByte(new Ir6502.Variable(0));
        var readVar = new Ir6502.Copy(
            new Ir6502.Variable(0),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([setupVar, convertInstruction, readVar]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)42);
    }

    [Fact]
    public void Can_Convert_Variable_With_Zero_Value()
    {
        var setupVar = new Ir6502.Copy(
            new Ir6502.Constant(0),
            new Ir6502.Variable(1));
        var convertInstruction = new Ir6502.ConvertVariableToByte(new Ir6502.Variable(1));
        var readVar = new Ir6502.Copy(
            new Ir6502.Variable(1),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([setupVar, convertInstruction, readVar]);
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)0);
    }

    [Fact]
    public void Can_Convert_Variable_With_Max_Byte_Value()
    {
        var setupVar = new Ir6502.Copy(
            new Ir6502.Constant(255),
            new Ir6502.Variable(2));
        var convertInstruction = new Ir6502.ConvertVariableToByte(new Ir6502.Variable(2));
        var readVar = new Ir6502.Copy(
            new Ir6502.Variable(2),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([setupVar, convertInstruction, readVar]);
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)255);
    }

    [Fact]
    public void Can_Convert_Variable_Then_Use_In_Memory_Operation()
    {
        var setupVar = new Ir6502.Copy(
            new Ir6502.Constant(123),
            new Ir6502.Variable(0));
        var convertInstruction = new Ir6502.ConvertVariableToByte(new Ir6502.Variable(0));
        var writeToMemory = new Ir6502.Copy(
            new Ir6502.Variable(0),
            new Ir6502.Memory(0x2000, null, false));

        var testRunner = new InstructionTestRunner([setupVar, convertInstruction, writeToMemory]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ReadMemory(0x2000).ShouldBe((byte)123);
    }

    [Fact]
    public void Can_Convert_Variable_Then_Copy_To_Flag()
    {
        var setupVar = new Ir6502.Copy(
            new Ir6502.Constant(1),
            new Ir6502.Variable(0));
        var convertInstruction = new Ir6502.ConvertVariableToByte(new Ir6502.Variable(0));
        var copyToFlag = new Ir6502.Copy(
            new Ir6502.Variable(0),
            new Ir6502.Flag(Ir6502.FlagName.Carry));

        var testRunner = new InstructionTestRunner([setupVar, convertInstruction, copyToFlag]);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Carry).ShouldBe(true);
    }

    [Fact]
    public void Can_Convert_Variable_Then_Copy_To_StackPointer()
    {
        var setupVar = new Ir6502.Copy(
            new Ir6502.Constant(0xF8),
            new Ir6502.Variable(3));
        var convertInstruction = new Ir6502.ConvertVariableToByte(new Ir6502.Variable(3));
        var copyToStackPointer = new Ir6502.Copy(
            new Ir6502.Variable(3),
            new Ir6502.StackPointer());

        var testRunner = new InstructionTestRunner([setupVar, convertInstruction, copyToStackPointer]);
        testRunner.RunTestMethod();

        testRunner.NesHal.StackPointer.ShouldBe((byte)0xF8);
    }

    [Fact]
    public void Can_Convert_Variable_With_Value_Greater_Than_255_To_Byte()
    {
        var setupVar1 = new Ir6502.Copy(
            new Ir6502.Constant(200),
            new Ir6502.Variable(0));
        var setupVar2 = new Ir6502.Copy(
            new Ir6502.Constant(150),
            new Ir6502.Variable(1));
        var addOperation = new Ir6502.Binary(
            Ir6502.BinaryOperator.Add,
            new Ir6502.Variable(0),
            new Ir6502.Variable(1),
            new Ir6502.Variable(2));
        var convertInstruction = new Ir6502.ConvertVariableToByte(new Ir6502.Variable(2));
        var readResult = new Ir6502.Copy(
            new Ir6502.Variable(2),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([setupVar1, setupVar2, addOperation, convertInstruction, readResult]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)94);
    }
}