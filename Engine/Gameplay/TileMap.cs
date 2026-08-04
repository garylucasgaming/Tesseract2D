using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.GamePlay
{
    public class TileMap : Map
    {

        public struct TileCell
        {
            public int TileId;
            public bool IsCollidable;
        }

        private TileCell[,] _cellGrid;

        public TileCell[,] CellGrid
        {
            get => _cellGrid;
            set => _cellGrid = value;
        }

        public TileMap(int width, int height) : base(width, height)
        {
            _cellGrid = new TileCell[width, height];
        }
    }
}
