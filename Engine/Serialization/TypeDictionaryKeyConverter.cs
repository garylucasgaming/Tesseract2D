using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Core.ECS;

namespace Engine.Core.Serialization
{
    /// <summary>
    /// Forces System.Text.Json to serialize Dictionary<Type, GameComponent> 
    /// using the type's AssemblyQualifiedName as the string key.
    /// </summary>
    public class TypeDictionaryKeyConverter : JsonConverter<Dictionary<Type, GameComponent>>
    {
        //  FIXED: Correct type name is Utf8JsonReader
        public override Dictionary<Type, GameComponent> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dictionary = new Dictionary<Type, GameComponent>();

            if(reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected JSON Object for component dictionary mapping.");

            while(reader.Read())
            {
                if(reader.TokenType == JsonTokenType.EndObject)
                    return dictionary;

                if(reader.TokenType == JsonTokenType.PropertyName)
                {
                    // 1. Extract the true Type directly from the dictionary string key!
                    string keyText = reader.GetString();
                    Type componentType = Type.GetType(keyText);

                    if(componentType == null)
                        throw new JsonException($"[Serialization Error] Could not resolve component Type from identifier: {keyText}");

                    // Move to the property value object block
                    reader.Read();

                    // 2. 👇 FIX: Isolate options to bypass your global polymorphic GameComponentConverter.
                    // This forces System.Text.Json to read the properties natively into the exact type we found.
                    var clearOptions = new JsonSerializerOptions(options);

                    GameComponent component = JsonSerializer.Deserialize(ref reader, componentType, clearOptions) as GameComponent;

                    if(component != null)
                    {
                        dictionary.Add(componentType, component);
                    }
                }
            }

            return dictionary;
        }
        //  FIXED: Correct type name is Utf8JsonWriter
        public override void Write(Utf8JsonWriter writer, Dictionary<Type, GameComponent> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            foreach(var kvp in value)
            {
                // 1. Write the type identifier path as the JSON property key
                string typeKey = kvp.Key.AssemblyQualifiedName ?? kvp.Key.FullName;
                writer.WritePropertyName(typeKey);

                // 2. 👇 FIX: Create a focused options footprint to serialize the component data values cleanly
                var componentOptions = new JsonSerializerOptions(options);

                // Serialize the concrete component properties (X, Y, Speed, etc.)
                JsonSerializer.Serialize(writer, kvp.Value, kvp.Value.GetType(), componentOptions);
            }

            writer.WriteEndObject();
        }
    }
}