using Engine.Core.Collections;
using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Engine.Core.Serialization
{
    /// <summary>
    /// Handles automated polymorphic JSON serialization settings via assembly reflection scanning.
    /// </summary>
    public static class JsonConfiguration
    {
        public static JsonSerializerOptions Options
        {
            get;
        }

        static JsonConfiguration()
        {
            Options = new JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { PolymorphicModifier }
                }
            };
        }

        private static void PolymorphicModifier(JsonTypeInfo typeInfo)
        {
            // 1. Automatically map any class deriving from GameComponent
            if(typeInfo.Type == typeof(GameComponent))
            {
                ConfigurePolymorphismForBaseType(typeInfo, typeof(GameComponent));
            }

            // 2. Automatically map any class deriving from DataResource
            if(typeInfo.Type == typeof(DataResource))
            {
                ConfigurePolymorphismForBaseType(typeInfo, typeof(DataResource));
            }
        }

        /// <summary>
        /// Scans all loaded assemblies to dynamically build the polymorphic type manifest.
        /// </summary>
        private static void ConfigurePolymorphismForBaseType(JsonTypeInfo typeInfo, Type baseType)
        {
            typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "$type",
                UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType
            };

            // Fetch all assemblies currently loaded in the game instance
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach(var assembly in assemblies)
            {
                // Optimization: Skip internal Microsoft/System libraries
                string? assemblyName = assembly.FullName;
                if(assemblyName != null && (assemblyName.StartsWith("System") || assemblyName.StartsWith("Microsoft")))
                {
                    continue;
                }

                try
                {
                    // Find all concrete classes that inherit from our target base type
                    var derivedTypes = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(baseType));

                    foreach(var derivedType in derivedTypes)
                    {
                        // Use the clear, simple Class Name as the JSON identifier string (e.g. "TransformComponent")
                        string discriminator = derivedType.Name;

                        // Prevent duplicate mapping attempts if the type modifier runs multiple times
                        if(!typeInfo.PolymorphismOptions.DerivedTypes.Any(d => d.DerivedType == derivedType))
                        {
                            typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(derivedType, discriminator));
                        }
                    }
                }
                catch(ReflectionTypeLoadException)
                {
                    // Safely skip any assemblies that can't be completely scanned at runtime
                    continue;
                }
            }
        }
    }
}
