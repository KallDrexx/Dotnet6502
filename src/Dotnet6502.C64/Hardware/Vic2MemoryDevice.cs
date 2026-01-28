using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Hardware;

/// <summary>
/// A memory device for VIC-II registers that handles special behavior
/// for collision registers ($D01E and $D01F) which are clear-on-read.
/// </summary>
public class Vic2MemoryDevice : IMemoryDevice
{
    private const ushort SpriteSpriteCollisionOffset = 0x1E;  // $D01E
    private const ushort SpriteDataCollisionOffset = 0x1F;    // $D01F

    private readonly byte[] _bytes;

    // Collision registers are managed separately (clear-on-read behavior)
    private byte _spriteSpriteCollision;
    private byte _spriteDataCollision;

    public uint Size => (uint)_bytes.Length;

    public ReadOnlyMemory<byte>? RawBlockFromZero => _bytes.AsMemory();

    public Vic2MemoryDevice(int size)
    {
        if (size is > ushort.MaxValue + 1 or <= 0)
        {
            var message = $"Size value of {size} is invalid for the VIC-II memory device";
            throw new InvalidOperationException(message);
        }

        _bytes = new byte[size];
    }

    public void Write(ushort offset, byte value)
    {
        // Collision registers are read-only, ignore writes
        if (offset == SpriteSpriteCollisionOffset || offset == SpriteDataCollisionOffset)
        {
            return;
        }

        _bytes[offset] = value;
    }

    public byte Read(ushort offset)
    {
        // Handle clear-on-read for collision registers
        if (offset == SpriteSpriteCollisionOffset)
        {
            var value = _spriteSpriteCollision;
            _spriteSpriteCollision = 0;
            return value;
        }

        if (offset == SpriteDataCollisionOffset)
        {
            var value = _spriteDataCollision;
            _spriteDataCollision = 0;
            return value;
        }

        return _bytes[offset];
    }

    /// <summary>
    /// Sets a bit in the sprite-sprite collision register.
    /// Called by VIC-II when collision is detected.
    /// </summary>
    public void SetSpriteSpriteCollision(int spriteIndex)
    {
        _spriteSpriteCollision |= (byte)(1 << spriteIndex);
    }

    /// <summary>
    /// Sets a bit in the sprite-data collision register.
    /// Called by VIC-II when collision is detected.
    /// </summary>
    public void SetSpriteDataCollision(int spriteIndex)
    {
        _spriteDataCollision |= (byte)(1 << spriteIndex);
    }

    /// <summary>
    /// Gets the current sprite-sprite collision value without clearing it.
    /// Used internally by VIC-II for collision detection logic.
    /// </summary>
    public byte GetSpriteSpriteCollisionRaw() => _spriteSpriteCollision;

    /// <summary>
    /// Gets the current sprite-data collision value without clearing it.
    /// Used internally by VIC-II for collision detection logic.
    /// </summary>
    public byte GetSpriteDataCollisionRaw() => _spriteDataCollision;
}
