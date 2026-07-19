using Engine.Core.ECS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Collections
{
    public class Database
    {
        public string Name = string.Empty;
        public Guid ID = Guid.NewGuid();
        public string DatabaseType = string.Empty;

        public Dictionary<Guid, DataComponent> ComponentDatabase = new Dictionary<Guid, DataComponent>();

        public DataComponent? GetComponent(Guid assetID)
        {

            if(ComponentDatabase.ContainsKey(assetID))
            {
                return ComponentDatabase[assetID];
            }
            else
            {
                return null;
            }

        }

    }
}
