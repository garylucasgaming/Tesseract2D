using Engine.Core.Collections;
using Engine.Core.Runtime;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.GamePlay
{
    public class Map
    {

        private int[,] _grid;
        private int _tileSize = 32;
        private string _tileSetPath = "";
        private string _MapName = "Untitled Map";
        private int _LayerOrder = 0;
        private bool _isEnabled = true;

        /// <summary>
        /// this is for linking up the grid values to a sprite tileset, so that the grid values can be used to determine which sprite to render for each tile in the grid
        /// </summary>
        private Dictionary<int, int> _tileProperties = new Dictionary<int, int>();

      

        public string MapName
        {
            get => _MapName;
            set => _MapName = value;
        }
        public int LayerOrder
        {
            get => _LayerOrder;
            set => _LayerOrder = value;
        }

        [Browsable(false)]
        [DatabaseIgnore]
        public int[,] Grid
        {
            get => _grid;
            set => _grid = value;
        }

        public int Width
        {
            get => _grid.GetLength(0);
            set => Resize(value, Height);
        }
        public int Height
        {
            get => _grid.GetLength(1);
            set => Resize(Width, value);
        }

        public int TileSize
        {
            get => _tileSize;
            set => _tileSize = value;
        }
        public string TileSetPath
        {
            get => _tileSetPath;
            set => _tileSetPath = value;
        }

        [Browsable(false)]
        [DatabaseIgnore]
        public Dictionary<int, int> TileProperties
        {
            get => _tileProperties;
            set => _tileProperties = value;
        }
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public Map(int width, int height)
        {
            // Pull default tile size from your new GameWorldManager
            _tileSize = GameWorldManager.TileSize;
            _grid = new int[width, height];
        }


        public void Resize(int newWidth, int newHeight)
        {
            var newGrid = new int[newWidth, newHeight];
            for(int x = 0; x < Math.Min(Width, newWidth); x++)
            {
                for(int y = 0; y < Math.Min(Height, newHeight); y++)
                {
                    newGrid[x, y] = _grid[x, y];
                }
            }
            _grid = newGrid;
        }


    }
}

