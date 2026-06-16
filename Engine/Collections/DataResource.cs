using Engine.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Collections
{
    public abstract class DataResource
    {

        public Guid Id { get; private set; } = Guid.NewGuid(); // Unique identifier for this resource, generated automatically upon instantiation.


        public string ResourceName { get; set; } = "New GameResource"; // Default name for new resources, should be overridden by specific implementations for clarity.

        public string ResourcePath { get; set; } = ""; // The file path or URI where this resource can be loaded from. Should be set by specific implementations to point to the actual asset location.


        public virtual bool validate()
        {
            
            if(string.IsNullOrWhiteSpace(ResourceName))
            {
                Log.Warning($"[GameResource Validation Failed] Resource with ID {Id} has an invalid name.");
                return false;
            }
            
            return true;
        }
    }
}
