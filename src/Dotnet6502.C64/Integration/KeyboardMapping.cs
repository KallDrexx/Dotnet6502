using Dotnet6502.C64.Hardware;
using Microsoft.Xna.Framework.Input;

namespace Dotnet6502.C64.Integration;

/// <summary>
/// Maps the current Monogame Keyboard state into C64 keyboard matrix information
/// </summary>
public class KeyboardMapping
{
    private readonly Lock _lock = new Lock();
    private KeyboardState _currentKeyboardState;

    // Keyboard matrix bit layout
    //
    //         Col 0     Col 1     Col 2     Col 3     Col 4     Col 5     Col 6     Col 7
    //        ---------------------------------------------------------------------------------
    // Row 0:  DEL       RETURN    RT/LF     F7       F1        F3        F5        DN/UP
    // Row 1:  3         W         A         4         Z         S         E         LSHIFT
    // Row 2:  5         R         D         6         C         F         T         X
    // Row 3:  7         Y         G         8         B         H         U         V
    // Row 4:  9         I         J         0         M         K         O         N
    // Row 5:  +         P         L         -         .         :         @         ,
    // Row 6:  £         *         ;         HOME      RSHIFT    =         ^         /
    // Row 7:  1         <-        CTRL      2         SPACE     C=        Q         STOP
    private static readonly Dictionary<Keys, (int Column, int Row)> KeyMappings = new()
    {
        // Row 0: DEL, RETURN, RT/LF, F7, F1, F3, F5, DN/UP
        [Keys.Back] = (0, 0),
        [Keys.Enter] = (1, 0),
        [Keys.Right] = (2, 0),
        [Keys.Left] = (2, 0),
        [Keys.F8] = (3, 0),
        [Keys.F7] = (3, 0),
        [Keys.F2] = (4, 0),
        [Keys.F1] = (4, 0),
        [Keys.F4] = (5, 0),
        [Keys.F3] = (5, 0),
        [Keys.F6] = (6, 0),
        [Keys.F5] = (6, 0),
        [Keys.Down] = (7, 0),
        [Keys.Up] = (7, 0),

        // Row 1: 3, W, A, 4, Z, S, E, LSHIFT
        [Keys.D3] = (0, 1),
        [Keys.W] = (1, 1),
        [Keys.A] = (2, 1),
        [Keys.D4] = (3, 1),
        [Keys.Z] = (4, 1),
        [Keys.S] = (5, 1),
        [Keys.E] = (6, 1),
        [Keys.LeftShift] = (7, 1),

        // Row 2: 5, R, D, 6, C, F, T, X
        [Keys.D5] = (0, 2),
        [Keys.R] = (1, 2),
        [Keys.D] = (2, 2),
        [Keys.D6] = (3, 2),
        [Keys.C] = (4, 2),
        [Keys.F] = (5, 2),
        [Keys.T] = (6, 2),
        [Keys.X] = (7, 2),

        // Row 3: 7, Y, G, 8, B, H, U, V
        [Keys.D7] = (0, 3),
        [Keys.Y] = (1, 3),
        [Keys.G] = (2, 3),
        [Keys.D8] = (3, 3),
        [Keys.B] = (4, 3),
        [Keys.H] = (5, 3),
        [Keys.U] = (6, 3),
        [Keys.V] = (7, 3),

        // Row 4: 9, I, J, 0, M, K, O, N
        [Keys.D9] = (0, 4),
        [Keys.I] = (1, 4),
        [Keys.J] = (2, 4),
        [Keys.D0] = (3, 4),
        [Keys.M] = (4, 4),
        [Keys.K] = (5, 4),
        [Keys.O] = (6, 4),
        [Keys.N] = (7, 4),

        // Row 5: +, P, L, -, ., :, @, ,
        [Keys.OemPlus] = (0, 5),
        [Keys.P] = (1, 5),
        [Keys.L] = (2, 5),
        [Keys.OemMinus] = (3, 5),
        [Keys.OemPeriod] = (4, 5),
        [Keys.OemSemicolon] = (5, 5),
        [Keys.OemTilde] = (6, 5),
        [Keys.OemComma] = (7, 5),

        // Row 6: £, *, ;, HOME, RSHIFT, =, ^, /
        [Keys.Insert] = (0, 6),       // £ -> Insert
        [Keys.OemCloseBrackets] = (1, 6), // * -> ]
        [Keys.OemQuotes] = (2, 6),    // ; -> '
        [Keys.Home] = (3, 6),
        [Keys.RightShift] = (4, 6),
        [Keys.OemOpenBrackets] = (5, 6),  // = -> [
        [Keys.Delete] = (6, 6),       // ^ -> Delete
        [Keys.OemQuestion] = (7, 6),

        // Row 7: 1, <-, CTRL, 2, SPACE, C=, Q, STOP
        [Keys.D1] = (0, 7),
        [Keys.OemPipe] = (1, 7),      // <- -> \
        [Keys.LeftControl] = (2, 7),
        [Keys.D2] = (3, 7),
        [Keys.Space] = (4, 7),
        [Keys.Tab] = (5, 7),          // C= -> Tab
        [Keys.Q] = (6, 7),
        [Keys.Escape] = (7, 7),       // STOP -> Escape
    };

    public void UpdateState(KeyboardState state)
    {
        lock (_lock)
        {
            _currentKeyboardState = state;
        }
    }

    /// <summary>
    /// Gets the value for the rows in the keyboard matrix based off of the columns
    /// specified by CIA1's Data Port A value.
    /// </summary>
    ///
    /// <returns>
    /// A byte representing which rows have keys active for the given column mask. Since the c64
    /// keyboard is active low, a 1 represents the row not being active.
    /// </returns>
    public byte GetRowValues(byte columnMask)
    {

        KeyboardState state;
        lock (_lock)
        {
            state = _currentKeyboardState;
        }

        var result = 0xFF; // Start off with all rows off
        var pressedKeys = state.GetPressedKeys() ?? [];

        foreach (var key in pressedKeys)
        {
            if (KeyMappings.TryGetValue(key, out var mapping))
            {
                // NOTE: row/col are transposed due to a bug, not sure what's going wrong
                // Probably something wrong with the CIA logic?
                var (row, col) = mapping;
                if ((columnMask & (1 << col)) == 0)
                {
                    result &= ~(1 << row);
                }
            }
        }

        return (byte)result;
    }
}