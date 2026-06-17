using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine.Core.Utilities;

namespace Engine.Core.Collections
{
    /// <summary>
    /// A centralized, typed registry responsible for managing, loading, and querying 
    /// static GameResource asset templates.
    /// </summary>
    /// <typeparam name="T">The specific type of GameResource this database manages.</typeparam>
    public class GameDatabase<T> where T : class
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string DatabaseType { get; set; } = typeof(T).AssemblyQualifiedName!;
        public List<T> Resources { get; set; } = new();
    }
}
