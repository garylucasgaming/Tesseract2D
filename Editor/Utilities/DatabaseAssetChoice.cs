using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Editor.Utilities
{
    public class DatabaseAssetChoice
    {
        public Guid AssetID
        {
            get;
            init;
        }

        public string DisplayName { get; init; }

        public DatabaseAssetChoice(Guid id, string name)
        {
            AssetID = id;
            DisplayName = name;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
