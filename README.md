# Dotnet6502

This project allows the just-in-time compilation and execution of applications compiled 
to 6502 assembly.

# How Does It Work?

## Components

## Process

The JIT process contains the following steps:

1. Disassemble and trace out the instructions
2. Convert the 6502 instructions into an intermediate representation
3. Performs any analysis or customizations on the IR instructions
4. Convert the intermediate representation into .net MSIL
5. Create a dynamic assembly with the generated MSIL
6. Execute the created method

### Disassembly

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

### Conversion To IR

There are 56 official instructions in the 6502 assembly instruction set. Some are simple while others
are complex. Many of them rely on a variety of access patterns depending on if they are fetching or
modifying registers, processor flags, or memory values. Attempting to write executable instructions
for each of these is complex, error-prone, hard to debug, and gives very little ability for 
optimizations and analysis.

Instead, it turns out that all 56 operations can be represented by combining [~12 smaller intermediate 
representation instructions](/src/Dotnet6502.Common/Compilation/Ir6502.cs). 

This step of the JIT process takes the 6502 disassembled instructions and [converts each one into one
or more IR instructions](/src/Dotnet6502.Common/Compilation/InstructionConverter.cs).

### IR Analysis And Customization

Now that we have a full set of IR instructions, we can perform some analysis on them and customize
them as needed.

This is done via a [`IJitCustomizer` interface](/src/Dotnet6502.Common/Compilation/IJitCustomizer.cs) that
allows different hardware emulation systems to add or remove instructions. There is a 
[standard JIT customizer](/src/Dotnet6502.Common/Compilation/StandardJitCustomizer.cs) which prepends
a debugging hook and a poll for interrupts prior to the instruction execution. 

The NES example further [adds a custom instruction to increment cycle counts](/src/Dotnet6502.Nes/NesJitCustomizer.cs).

### MSIL Generation

Once the final set of IR instructions are available, we can then 
[generate MSIL for each of them](/src/Dotnet6502.Common/Compilation/MsilGenerator.cs). 

### Assembly Generation

The generated MSIL is placed within its own 
[static method in a static type in its own dynamic assembly](/src/Dotnet6502.Common/Compilation/ExecutableMethodGenerator.cs).

The containing type is then compiled by the .net runtime and a delegate is created that we can then execute

### Execution

Now that we have a delegate containing the compiled code, we instruct the JIT to execute the delegate, 
thereby running the 6502 application. The delegate will run until it returns with an address to execute next, 
at which point the JIT will repeat the process for the returned address.

It assumes a second call to an address that's already been compiled is for the same function, and therefore
will re-use delegates that it has previously compiled