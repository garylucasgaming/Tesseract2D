using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization; // Essential for protecting our hierarchy from JSON loops!
using Engine.Core.ECS.Components;
using Engine.Core.Utilities;

namespace Engine.Core.ECS
{
    public class GameObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "Game Object";
        public bool isActive { get; set; } = true;

        public List<String> tags { get; set; } = new List<String>();

        public Dictionary<Type, GameComponent> Components { get; set; } = new Dictionary<Type, GameComponent>();
        //public List<GameComponent> Components { get; set; } = new List<GameComponent>();
       [JsonIgnore]
        public GameScene ContextScene { get; set; } = null!;

        public event Action<GameObject, GameComponent>? OnComponentAdded;
        public event Action<GameObject, GameComponent>? OnComponentRemoved;



        // --- NEW: Hierarchy Tracking Properties ---

        [JsonIgnore] // CRITICAL: Prevent circular reference crashes when saving scenes to JSON!
        public GameObject? Parent
        {
            get; private set;
        }

        public Guid? ParentId { get; set; } = null;

        [JsonIgnore]
        public List<GameObject> Children { get; set; } = new List<GameObject>();

        // --- NEW: Frontloaded Core Components ---

        [JsonIgnore] // Fast-access property shortcut for the editor and systems
        public TransformComponent Transform
        {
            get; private set;
        }

        /// <summary>
        /// Constructor guarantees every single GameObject spawns with a valid spatial footprint.
        /// </summary>
        public GameObject()
        {
            // Frontload the transform component automatically
            Transform = new TransformComponent() { gameObject = this };
            AddComponent(Transform);
        }

        // --- NEW: Hierarchy Management Methods ---

        /// <summary>
        /// Attaches a child GameObject to this object, automatically handling transform inheritance.
        /// </summary>
        /// 
        public void SetParent(GameObject? newParent)
        {
            if(Parent != null)
            {
                Parent.Children.Remove(this);
            }

            Parent = newParent;

            if(newParent != null)
            {
                ParentId = newParent.Id;
                if(!newParent.Children.Contains(this))
                {
                    newParent.Children.Add(this);
                }

                // 👇FIX: Synchronize the offsets using the absolute coordinates loaded from JSON!
                var myTransform = GetComponent<TransformComponent>();
                var parentTransform = newParent.GetComponent<TransformComponent>();

                if(myTransform != null && parentTransform != null)
                {
                    // Calculate where I am in world space relative to my new parent's world space
                    myTransform.XOffset = myTransform.X - parentTransform.X;
                    myTransform.YOffset = myTransform.Y - parentTransform.Y;
                }
            }
            else
            {
                ParentId = null;
            }
        }
        public void AddChild(GameObject child)
        {
            if(child == null || child == this)
                return;

            // If the child already has a different parent, cleanly detach it first
            child.Parent?.RemoveChild(child);

            child.Parent = this;
            Children.Add(child);

            
        }

        /// <summary>
        /// Removes a child relationship and returns the object back to the root scene level.
        /// </summary>
        public void RemoveChild(GameObject child)
        {
            if(Children.Contains(child))
            {
                child.Parent = null;
                Children.Remove(child);
            }
        }


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
    }
}