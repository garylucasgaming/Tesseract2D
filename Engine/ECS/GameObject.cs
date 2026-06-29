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

        public List<GameComponent> Components { get; set; } = new List<GameComponent>();
        public GameScene ContextScene { get; set; } = null!;

        public event Action<GameObject, GameComponent>? OnComponentAdded;
        public event Action<GameObject, GameComponent>? OnComponentRemoved;



        // --- NEW: Hierarchy Tracking Properties ---

        [JsonIgnore] // CRITICAL: Prevent circular reference crashes when saving scenes to JSON!
        public GameObject? Parent
        {
            get; private set;
        }

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
            Transform = new TransformComponent();
            Transform.Owner = this;
            Components.Add(Transform);
        }

        // --- NEW: Hierarchy Management Methods ---

        /// <summary>
        /// Attaches a child GameObject to this object, automatically handling transform inheritance.
        /// </summary>
        public void AddChild(GameObject child)
        {
            if(child == null || child == this)
                return;

            // If the child already has a different parent, cleanly detach it first
            child.Parent?.RemoveChild(child);

            child.Parent = this;
            Children.Add(child);

            // Tell the child's transform that it now answers to our transform matrix
            child.Transform.ParentTransform = this.Transform;
        }

        /// <summary>
        /// Removes a child relationship and returns the object back to the root scene level.
        /// </summary>
        public void RemoveChild(GameObject child)
        {
            if(Children.Contains(child))
            {
                child.Parent = null;
                child.Transform.ParentTransform = null;
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

            T newComponent = new T { Owner = this };
            Components.Add(newComponent);
            OnComponentAdded?.Invoke(this, newComponent);

            return newComponent;
        }

        public void AddComponent(GameComponent component)
        {
            if(component == null)
                return;

            Type componentType = component.GetType();

            // Use your non-generic component list to check for duplicates safely at runtime
            if(Components.Any(c => c.GetType() == componentType))
            {
                Log.Warning($"[ECS Warning] Component of type '{componentType.Name}' is already attached to '{Name}'.");
                return;
            }

            component.Owner = this;
            Components.Add(component);

            // This instantly updates the EntityManager's buckets in real-time!
            OnComponentAdded?.Invoke(this, component);
        }

        public T? GetComponent<T>() where T : GameComponent
        {
            return Components.OfType<T>().FirstOrDefault();
        }

        public bool HasComponent<T>() where T : GameComponent
        {
            return Components.OfType<T>().Any();
        }

        public bool HasComponents(params Type[] componentTypes)
        {
            // If no components are requested, technically it has all of them
            if(componentTypes == null || componentTypes.Length == 0)
                return true;

            // Verify that every requested type matches at least one component in our list
            foreach(var requiredType in componentTypes)
            {
                bool hasThisComponent = false;

                for(int i = 0; i < Components.Count; i++)
                {
                    // IsAssignableFrom handles inheritance safely (e.g., if a system asks for Collider, BoxCollider matches)
                    if(requiredType.IsAssignableFrom(Components[i].GetType()))
                    {
                        hasThisComponent = true;
                        break; // Found it, move to the next required type
                    }
                }

                // If even one required component type is missing, the entity fails the check
                if(!hasThisComponent)
                    return false;
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

            componentToRemove.Owner = null;
            Components.Remove(componentToRemove);
            OnComponentRemoved?.Invoke(this, componentToRemove);
        }
    }
}