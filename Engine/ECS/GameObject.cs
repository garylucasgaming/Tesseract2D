using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization; // Essential for protecting our hierarchy from JSON loops!
using Engine.Core.ECS.Components;
using Engine.Core.Utilities;
using GISM.Core.Attributes;

namespace Engine.Core.ECS
{
    public class GameObject : Object
    {

        
        public string Name { get; set; } = "Game Object";
        public bool isActive { get; set; } = true;

        public List<String> tags { get; set; } = new List<String>();
       
        [Browsable(true)]
        public Dictionary<Type, GameComponent> Components { get; set; } = new Dictionary<Type, GameComponent>();
        //public List<GameComponent> Components { get; set; } = new List<GameComponent>();
        [GISMIgnore]
        public GameScene ContextScene { get; set; } = null!;

        public event Action<GameObject, GameComponent>? OnComponentAdded;
        public event Action<GameObject, GameComponent>? OnComponentRemoved;



    

        /// <summary>
        /// Instantiates and attaches a component of type T to this GameObject.
        /// </summary>
        public T AddComponent<T>() where T : GameComponent, new()
        {
            if(HasComponent<T>())
            {
                Log.Warning($"[ECS Warning] Component of type '{typeof(T).Name}' is already attached to '{Name}'.");
                return GetComponent<T>()!;
            }

            T newComponent = new T { gameObject = this };

            Components[typeof(T)] = newComponent;
            OnComponentAdded?.Invoke(this, newComponent);

            return newComponent;
        }

        public void AddComponent(GameComponent component)
        {
            if(component == null)
                return;

            component.gameObject = this;

            Type componentType = component.GetType();

            // Use your non-generic component list to check for duplicates safely at runtime
            if(Components.ContainsKey(componentType))
            {
                Log.Warning($"[ECS Warning] Component of type '{componentType.Name}' is already attached to '{Name}'.");
                return;
            }


            Components[componentType] = component;

            // This instantly updates the EntityManager's buckets in real-time!
            OnComponentAdded?.Invoke(this, component);
        }

        public T? GetComponent<T>() where T : GameComponent
        {
            // return Components.OfType<T>().FirstOrDefault();
            if(Components.TryGetValue(typeof(T), out var component))
            {
                return (T) component;

            }
            return null;
        }

        public bool HasComponent<T>() where T : GameComponent
        {
            return Components.ContainsKey(typeof(T));
        }

        public bool HasComponents(params Type[] componentTypes)
        {
            // If no components are requested, technically it has all of them
            if(componentTypes == null || componentTypes.Length == 0)
                return true;

            // Verify that every single requested type exists exactly as a key in our dictionary
            foreach(var requiredType in componentTypes)
            {
                // O(1) direct hash check instead of looping and checking IsAssignableFrom!
                if(!Components.ContainsKey(requiredType))
                {
                    return false; // If even one exact type is missing, it fails immediately
                }
            }

            return true;
        }

        /// <summary>
        /// Removes the first component matching type T from this object.
        /// </summary>
        public void RemoveComponent<T>() where T : GameComponent
        {
            if(typeof(T) == typeof(TransformComponent))
                return;

            var componentToRemove = GetComponent<T>();
            if(componentToRemove == null)
                return;

            componentToRemove.gameObject = null;
            Components.Remove(typeof(T));
            OnComponentRemoved?.Invoke(this, componentToRemove);
        }

        public void RemoveComponent(GameComponent component)
        {
            if(component.GetType() == typeof(TransformComponent))
                return;

            if(Components.ContainsKey(component.GetType()))
            {
                component.gameObject = null;
                Components.Remove(component.GetType());
                OnComponentRemoved?.Invoke(this, component);
            }
        }
    }
}
