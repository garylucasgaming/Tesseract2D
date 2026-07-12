using Microsoft.Xna.Framework.Content.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Log = Engine.Core.Utilities.Log;

namespace Engine.Content.Builder
{
    public class EngineContentLogger : ContentBuildLogger
    {
       
        public override void Log(LogLevel level, string message)
        {
            // Redirect based on the level
            if(level == LogLevel.Error)
                Engine.Core.Utilities.Log.Error($"[Content Builder] {message}");
            else if(level == LogLevel.Warning)
                Engine.Core.Utilities.Log.Warning($"[Content Builder] {message}");
            else
            {
                Engine.Core.Utilities.Log.Info($"[Content Builder] {message}");
            }
        }

    }
}
