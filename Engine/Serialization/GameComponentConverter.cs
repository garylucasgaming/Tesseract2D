using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Engine.Core.ECS;

namespace Engine.Core.Serialization
{
    public class GameComponentConverter : JsonConverter<GameComponent>
    {
        public override GameComponent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jsonObject = JsonNode.Parse(ref reader)?.AsObject();
            if(jsonObject == null)
                return null;

            // Look for our custom type discriminator field
            if(!jsonObject.TryGetPropertyValue("$Type", out var typeNode) || typeNode == null)
            {
                throw new JsonException("Missing '$Type' property inside component data block.");
            }

            string typeAlias = typeNode.GetValue<string>();
            Type? targetType = ComponentTypeRegistry.GetType(typeAlias);

            // If the type isn't registered, skip it safely so the editor/game doesn't hard-crash
            if(targetType == null)
                return null;

            return jsonObject.Deserialize(targetType, options) as GameComponent;
        }

        public override void Write(Utf8JsonWriter writer, GameComponent value, JsonSerializerOptions options)
        {
            Type runtimeType = value.GetType();
            string alias = ComponentTypeRegistry.GetAlias(runtimeType);

            // 👇 FIX: Create clean component options to isolate property-level fields (X, Y, etc.)
            var clearOptions = new JsonSerializerOptions(options);

            // Serialize the component data into a temporary node tree safely
            var jsonNode = JsonSerializer.SerializeToNode(value, runtimeType, clearOptions)?.AsObject();

            if(jsonNode != null)
            {
                // Inject the $Type property cleanly at the top of the object block
                var specializedObject = new JsonObject { { "$Type", alias } };
                foreach(var kvp in jsonNode)
                {
                    if(kvp.Value != null)
                    {
                        // Note: using .DeepClone() here works perfectly fine once the node tree is clean!
                        specializedObject.Add(kvp.Key, kvp.Value.DeepClone());
                    }
                }

                specializedObject.WriteTo(writer, options);
            }
        }
    }
}
