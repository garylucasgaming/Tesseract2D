using Engine.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Collections
{
    public abstract class GameResource
    {

        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "New Resource";
        public string ResourceType { get; set; } = "Default";

        // Store the smart values directly inside the bag
        public Dictionary<string, ResourcePropertyValue> Properties { get; set; } = new();

        /// <summary>
        /// Universal gateway to access any dynamic property type safely.
        /// </summary>
        public ResourcePropertyValue Property(string propertyName)
        {
            if(Properties.TryGetValue(propertyName, out var val))
            {
                return val;
            }

            // Return a safe fallback fallback value instead of throwing a null exception
            return new ResourcePropertyValue(string.Empty, PropertyDataType.String);
        }
    }

    public enum PropertyDataType
    {
        Integer,
        Float,
        String,
        Boolean,
        Enum,
        ResourceLink // Crucial! This lets a property point to the Guid of another Resource
    }


    public class ResourcePropertyValue
    {
        public string RawValue { get; set; } = string.Empty;
        public PropertyDataType DataType
        {
            get; set;
        }

        public ResourcePropertyValue(string rawValue, PropertyDataType dataType)
        {
            RawValue = rawValue;
            DataType = dataType;
        }

        // --- THE MAGIC: IMPLICIT CASTING OPERATORS ---

        // Automatically converts to int when assigned to an int variable
        public static implicit operator int(ResourcePropertyValue value)
        {
            if(value == null)
                return 0;
            return int.TryParse(value.RawValue, out var result) ? result : 0;
        }

        // Automatically converts to float when assigned to a float variable
        public static implicit operator float(ResourcePropertyValue value)
        {
            if(value == null)
                return 0f;
            return float.TryParse(value.RawValue, out var result) ? result : 0f;
        }

        // Automatically converts to bool when assigned to a bool variable
        public static implicit operator bool(ResourcePropertyValue value)
        {
            if(value == null)
                return false;
            return bool.TryParse(value.RawValue, out var result) && result;
        }

        // Automatically converts to string when assigned to a string variable
        public static implicit operator string(ResourcePropertyValue value)
        {
            return value?.RawValue ?? string.Empty;
        }

        // Automatically converts to Guid when assigned to a Guid variable
        public static implicit operator Guid(ResourcePropertyValue value)
        {
            if(value == null)
                return Guid.Empty;
            return Guid.TryParse(value.RawValue, out var result) ? result : Guid.Empty;
        }
    }
}
