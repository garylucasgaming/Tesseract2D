using Engine.Core.ECS;
using Engine.Core.Runtime;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameComponent = Engine.Core.ECS.GameComponent; // Reference MonoGame's Vector2

namespace Engine.Core.Serialization
{
    public static class ComponentSerializer
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance;

        private static bool ShouldIgnore(MemberInfo member)
        {
            foreach(var attr in member.GetCustomAttributes(true))
            {
                var attrName = attr.GetType().Name;
                if(attrName == "IgnoreAttribute")
                {
                    return true;
                }
            }
            return false;
        }

        // ====================================================
        // 1. EXPORT PASS
        // ====================================================

        public static Dictionary<string, object> ExportComponent(ECS.GameComponent component)
        {
            var data = new Dictionary<string, object>();
            var type = component.GetType();

            // 1. Fields
            foreach(var field in type.GetFields(Flags))
            {
                if(field.Name == "gameObject" || field.Name == "Parent" || ShouldIgnore(field))
                    continue;

                var value = field.GetValue(component);
                if(value != null)
                {
                    var exported = ExportValue(value);
                    if(exported != null)
                        data[field.Name] = exported;
                }
            }

            // 2. Properties
            foreach(var prop in type.GetProperties(Flags))
            {
                if(!prop.CanRead || !prop.CanWrite || prop.Name == "gameObject" || prop.Name == "Parent" || ShouldIgnore(prop))
                    continue;

                var value = prop.GetValue(component);
                if(value != null)
                {
                    var exported = ExportValue(value);
                    if(exported != null)
                        data[prop.Name] = exported;
                }
            }

            return data;
        }

        private static object ExportValue(object value)
        {
            if(value == null)
                return null;

            Type valType = value.GetType();

            // Primitives & Value Types
            if(valType.IsEnum)
                return value.ToString();
            if(valType == typeof(Vector2))
            {
                var vec = (Vector2) value;
                return new Dictionary<string, float> { { "X", vec.X }, { "Y", vec.Y } };
            }
            if(valType.IsPrimitive || valType == typeof(string))
                return value;

            // Single GameObject Reference -> Store Guid metadata
            if(value is GameObject go)
            {
                return new Dictionary<string, string>
                {
                    { "$ref", "GameObject" },
                    { "Id", go.Id.ToString() }
                };
            }

            // Single GameComponent Reference -> Store Owning GameObject Guid + Component Type Name
            if(value is GameComponent comp)
            {
                string goId = comp.gameObject != null ? comp.gameObject.Id.ToString() : Guid.Empty.ToString();
                return new Dictionary<string, string>
                {
                    { "$ref", "Component" },
                    { "GameObjectId", goId },
                    { "ComponentType", comp.GetType().Name }
                };
            }

            // Single GameEvent Reference / Payload
            if(value is GameEvent gameEvent)
            {
                string targetGoId = gameEvent.TargetGameObject != null ? gameEvent.TargetGameObject.Id.ToString() : Guid.Empty.ToString();
                return new Dictionary<string, object>
                {
                    { "$ref", "GameEvent" },
                    { "TargetGameObjectId", targetGoId },
                    { "TargetComponentTypeName", gameEvent.TargetComponentTypeName ?? string.Empty },
                    { "MethodName", gameEvent.MethodName ?? string.Empty }
                };
            }

            // Lists & Arrays of References or Values
            if(value is IList list)
            {
                var exportList = new List<object>();
                foreach(var item in list)
                {
                    var exportedItem = ExportValue(item);
                    if(exportedItem != null)
                        exportList.Add(exportedItem);
                }
                return exportList;
            }

            return null;
        }

        // ====================================================
        // 2. IMPORT PASS 1 (Primitives, Enums & Vector2s)
        // UNTOUCHED - Preserves exact existing behavior!
        // ====================================================

        public static void ImportComponent(ECS.GameComponent component, Dictionary<string, object> state)
        {
            if(state == null)
                return;
            var type = component.GetType();

            foreach(var kvp in state)
            {
                try
                {
                    // Try Field
                    var field = type.GetField(kvp.Key, Flags);
                    if(field != null && !ShouldIgnore(field))
                    {
                        if(field.FieldType.IsEnum)
                        {
                            field.SetValue(component, Enum.Parse(field.FieldType, kvp.Value.ToString()));
                        }
                        else if(field.FieldType == typeof(Vector2))
                        {
                            field.SetValue(component, ParseVector2(kvp.Value));
                        }
                        else
                        {
                            var converted = Convert.ChangeType(kvp.Value, field.FieldType);
                            field.SetValue(component, converted);
                        }
                        continue;
                    }

                    // Try Property
                    var prop = type.GetProperty(kvp.Key, Flags);
                    if(prop != null && prop.CanWrite && !ShouldIgnore(prop))
                    {
                        if(prop.PropertyType.IsEnum)
                        {
                            prop.SetValue(component, Enum.Parse(prop.PropertyType, kvp.Value.ToString()), null);
                        }
                        else if(prop.PropertyType == typeof(Vector2))
                        {
                            prop.SetValue(component, ParseVector2(kvp.Value), null);
                        }
                        else
                        {
                            var converted = Convert.ChangeType(kvp.Value, prop.PropertyType);
                            prop.SetValue(component, converted, null);
                        }
                    }
                }
                catch
                {
                    // Reference dictionaries are ignored gracefully during primitive pass
                }
            }
        }

        // ====================================================
        // 3. IMPORT PASS 2 (Late-Binding Reference Resolver)
        // ====================================================

        public static void ResolveComponentReferences(ECS.GameComponent component, Dictionary<string, object> state, Dictionary<Guid, GameObject> idToEntityMap)
        {
            if(state == null || component == null || idToEntityMap == null)
                return;

            var type = component.GetType();

            foreach(var kvp in state)
            {
                if(kvp.Value == null)
                    continue;

                var field = type.GetField(kvp.Key, Flags);
                var prop = type.GetProperty(kvp.Key, Flags);

                if(field == null && prop == null)
                    continue;
                if(field != null && ShouldIgnore(field))
                    continue;
                if(prop != null && ShouldIgnore(prop))
                    continue;

                Type memberType = field != null ? field.FieldType : prop.PropertyType;

                object resolvedValue = ResolveValue(kvp.Value, memberType, idToEntityMap);
                if(resolvedValue != null)
                {
                    if(field != null)
                        field.SetValue(component, resolvedValue);
                    else if(prop != null && prop.CanWrite)
                        prop.SetValue(component, resolvedValue, null);
                }
            }
        }

        private static object ResolveValue(object rawValue, Type targetType, Dictionary<Guid, GameObject> idToEntityMap)
        {
            // 1. Single Reference Payload
            if(rawValue is IDictionary dict)
            {
                string refType = GetDictString(dict, "$ref");
                if(refType == "GameObject")
                {
                    string idStr = GetDictString(dict, "Id");
                    if(Guid.TryParse(idStr, out Guid goId) && idToEntityMap.TryGetValue(goId, out var targetGo))
                    {
                        return targetGo;
                    }
                }
                else if(refType == "Component")
                {
                    string goIdStr = GetDictString(dict, "GameObjectId");
                    string compTypeName = GetDictString(dict, "ComponentType");

                    if(Guid.TryParse(goIdStr, out Guid goId) && idToEntityMap.TryGetValue(goId, out var targetGo))
                    {
                        if(targetGo.Components != null)
                        {
                            foreach(var compKvp in targetGo.Components)
                            {
                                if(compKvp.Key.Name.Equals(compTypeName, StringComparison.OrdinalIgnoreCase))
                                {
                                    return compKvp.Value;
                                }
                            }
                        }
                    }
                }
                else if(refType == "GameEvent")
                {
                    var gameEvent = new GameEvent();
                    string targetGoIdStr = GetDictString(dict, "TargetGameObjectId");
                    if(Guid.TryParse(targetGoIdStr, out Guid targetGoId) && idToEntityMap.TryGetValue(targetGoId, out var targetGo))
                    {
                        gameEvent.TargetGameObject = targetGo;
                    }
                    gameEvent.TargetComponentTypeName = GetDictString(dict, "TargetComponentTypeName") ?? string.Empty;
                    gameEvent.MethodName = GetDictString(dict, "MethodName") ?? string.Empty;
                    return gameEvent;
                }
                return null;
            }

            // 2. Collection / List of References
            if(rawValue is IList rawList && typeof(IList).IsAssignableFrom(targetType))
            {
                Type elementType = targetType.IsArray
                    ? targetType.GetElementType()
                    : targetType.GetGenericArguments().FirstOrDefault();

                if(elementType == null)
                    return null;

                var targetList = (IList) Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));

                foreach(var item in rawList)
                {
                    object resolvedItem = ResolveValue(item, elementType, idToEntityMap);
                    if(resolvedItem != null)
                    {
                        targetList.Add(resolvedItem);
                    }
                }

                if(targetType.IsArray)
                {
                    Array array = Array.CreateInstance(elementType, targetList.Count);
                    targetList.CopyTo(array, 0);
                    return array;
                }

                return targetList;
            }

            return null;
        }

        private static string GetDictString(IDictionary dict, string key)
        {
            if(dict.Contains(key))
                return dict[key]?.ToString();
            return null;
        }

        private static Vector2 ParseVector2(object value)
        {
            if(value is Vector2 directVec)
            {
                return directVec;
            }

            if(value is IDictionary dict)
            {
                float x = 0f;
                float y = 0f;

                if(dict.Contains("X"))
                    x = Convert.ToSingle(dict["X"]);
                if(dict.Contains("Y"))
                    y = Convert.ToSingle(dict["Y"]);

                return new Vector2(x, y);
            }

            if(value is string str)
            {
                str = str.Trim('{', '}', ' ');
                var parts = str.Split(new[] { ',', ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
                if(parts.Length >= 2)
                {
                    float x = 0, y = 0;
                    if(parts.Length == 2)
                    {
                        float.TryParse(parts[0], out x);
                        float.TryParse(parts[1], out y);
                    }
                    else
                    {
                        for(int i = 0; i < parts.Length - 1; i++)
                        {
                            if(parts[i].Equals("X", StringComparison.OrdinalIgnoreCase))
                                float.TryParse(parts[i + 1], out x);
                            if(parts[i].Equals("Y", StringComparison.OrdinalIgnoreCase))
                                float.TryParse(parts[i + 1], out y);
                        }
                    }
                    return new Vector2(x, y);
                }
            }

            return Vector2.Zero;
        }
    }
}