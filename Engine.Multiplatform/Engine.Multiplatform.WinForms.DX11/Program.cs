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
#if OculusOVR
            // Enable VR with the nkast.Kni.Platform.WinForms.DX11.OculusOVR package.
            Microsoft.Xna.Platform.XR.XRFactory.RegisterXRFactory(new Microsoft.Xna.Platform.XR.LibOVR.ConcreteXRFactory());
#endif
            using(Game game = new PlatformEngine_MultiplatformGame())
                game.Run();
        }
    }
}
