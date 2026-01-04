using System.Diagnostics;
using Dotnet6502.C64.Emulation;
using Dotnet6502.C64.Hardware;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Dotnet6502.C64.Integration;

public class MonogameApp : Game, IC64Display
{
    private const int Width = Vic2.VisibleDotsPerScanLine;
    private const int Height = Vic2.VisibleScanLines;

    private readonly GraphicsDeviceManager _graphicsDeviceManager;
    private readonly object _synchronizationLock = new();
    private readonly Color[] _pixelColors = new Color[Width * Height];
    private readonly bool _trackTime;
    private readonly Stopwatch _timer = new();
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _texture = null!;
    private bool _readyToContinue;
    private bool _displayQuit; // Tells the VIC-II that the display quit, and thus Monitor will never be pulsed
    private TimeSpan _totalTime;
    private int _frameCountSinceLastTimer;
    private bool _warningRaised;

    public Task? C64CodeTask { get; set; }

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
            var message = "Frame buffer from the C64 has different pixel count than texture pixel array length";
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
                Console.WriteLine($"Average C64 frame time: {averageTime.TotalMilliseconds}ms");

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
        if (C64CodeTask == null)
        {
            throw new InvalidOperationException("No C64 code task available yet");
        }

        if (C64CodeTask.IsCompleted)
        {
            if (C64CodeTask.IsCompletedSuccessfully)
            {
                Console.WriteLine("C64 code ended");
                Exit();

                return;
            }

            if (C64CodeTask.Exception != null)
            {
                throw C64CodeTask.Exception;
            }

            throw new InvalidOperationException("C64 code faulted without exception");
        }

        // Now that update has called, Signal to the C64 thread that it can continue with the next frame
        lock (_synchronizationLock)
        {
            _texture.SetData(_pixelColors);
            _readyToContinue = true;
            Monitor.Pulse(_synchronizationLock);

            if (_timer.Elapsed > TimeSpan.FromSeconds(60) && !_warningRaised)
            {
                Console.WriteLine($"Frame not received from vic2 in over 60 seconds");
                _warningRaised = true;
            }
        }

        var keyboardState = Keyboard.GetState();
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
        const float textureAspectRatio = (float)Width / Height;
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
}