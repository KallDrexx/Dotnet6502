using Dotnet6502.C64.Media;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Patches;

public class DiskImageReadPatch : Patch
{
    private readonly D64Image _image;

    public override ushort FunctionEntryAddress => 0xFFD5;

    public DiskImageReadPatch(D64Image image)
    {
        _image = image;
    }

    protected override int NativeFunction(Base6502Hal hal)
    {
        var device = hal.ReadMemory(0xBA);

        if (device != 8)
        {
            // Not requesting disk image, so fall through to kernel implementation
            return -1;
        }

        // Get the filename
        var nameLength = hal.ReadMemory(0xB7);
        var nameAddress = (hal.ReadMemory(0xBC) << 8) | hal.ReadMemory(0xBB);
        var petsciiFilename = new byte[nameLength];

        for (var x = 0; x < nameLength; x++)
        {
            petsciiFilename[x] = hal.ReadMemory((ushort)(nameAddress + x));
        }

        var entries = _image.ListFiles();
        D64DirectoryEntry? foundEntry = null;
        foreach (var entry in entries)
        {
            if (entry.PetsciiName.Length != petsciiFilename.Length)
            {
                continue;
            }

            for (var x = 0; x < petsciiFilename.Length; x++)
            {
                if (entry.PetsciiName[x] != petsciiFilename[x])
                {
                    continue;
                }
            }

            foundEntry = entry;
            break;
        }

        if (foundEntry == null)
        {
            hal.WriteMemory(0x90, 0x04);  // file not found status
            hal.SetFlag(CpuStatusFlags.Carry, true); // carry = error

            return SimulateRts(hal);
        }

        var content = _image.ReadFile(foundEntry.AsciiName);
        ushort loadAddress;
        if (hal.ReadMemory(0xB9) == 0)
        {
            // 0 here means load to BASIC start instead
            loadAddress = (ushort)((hal.ReadMemory(0x2c) << 8) | hal.ReadMemory(0x2b));
        }
        else
        {
            loadAddress = (ushort)((content[1] << 8) | content[0]);
        }

        // Copy the file to memory
        for (var x = 2; x < content.Length; x++)
        {
            hal.WriteMemory((ushort)(loadAddress + x - 2), content[x]);
        }

        // Set end address in X/Y registers
        var endAddress = loadAddress + content.Length - 2;
        hal.XRegister = (byte)(endAddress & 0xFF);
        hal.YRegister = (byte)(endAddress >> 8);
        hal.WriteMemory(0x90, 0x00); // status ok
        hal.SetFlag(CpuStatusFlags.Carry, false);

        return SimulateRts(hal);
    }
}