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
        
        public string Name { get; set; } = "New Resource";

        public virtual bool validate()
        {
            
            if(string.IsNullOrWhiteSpace(Name))
            {
                Log.Warning($"[GameResource Validation Failed] Resource with ID {Id} has an invalid name.");
                return false;
            }
            
            return true;
        }
    }
}
