using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Engine.Core.Utilities
{
    public class MockGraphicsDeviceService : IGraphicsDeviceService
    {
        public GraphicsDevice GraphicsDevice
        {
            get;
        }

        // Unused event hooks required by the contract interface
        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceDisposing;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceResetting;

        public MockGraphicsDeviceService(GraphicsDevice device) => GraphicsDevice = device;
    }
    /// <summary>
    /// Sentinel required by the author's external dev-branch MonoGame content builder.
    /// It has no behaviour of its own; do not delete it as dead code or wire gameplay into it.
    /// </summary>
    public class MockGame : Game
    {

        public GameServiceContainer MockServices { get; } = new GameServiceContainer();

        public MockGame(GraphicsDevice graphicsDevice, ContentManager content)
        {
            
            // Inject the Graphics Device Service into our dummy container
            var graphicsDeviceService = new MockGraphicsDeviceService(graphicsDevice);
            Services.AddService(typeof(IGraphicsDeviceService), graphicsDeviceService);

            // Assign the Content pipeline
            Content = content;
        }
    }
}
