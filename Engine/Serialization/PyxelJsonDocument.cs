using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Serialization
{
    public class PyxelJsonTile
    {
        public int tile
        {
            get; set;
        }
        public int x
        {
            get; set;
        }
        public int y
        {
            get; set;
        }
        public int index
        {
            get; set;
        }
        public int rot
        {
            get; set;
        }
        public bool flipX
        {
            get; set;
        }
    }

    public class PyxelJsonLayer
    {
        public string name
        {
            get; set;
        }
        public int number
        {
            get; set;
        }
        public List<PyxelJsonTile> tiles
        {
            get; set;
        }
    }

    public class PyxelJsonDocument
    {
        public int tileheight
        {
            get; set;
        }
        public int tilewidth
        {
            get; set;
        }
        public int tileswide
        {
            get; set;
        }
        public int tileshigh
        {
            get; set;
        }
        public List<PyxelJsonLayer> layers
        {
            get; set;
        }
    }
}
