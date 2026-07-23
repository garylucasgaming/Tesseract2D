using Engine.Core.Runtime;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Components
{
    public class DataLinkComponent : GameComponent
    {

        [Browsable(false)]
        public Guid AssetID
        {
            get;
            set;
        }

        public string DatabaseName { get; set; }

        [Browsable(false)]
        public DataComponent CachedData
        {
            get
            {
                if(_cachedData == null)
                {
                    _cachedData = gameObject.ContextScene.Database.GetDatabaseByName(DatabaseName)?.GetComponent(AssetID);
                }
                return _cachedData;
            }
            
        }

        private DataComponent _cachedData;
    }
}
