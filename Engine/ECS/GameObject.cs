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

        private bool _isActive = true;
        public string Name { get; set; } = "Game Object";
        public bool isActive { 
            get => _isActive;
            set 
            {
            _isActive = value;
                if(_isActive)
                {
                    OnEnable();
                }
                else
                {
                    OnDisable();
                }
                    ActiveEvent?.Invoke(this, isActive);
            }
        }

        public List<String> tags { get; set; } = new List<String>();
       
        [Browsable(true)]
        public Dictionary<Type, GameComponent> Components { get; set; } = new Dictionary<Type, GameComponent>();
        //public List<GameComponent> Components { get; set; } = new List<GameComponent>();
        [GISMIgnore]
        public GameScene ContextScene { get; set; } = null!;

        public event Action<GameObject, GameComponent>? OnComponentAdded;
        public event Action<GameObject, GameComponent>? OnComponentRemoved;
        public event Action<GameObject, bool>? ActiveEvent;


        // --- NEW: Hierarchy Tracking Properties ---

        // CRITICAL: Prevent circular reference crashes when saving scenes 

        [Browsable(false)]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Browsable(false)]
        public GameObject? Parent
        {
            get; set;
        }
        [Browsable(false)]
        public Guid ParentId { get; set; } = Guid.Empty;


        [Browsable(false)]
        public List<GameObject> Children { get; set; } = new List<GameObject>();


        // --- NEW: Frontloaded Core Components ---



        // --- NEW: Hierarchy Management Methods ---

        /// <summary>
        /// Attaches a child GameObject to this object, automatically handling transform inheritance.
        /// </summary>
        /// 

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

        public List<GameObject> GetChildGameObjects()
        {
            var tempList = new List<GameObject>();
            foreach(var child in Children)
            {
                if(child is GameObject gameObject)
                {
                    tempList.Add(gameObject);

                }
            }
            return tempList;
        }

        public GameObject? GetChildGameObject(string name)
        {
            foreach(var child in Children)
            {
                if(child is GameObject gameObject)
                {
                    if(gameObject.Name == name)
                    {
                        return gameObject;
                    }
                }
            }
            return null;
        }

        public void SetParent(GameObject? newParent)
        {

            if(Parent != null)
            {
                Parent.Children.Remove(this);
            }


            if(newParent != null)
            {
                Parent = newParent;

                ParentId = newParent.Id;
                if(!newParent.Children.Contains(this))
                {
                    newParent.Children.Add(this);
                }

                if(this is GameObject myGameObject && newParent is GameObject newParentGameObject)
                {

                    if(myGameObject != null && newParentGameObject != null)
                    {
                        // 👇FIX: Synchronize the offsets using the absolute coordinates loaded from JSON!
                        var myTransform = myGameObject.GetComponent<TransformComponent>();
                        var parentTransform = newParentGameObject.GetComponent<TransformComponent>();

                        if(myTransform != null && parentTransform != null)
                        {
                            // Calculate where I am in world space relative to my new parent's world space
                            myTransform.XOffset = myTransform.X - parentTransform.X;
                            myTransform.YOffset = myTransform.Y - parentTransform.Y;
                        }
                    }
                }

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
            Type targetType = typeof(T);
            // return Components.OfType<T>().FirstOrDefault();
            if(Components.TryGetValue(targetType, out var exactComponent))
            {
                return exactComponent as T;
            }

            foreach(var component in Components.Values)
            {
                if(component is T polymorphicComponent)
                {
                    return polymorphicComponent;
                }
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



        public void OnDisable()
        {
            foreach(var kvp in Components)
            {
                if(kvp.Value is ScriptComponent script)
                {
                    // Only trigger if it was initialized and running
                   
                    script.OnDisable();
                    script.hasStarted = false;
                }
            }
        }

        public void OnEnable()
        {
            foreach(var kvp in Components)
            {
                if(kvp.Value is ScriptComponent script)
                {
                    
                    script.OnEnable();
                    
                }
            }
        }

        public void Destroy()
        {
            if(ContextScene == null)
                return;

            // 1. Notify systems manager immediately to wipe it out of all warm hashes
            ContextScene.Systems?.OnEntityDestroyed(this);

            // 2. Propagate destruction downward to all tracking scripts
            foreach(var kvp in Components)
            {
                if(kvp.Value is ScriptComponent script)
                {
                    // Reset lifecycle flags just in case
                   
                    script.hasStarted = false;
                }
            }

            // 3. Unlink relationships dynamically from the hierarchy tree
            Parent?.RemoveChild(this);

            // Unlink any children cleanly so they return to root scene level or drop out safely
            var currentChildren = new List<GameObject>(Children);
            foreach(var child in currentChildren)
            {
                RemoveChild(child);
            }

            // 4. Finally, pull the plug out of the underlying registry bucket array
            ContextScene.Entities.RemoveEntity(this);
        }



    }
}
