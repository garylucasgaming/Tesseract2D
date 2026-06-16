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
    public class GameDatabase<T> where T : DataResource
    {
        /// <summary>
        /// Human-readable identifier for editor tree-views (e.g., "Voxel Block Registry").
        /// </summary>
        public string DatabaseName { get; set; } = "New GameDatabase";

        // An internal dictionary for O(1) lightning-fast lookup performance by Guid
        private readonly Dictionary<Guid, T> _resourceCache = new();

        /// <summary>
        /// Exposes all managed resources as a read-only list for WinForms editor item lists.
        /// </summary>
        public IReadOnlyList<T> AllResources => _resourceCache.Values.ToList();

        /// <summary>
        /// Instantly finds a resource template by its unique identifier string.
        /// Used automatically by our GameResource wrappers.
        /// </summary>
        public T? GetById(Guid id)
        {
            if(_resourceCache.TryGetValue(id, out var resource))
            {
                return resource;
            }
            return null;
        }

        /// <summary>
        /// Finds a resource by its human-readable editor name. Excellent for developer queries.
        /// </summary>
        public T? GetByName(string name)
        {
            return _resourceCache.Values.FirstOrDefault(r =>
                r.ResourceName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Adds a resource to the memory cache registry.
        /// </summary>
        public void Add(T resource)
        {
            if(resource == null)
                return;

            if(!_resourceCache.ContainsKey(resource.Id))
            {
                _resourceCache.Add(resource.Id, resource);
            }
            else
            {
                // Overwrite the existing copy (critical for live hot-reloading in your editor!)
                _resourceCache[resource.Id] = resource;
            }
        }

        /// <summary>
        /// Removes a resource from the runtime registry.
        /// </summary>
        public void Remove(Guid id)
        {
            if(_resourceCache.Remove(id))
            {
               Log.Info($"Removed resource ID '{id}' from {DatabaseName}.");
            }
        }

        /// <summary>
        /// Empties the entire memory cache registry.
        /// </summary>
        public void Clear()
        {
            _resourceCache.Clear();
        }
    }
}
