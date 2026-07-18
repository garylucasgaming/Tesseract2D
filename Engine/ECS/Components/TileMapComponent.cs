using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Components
{
    public class TileMapComponent : GameComponent
    {
        private int _width =32;
        private int _height = 32;
        private int _tileSize = 32;
        private int _chunkSize = 16;
        public int MapWidthInTiles 
        {
            get => _width;
            set => _width = value;
        }
        public int MapHeightInTiles
        {
            get => _height;
            set => _height = value;
        }

        public int TileSize
        {
            get => _tileSize;
            set => _tileSize = value;
        } 

        
        public int ChunkSize
        {
            get => _chunkSize;
            set => _chunkSize = value;
        }

        [Browsable(false)]
        public TileMapChunk[,]? ChunkGrid { get; set; }

        public void InitializeMap()
        {

            int chunksX = (int) Math.Ceiling((double) MapWidthInTiles / ChunkSize);
            int chunkY = (int) Math.Ceiling((double) MapHeightInTiles / ChunkSize);
            ChunkGrid = new TileMapChunk[chunksX, chunkY];

            for(int x = 0; x < chunksX; x++)
            {
                for(int y = 0; y < chunkY; y++)
                {
                    ChunkGrid[x, y] = new TileMapChunk(x, y, ChunkSize);
                }
            }

        }

        public GameObject GetTileAt(int gridX, int gridY)
        {
            if(gridX < 0 || gridX >= MapWidthInTiles || gridY < 0 || gridY >= MapHeightInTiles)
                return null;

            int chunkX = gridX / ChunkSize;
            int chunkY = gridY / ChunkSize;

            int localX = gridX % ChunkSize;
            int localY = gridY % ChunkSize;

            return ChunkGrid[chunkX, chunkY].TileGameObjects[localX, localY];

        }

    }
}
