using Engine.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Collections
{
    //a smart pointer wrapper that makes referencing game resources easier and more efficient. It holds a reference to a GameResource and provides implicit conversion to the underlying resource type, allowing for seamless access while maintaining reference integrity.
    public class GameResource <T> where T: DataResource
    {

        public Guid Id {get;  set;} // The unique identifier of the target GameResource. This is used to look up the actual resource instance from a central registry or manager.


        private T? _cachedAsset; // A cached reference to the actual GameResource instance. This allows for quick access after the first lookup, improving performance by avoiding repeated searches.

        public T Asset
        {
            get
            {
                if(_cachedAsset == null)
                {
                    _cachedAsset = GameDatabaseDirector.FindResource<T>(Id);
                    if(_cachedAsset == null)
                    {
                        Log.Error($"[GameResource Error] Failed to find resource of type {typeof(T).Name} with ID {Id}. Ensure the resource exists and is registered in the GameDatabaseSystem.");
                        throw new Exception($"[GameResource Error] Failed to find resource of type {typeof(T).Name} with ID {Id}. Ensure the resource exists and is registered in the GameDatabaseSystem.");
                    }
                }
                return _cachedAsset;
            }
            set
            {
                _cachedAsset = value;
                Id = value?.Id ?? Guid.Empty; // Update the Id to match the new asset's ID, or set to Guid.Empty if the new asset is null.
            }
        }

    }
}
