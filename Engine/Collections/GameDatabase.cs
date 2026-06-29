using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine.Core.Utilities;

namespace Engine.Core.Collections
{
    
    public class GameDatabase
    {
        public static GameDatabase Current { get; set; } = null!;

        private readonly Dictionary<string, List<GameResource>> _tables = new();

        /// <summary>
        /// Finds a loaded asset definition by its category table name and unique Guid.
        /// </summary>
        public GameResource? GetResource(string category, Guid id)
        {
            if(_tables.TryGetValue(category, out var list))
            {
                return list.Find(r => r.Id == id);
            }
            return null;
        }

        /// <summary>
        /// Direct access to a full data table, perfect for populating the Editor spreadsheet UI rows.
        /// </summary>
        public List<GameResource> GetTable(string category)
        {
            return _tables.TryGetValue(category, out var table) ? table : new List<GameResource>();
        }

        /// <summary>
        /// Adds a newly created resource from the editor into its designated data category table.
        /// </summary>
        public void AddResource(string category, GameResource resource)
        {
            resource.ResourceType = category;
            if(!_tables.ContainsKey(category))
            {
                _tables[category] = new List<GameResource>();
            }
            _tables[category].Add(resource);
        }
    }
}

