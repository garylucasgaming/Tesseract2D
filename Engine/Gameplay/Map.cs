using Engine.Core.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.GamePlay
{
    public class Map
    {

        protected int[,] _grid;
        protected int _tileSize;

        public int[,] Grid
        {
            get => _grid;
            set => _grid = value;
        }

        public int Width => _grid.GetLength(0);
        public int Height => _grid.GetLength(1);

        public int TileSize
        {
            get => _tileSize;
            set => _tileSize = value;
        }

        public Map(int width, int height)
        {
            // Pull default tile size from your new GameWorldManager
            _tileSize = GameWorldManager.TileSize;
            _grid = new int[width, height];
        }
    }




}

