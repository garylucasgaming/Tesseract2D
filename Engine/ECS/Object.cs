using Engine.Core.ECS.Components;
using GISM.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS
{
    public class Object
    {
        // --- NEW: Hierarchy Tracking Properties ---

        // CRITICAL: Prevent circular reference crashes when saving scenes 

       
        public Guid Id { get; set; } = Guid.NewGuid();

        public Object Parent
        {
            get; set;
        }

        public Guid ParentId { get; set; } = Guid.Empty;


        [GISMIgnore]
        public List<Object> Children { get; set; } = new List<Object>();


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

        public void SetParent(Object? newParent)
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
    }
}
