using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;

namespace Engine_Multiplatform
{
    /// <inheritdoc/>
    public class PlatformEngine_MultiplatformGame : Engine_MultiplatformGame
    {

        public PlatformEngine_MultiplatformGame() : base()
        {
            // TODO: Add platform specific initialization logic here

            base.graphics.IsFullScreen = true;
        }

        /// <inheritdoc/>
        protected override void Initialize()
        {
            base.Initialize();
        }

        /// <inheritdoc/>
        protected override void LoadContent()
        {
            base.LoadContent();
        }

        /// <inheritdoc/>
        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        /// <inheritdoc/>
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        /// <inheritdoc/>
        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }
    }
}
