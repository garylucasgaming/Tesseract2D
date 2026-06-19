using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization; // Essential for protecting our hierarchy from JSON loops!
using Engine.Core.ECS.Components;

namespace Engine.Core.ECS
{
    public class GameObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "Game Object";
        public bool isActive { get; set; } = true;

        public List<GameComponent> Components { get; set; } = new List<GameComponent>();
        public GameScene ContextScene { get; set; } = null!;

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
            AddComponent(Transform);
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

        // --- Existing Component Logic (Kept exactly as you wrote it) ---

        public void AddComponent<T>(T component) where T : GameComponent
        {
            component.Owner = this;
            Components.Add(component);
        }

        public T? GetComponent<T>() where T : GameComponent
        {
            return Components.OfType<T>().FirstOrDefault();
        }

        public bool HasComponent<T>() where T : GameComponent
        {
            return Components.OfType<T>().Any();
        }

        public void RemoveComponent(GameComponent component)
        {
            // Guard safety loop: Prevent developers from accidentally deleting the baseline transform!
            if(component is TransformComponent)
            {
                Utilities.Log.Warning($"[ECS Warning] Cannot remove TransformComponent from '{Name}'. Every GameObject requires a spatial transform.");
                return;
            }

            if(Components.Contains(component))
            {
                component.Owner = null;
                Components.Remove(component);
            }
        }
    }
}