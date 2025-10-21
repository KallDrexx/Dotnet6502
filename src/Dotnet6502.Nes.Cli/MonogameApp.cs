using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Dotnet6502.Nes.Cli;

/// <summary>
/// Handles input and rendering of the NES game
/// </summary>
public class MonogameApp : Game, INesDisplay, INesInput
{
    private const int Width = 256;
    private const int Height = 240;

    private readonly GraphicsDeviceManager _graphicsDeviceManager;
    private readonly ControllerState _controllerState = new();
    private readonly object _synchronizationLock = new();
    private readonly Color[] _pixelColors = new Color[Width * Height];
    private readonly bool _trackTime;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _texture = null!;
    private bool _readyToContinue;
    private bool _displayQuit; // Tells the PPU that the display quit, and thus Monitor will never be pulsed
    private Stopwatch _timer = new();
    private TimeSpan _totalTime;
    private int _frameCountSinceLastTimer;

    public Task? NesCodeTask { get; set; }

    public MonogameApp(bool trackTime)
    {
        _graphicsDeviceManager = new GraphicsDeviceManager(this);

        IsMouseVisible = true;
        Window.AllowUserResizing = true;

        _trackTime = trackTime;
    }

    public void RenderFrame(RgbColor[] pixels)
    {
        if (pixels.Length != _pixelColors.Length)
        {
            var message = "Frame buffer from NES has different pixel count than texture pixel array length";
            throw new InvalidOperationException(message);
        }

        if (_trackTime)
        {
            _timer.Stop();
            _totalTime += _timer.Elapsed;
            _frameCountSinceLastTimer++;

            if (_frameCountSinceLastTimer == 120)
            {
                var averageTime = _totalTime / _frameCountSinceLastTimer;
                Console.WriteLine($"Average NES time: {averageTime.TotalMilliseconds}ms");

                _totalTime = TimeSpan.Zero;
                _frameCountSinceLastTimer = 0;
            }
        }

        lock (_synchronizationLock)
        {
            for (var x = 0; x < _pixelColors.Length; x++)
            {
                var color = new Color(pixels[x].Red, pixels[x].Green, pixels[x].Blue);
                _pixelColors[x] = color;
            }

            while (!_readyToContinue && !_displayQuit)
            {
                Monitor.Wait(_synchronizationLock);
            }

            if (_trackTime)
            {
                _timer.Restart();
            }

            _readyToContinue = false;
        }
    }

    protected override void Initialize()
    {
        _graphicsDeviceManager.PreferredBackBufferWidth = 1024;
        _graphicsDeviceManager.PreferredBackBufferHeight = 768;
        _graphicsDeviceManager.ApplyChanges();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _texture = new Texture2D(_graphicsDeviceManager.GraphicsDevice, Width, Height);

        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        if (NesCodeTask == null)
        {
            throw new InvalidOperationException("No NES code task available yet");
        }

        if (NesCodeTask.IsCompleted)
        {
            if (NesCodeTask.IsCompletedSuccessfully)
            {
                Console.WriteLine("NES code ended");
                Exit();

                return;
            }

            if (NesCodeTask.Exception != null)
            {
                throw NesCodeTask.Exception;
            }

            throw new InvalidOperationException("NES code faulted without exception");
        }

        // Now that update has called, Signal to the NES thread that it can continue with the next frame
        lock (_synchronizationLock)
        {
            _readyToContinue = true;
            Monitor.Pulse(_synchronizationLock);

            _texture.SetData(_pixelColors);
        }

        var keyboardState = Keyboard.GetState();
        _controllerState.Up = keyboardState.IsKeyDown(Keys.Up);
        _controllerState.Down = keyboardState.IsKeyDown(Keys.Down);
        _controllerState.Left = keyboardState.IsKeyDown(Keys.Left);
        _controllerState.Right = keyboardState.IsKeyDown(Keys.Right);
        _controllerState.Start = keyboardState.IsKeyDown(Keys.Enter);
        _controllerState.Select = keyboardState.IsKeyDown(Keys.Back);
        _controllerState.A = keyboardState.IsKeyDown(Keys.Z);
        _controllerState.B = keyboardState.IsKeyDown(Keys.X);

        if (keyboardState.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();
        _spriteBatch.Draw(_texture,
            ComputeDestinationRectangle(),
            new Rectangle(0, 0, Width, Height),
            Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        lock (_synchronizationLock)
        {
            _displayQuit = true;
            Monitor.Pulse(_synchronizationLock);
        }

        base.OnExiting(sender, args);
    }

    private Rectangle ComputeDestinationRectangle()
    {
        var textureAspectRatio = (float)Width / Height;
        var viewportAspectRatio = (float)GraphicsDevice.Viewport.Width / GraphicsDevice.Viewport.Height;
        var startX = 0;
        var startY = 0;
        int height, width;

        if (textureAspectRatio > viewportAspectRatio)
        {
            // texture has a wider aspect ratio than the viewport, texture is width constrained.
            width = GraphicsDevice.Viewport.Width;
            height = (int)Math.Round(width * textureAspectRatio);
            startY = (GraphicsDevice.Viewport.Height - height) / 2;
        }
        else
        {
            // Viewport has a wider aspect ratio than the texture, texture is height constrained.
            height = GraphicsDevice.Viewport.Height;
            width = (int)Math.Round(height * textureAspectRatio);
            startX = (GraphicsDevice.Viewport.Width - width) / 2;
        }

        return new Rectangle(startX, startY, width, height);
    }

    public ControllerState GetGamepad1State()
    {
        return _controllerState;
    }
}