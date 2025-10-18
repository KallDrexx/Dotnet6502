This project allows the just-in-time compilation and execution of applications compiled 
to 6502 assembly.

<!-- TOC -->
* [How Does It Work?](#how-does-it-work)
  * [Disassembly](#disassembly)
  * [Conversion To IR](#conversion-to-ir)
  * [IR Analysis And Customization](#ir-analysis-and-customization)
  * [MSIL Generation](#msil-generation)
  * [Assembly Generation](#assembly-generation)
  * [Execution](#execution)
* [Creating An Emulator](#creating-an-emulator)
* [Example Implementations](#example-implementations)
  * [NES Emulator](#nes-emulator)
<!-- TOC -->

# How Does It Work?

The JIT process contains the following steps:

1. Disassemble and trace out the instructions
2. Convert the 6502 instructions into an intermediate representation
3. Performs any analysis or customizations on the IR instructions
4. Convert the intermediate representation into .net MSIL
5. Create a dynamic assembly with the generated MSIL
6. Execute the created method

## Disassembly

When the [JIT compiler](/src/Dotnet6502.Common/Compilation/JitCompiler.cs) is given the address of an 
instruction to run, it pulls all code eligible memory regions from the memory bus and passes them 
(and the address of the first instruction) to the decompiler.

[The decompiler](/NESDecompiler/NESDecompiler.Core/Decompilation/FunctionDecompiler.cs) starts disassembling 
and tracing the 6502 instructions starting from that address in the memory regions until all branches 
terminate in a loop or a function boundary. Any `RTS`, `RTI`, `BRK`, `JSR`, or indirect `JMP` instruction is
considered the end of a function. 

An invalid 6502 instruction is also considered the end of a function, as there are times when
an unconditional branch instruction is used instead of a `JMP` instruction to save bytes/cycles.

Once this is complete, we have the full set of disassembled instructions that comprise the provided function.

## Conversion To IR

There are 56 official instructions in the 6502 assembly instruction set. Some are simple while others
are complex. Many of them rely on a variety of access patterns depending on if they are fetching or
modifying registers, processor flags, or memory values. Attempting to write executable instructions
for each of these is complex, error-prone, hard to debug, and gives very little ability for 
optimizations and analysis.

Instead, it turns out that all 56 operations can be represented by combining [~12 smaller intermediate 
representation instructions](/src/Dotnet6502.Common/Compilation/Ir6502.cs). 

This step of the JIT process takes the 6502 disassembled instructions and [converts each one into one
or more IR instructions](/src/Dotnet6502.Common/Compilation/InstructionConverter.cs).

## IR Analysis And Customization

Now that we have a full set of IR instructions, we can perform some analysis on them and customize
them as needed.

This is done via a [`IJitCustomizer` interface](/src/Dotnet6502.Common/Compilation/IJitCustomizer.cs) that
allows different hardware emulation systems to add or remove instructions. There is a 
[standard JIT customizer](/src/Dotnet6502.Common/Compilation/StandardJitCustomizer.cs) which prepends
a debugging hook and a poll for interrupts prior to the instruction execution. 

The NES example further [adds a custom instruction to increment cycle counts](/src/Dotnet6502.Nes/NesJitCustomizer.cs).

## MSIL Generation

Once the final set of IR instructions are available, we can then 
[generate MSIL for each of them](/src/Dotnet6502.Common/Compilation/MsilGenerator.cs). 

## Assembly Generation

The generated MSIL is placed within its own 
[static method in a static type in its own dynamic assembly](/src/Dotnet6502.Common/Compilation/ExecutableMethodGenerator.cs).

The containing type is then compiled by the .net runtime and a delegate is created that we can then execute

## Execution

Now that we have a delegate containing the compiled code, we instruct the JIT to execute the delegate, 
thereby running the 6502 application. The delegate will run until it returns with an address to execute next, 
at which point the JIT will repeat the process for the returned address.

It assumes a second call to an address that's already been compiled is for the same function, and therefore
will re-use delegates that it has previously compiled

# Creating An Emulator

To emulate a 6502 based system:

1. Create [IMemoryDevice implementations](/src/Dotnet6502.Common/Hardware/IMemoryDevice.cs) devices for
    all memory mapped regions
   * Normal RAM areas can use the [basic ram implementation](/src/Dotnet6502.Common/Hardware/BasicRamMemoryDevice.cs).
2. Populate any required memory devices with the program ROM you are intended to execute.
   * Most 6502 applications tend to have the application code loaded towards the end of the memory range.
3. Instantiate a [MemoryBus](/src/Dotnet6502.Common/Hardware/MemoryBus.cs) and attach all memory devices to
    their respective addresses. 
   * This handles all memory reads and writes and maps them to the correct offset to the expected
     memory mapped device.
4. Instantiate a [Hardware Abstraction Layer instance](/src/Dotnet6502.Common/Hardware/Base6502Hal.cs).
   * This contains all CPU registers and passes memory read and write calls to the memory bus.
   * A custom implementation will be needed to ensure hardware interrupts are triggered.
   * It is a good idea for a custom implementation to take in a `CancellationToken`, so you can stop
     execution as needed during `PollForInterrupt`.
5. Create a [IJitCustomizer implementation](/src/Dotnet6502.Common/Compilation/IJitCustomizer.cs) if JIT
    customizations are required.
6. Instantiate a [JitCompiler instance](/src/Dotnet6502.Common/Compilation/JitCompiler.cs).
7. Call `JitCompiler.RunMethod()` with the address of the initial function to execute.
   * Most 6502 applications store this address in `$FFFC` and `$FFFD`.

The JIT will now run and execute the program, usually forever. You can call `RunMethod()` in a background thread.

You'll want to have some way to synchronize the 6502 code with the system somehow. Many 6502 devices have a display
that always runs at 60Hz, and so you can use the trigger of VBlank on the display emulator you create to pause
the 6502 code for 16ms.  This ensures only 1 frame of 6502 worth of assembly executes within a 16ms window.

The disassembly instructions do have cycle counts with them, so pure cycle counting + sleep is also a viable option.

# Example Implementations

## NES Emulator

The [Dotnet6502.Nes.Cli](/src/Dotnet6502.Nes.Cli) and [Dotnet6502.Nes](/src/Dotnet6502.Nes) projects contain
an implementation of the 6502 Just-In-Time compilation system to execute NES roms. Monogame is used for the
window and input handling mechanism.

To play a NES game, obtain the ROM you wish to play and run: 
`dotnet run --project src/Dotnet6502.Nes.Cli/Dotnet6502.Nes.Cli --rom <path-to-rom>`. A window should pop up.

The controls are:
* Arrow keys for directional input
* `Enter` - Start
* `Backspace` - Select
* `Z` - A button
* `X` - B button

Note that not all roms will work with the emulator. Custom memory mapping hardware has not been implemented, so
any games that are larger than 32KB will not map correctly.  Likewise, any game with more than 16KB of character
ROM will not work either.

A good example homebrew game is [Alter Ego](https://www.romhacking.net/homebrew/1/).