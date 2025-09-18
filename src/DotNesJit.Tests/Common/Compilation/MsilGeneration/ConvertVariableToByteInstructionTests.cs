using DotNesJit.Common;
using DotNesJit.Common.Compilation;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.MsilGeneration;

public class ConvertVariableToByteInstructionTests
{
    [Fact]
    public void Can_Convert_Variable_With_Small_Value()
    {
        var setupVar = new NesIr.Copy(
            new NesIr.Constant(42),
            new NesIr.Variable(0));
        var convertInstruction = new NesIr.ConvertVariableToByte(new NesIr.Variable(0));
        var readVar = new NesIr.Copy(
            new NesIr.Variable(0),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([setupVar, convertInstruction, readVar]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)42);
    }

    [Fact]
    public void Can_Convert_Variable_With_Zero_Value()
    {
        var setupVar = new NesIr.Copy(
            new NesIr.Constant(0),
            new NesIr.Variable(1));
        var convertInstruction = new NesIr.ConvertVariableToByte(new NesIr.Variable(1));
        var readVar = new NesIr.Copy(
            new NesIr.Variable(1),
            new NesIr.Register(NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([setupVar, convertInstruction, readVar]);
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)0);
    }

    [Fact]
    public void Can_Convert_Variable_With_Max_Byte_Value()
    {
        var setupVar = new NesIr.Copy(
            new NesIr.Constant(255),
            new NesIr.Variable(2));
        var convertInstruction = new NesIr.ConvertVariableToByte(new NesIr.Variable(2));
        var readVar = new NesIr.Copy(
            new NesIr.Variable(2),
            new NesIr.Register(NesIr.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([setupVar, convertInstruction, readVar]);
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)255);
    }

    [Fact]
    public void Can_Convert_Variable_Then_Use_In_Memory_Operation()
    {
        var setupVar = new NesIr.Copy(
            new NesIr.Constant(123),
            new NesIr.Variable(0));
        var convertInstruction = new NesIr.ConvertVariableToByte(new NesIr.Variable(0));
        var writeToMemory = new NesIr.Copy(
            new NesIr.Variable(0),
            new NesIr.Memory(0x2000, null, false));

        var testRunner = new InstructionTestRunner([setupVar, convertInstruction, writeToMemory]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ReadMemory(0x2000).ShouldBe((byte)123);
    }

    [Fact]
    public void Can_Convert_Variable_Then_Copy_To_Flag()
    {
        var setupVar = new NesIr.Copy(
            new NesIr.Constant(1),
            new NesIr.Variable(0));
        var convertInstruction = new NesIr.ConvertVariableToByte(new NesIr.Variable(0));
        var copyToFlag = new NesIr.Copy(
            new NesIr.Variable(0),
            new NesIr.Flag(NesIr.FlagName.Carry));

        var testRunner = new InstructionTestRunner([setupVar, convertInstruction, copyToFlag]);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Carry).ShouldBe(true);
    }

    [Fact]
    public void Can_Convert_Variable_Then_Copy_To_StackPointer()
    {
        var setupVar = new NesIr.Copy(
            new NesIr.Constant(0xF8),
            new NesIr.Variable(3));
        var convertInstruction = new NesIr.ConvertVariableToByte(new NesIr.Variable(3));
        var copyToStackPointer = new NesIr.Copy(
            new NesIr.Variable(3),
            new NesIr.StackPointer());

        var testRunner = new InstructionTestRunner([setupVar, convertInstruction, copyToStackPointer]);
        testRunner.RunTestMethod();

        testRunner.NesHal.StackPointer.ShouldBe((byte)0xF8);
    }

    [Fact]
    public void Can_Convert_Variable_With_Value_Greater_Than_255_To_Byte()
    {
        var setupVar1 = new NesIr.Copy(
            new NesIr.Constant(200),
            new NesIr.Variable(0));
        var setupVar2 = new NesIr.Copy(
            new NesIr.Constant(150),
            new NesIr.Variable(1));
        var addOperation = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Variable(0),
            new NesIr.Variable(1),
            new NesIr.Variable(2));
        var convertInstruction = new NesIr.ConvertVariableToByte(new NesIr.Variable(2));
        var readResult = new NesIr.Copy(
            new NesIr.Variable(2),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([setupVar1, setupVar2, addOperation, convertInstruction, readResult]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)94);
    }
}