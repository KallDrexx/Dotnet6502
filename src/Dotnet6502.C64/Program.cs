using Dotnet6502.C64.Hardware;
using Dotnet6502.Common.Hardware;

var pla = new ProgrammableLogicArray();
var memoryBus = new MemoryBus();
memoryBus.Attach(pla, 0x0000);
pla.AttachToBus(memoryBus);

// fill in the rest with ram
memoryBus.Attach(new BasicRamMemoryDevice(0x7fff - 0x0002 + 1), 0x0002);
memoryBus.Attach(new BasicRamMemoryDevice(0xcfff - 0xc000 + 1), 0xc000);

// TODO: Add cartridge rom low swapping to this region
memoryBus.Attach(new BasicRamMemoryDevice(0x9fff - 0x8000 + 1), 0x8000);


