using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using Engine.Core.Utilities;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Engine.Core.Serialization
{
    /// <summary>
    /// Handles polymorphic JSON serialization and file I/O for the engine's entity hierarchies.
    /// </summary>
    public static class SceneSerializer
    {
        private static readonly JsonSerializerOptions _options;

        static SceneSerializer()
        {
            _options = new JsonSerializerOptions
            {
                WriteIndented = true, // Keeps files clean, human-readable, and moddable
                TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { PolymorphicModifier }
                }
            };
        }

        /// <summary>
        /// Intercepts component serialization to handle abstract GameComponent derivatives cleanly.
        /// </summary>
        private static void PolymorphicModifier(JsonTypeInfo typeInfo)
        {
            if(typeInfo.Type == typeof(GameComponent))
            {
                typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = "$type",
                    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType
                };

                // Register our mandatory transform component
                typeInfo.PolymorphismOptions.DerivedTypes.Add(
                    new JsonDerivedType(typeof(TransformComponent), "TransformComponent"));

                // NOTE: As you add new gameplay components later, you'll register them right here!
            }
        }

        /// <summary>
        /// Serializes a GameObject tree and writes it to a physical file on disk.
        /// </summary>
        public static void SaveSceneToFile(GameObject rootObject, string relativePath)
        {
            try
            {
                // MAGIC LINE: Forces the file out of bin/Debug and into your permanent folder!
                string absolutePath = AssetPathProvider.ResolveProjectPath(relativePath);

                string jsonString = JsonSerializer.Serialize(rootObject, _options);

                string? directory = Path.GetDirectoryName(absolutePath);
                if(!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(absolutePath, jsonString);
                Log.Info($"[Serializer] Physically saved scene asset to permanent drive location: {absolutePath}");
            }
            catch(Exception ex)
            {
                Log.Error($"[Serializer Error] Failed to save scene file. Reason: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads a physical file from disk and parses it back into a fully operational GameObject tree.
        /// </summary>
        public static GameObject? LoadSceneFromFile(string relativePath)
        {
            try
            {
                // MAGIC LINE: Resolves the path to your permanent 'SavedGameProjectData' folder!
                string absolutePath = AssetPathProvider.ResolveProjectPath(relativePath);

                if(!File.Exists(absolutePath))
                {
                    Log.Error($"[Serializer Error] Scene file does not exist at absolute path: {absolutePath}");
                    return null;
                }

                // Read out the text file from your permanent drive location
                string jsonString = File.ReadAllText(absolutePath);
                GameObject? root = JsonSerializer.Deserialize<GameObject>(jsonString, _options);

                if(root != null)
                {
                    // Run the crucial post-load pass to rebuild pointers ignored by JSON
                    FixHierarchyPointers(root);
                    Log.Info($"[Serializer] Successfully loaded scene asset from: {relativePath}");
                }

                return root;
            }
            catch(Exception ex)
            {
                Log.Error($"[Serializer Error] Failed to read scene file at '{relativePath}'. Reason: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deep-traverses the loaded tree to stitch Owner, Parent, and Transform links back together.
        /// </summary>
        private static void FixHierarchyPointers(GameObject node)
        {
            // 1. Re-link component owners
            foreach(var component in node.Components)
            {
                component.Owner = node;
            }

            // 2. Re-link the fast-access Transform property shortcut
            var transform = node.GetComponent<TransformComponent>();
            if(transform != null)
            {
                var prop = typeof(GameObject).GetProperty(nameof(GameObject.Transform));
                prop?.SetValue(node, transform);
            }

            // 3. Drill down into children recursively
            foreach(var child in node.Children)
            {
                // Re-bind the auto-implemented backing field for the Parent property safely via reflection
                var parentField = typeof(GameObject).GetField("<Parent>k__BackingField",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                parentField?.SetValue(child, node);

                // Build spatial tree linkage
                if(child.Transform != null && node.Transform != null)
                {
                    child.Transform.ParentTransform = node.Transform;
                }

                // Continue recursion
                FixHierarchyPointers(child);
            }
        }
    }
}