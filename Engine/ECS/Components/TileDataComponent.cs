using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Components
{
    public abstract class TileDataComponent : DataComponent
    {

        public List<string> TileTypeID
        {
            get;
            set;
        } = new List<string>();

        public List<string> TileGroupID
        {
            get;
            set;
        } = new List<string>();


        public int GridX { get; set; }
        public int GridY { get; set; }

        public virtual void OnTileDestroy()
        {
        }


        public virtual void OnTileUpdate()
        {
        }

        public virtual void OnTileInteract()
        {
        }

        public virtual void OnTileAwake()
        {
        }

        


    }
}
