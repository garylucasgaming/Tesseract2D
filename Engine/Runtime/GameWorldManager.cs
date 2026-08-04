using Engine.Core.ECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Runtime
{
    public static class GameWorldManager
    {
        private static int _tileSize = 32;

        public static int TileSize
        {
            get => _tileSize;
            set => _tileSize = value;
        }

        


    }
}
