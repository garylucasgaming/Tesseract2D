using Microsoft.Xna.Framework;
using System;

namespace Engine_Multiplatform
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using(Game game = new PlatformEngine_MultiplatformGame())
                game.Run();
        }
    }
}
