using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Dynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS
{
    public class TileMapChunk
    {

        public int ChunkGridX { get; set; }
        public int ChunkGridY { get; set; }

        public GameObject[,] TileGameObjects
        {
            get; 
            set;
        } = new GameObject[16, 16];

        public RenderTarget2D BakedTexture { get; set; }
        public bool IsRenderDirty { get; set; } = true;

        public Body ChunkStaticBody
        {
            get;
            set;
        }

        public bool IsPhysicsDirty { get; set; } = true;

        public TileMapChunk(int chunkX, int chunkY, int chunksize)
        {
            this.ChunkGridX = chunkX;
            this.ChunkGridY = chunkY;
            this.TileGameObjects = new GameObject[chunksize, chunksize];
        }
            

    }
}
