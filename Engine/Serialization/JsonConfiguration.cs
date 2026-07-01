
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
    }
}
