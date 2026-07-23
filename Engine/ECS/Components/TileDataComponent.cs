using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Components
{

    public enum TileType
    {
        Water, Air, Grass, Stone

    }
    public class TileDataComponent : DataComponent
    {
       
        public override string DisplayName
        {
            get;
            set;
        }


        public int TileSize
        {
            get;set;
        }

        public string SpritePath
        {
            get; set;
        }

        public TileType TileType {
            get; set;
        }

        public bool hasCollider { get; set; }

       

        
    }
}
