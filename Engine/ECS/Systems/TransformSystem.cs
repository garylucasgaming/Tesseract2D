using System;
using System.Collections.Generic;
using Engine.Core.ECS.Components;
using Engine.Core.Utilities;

namespace Engine.Core.ECS.Systems
{
    public class TransformSystem : GameSystem
    {
        public override IComponentQuery RequiredComponents
        {
            get; set;
        }
        private readonly HashSet<TransformComponent> _hookedComponents = new HashSet<TransformComponent>();

        // 👇 FIX: Track specific components actively being synced to allow deep hierarchy propagation
        private readonly HashSet<TransformComponent> _activeSyncingComponents = new HashSet<TransformComponent>();

        public TransformSystem()
        {
            RequiredComponents = Query.Has<TransformComponent>();
            UsedInEditor = true;
            UpdatePolicy = SystemUpdatePolicy.FrameUpdate;
        }

        public override void Update(HashSet<GameObject> gameObjects, float deltaTime)
        {
            foreach(var entity in gameObjects)
            {
                var transform = entity.GetComponent<TransformComponent>();
                if(transform == null)
                    continue;

                if(!_hookedComponents.Contains(transform))
                {
                    transform.OnTransformChanged += HandleTransformModified;
                    _hookedComponents.Add(transform);
                }

                // Root objects are either completely parentless, or parented directly to the scene wrapper
                if(entity.Parent == null || entity.Parent is GameScene)
                {
                    if(_activeSyncingComponents.Add(transform))
                    {
                        try
                        {
                            SyncTransformHierarchy(entity, transform);
                        }
                        finally
                        {
                            _activeSyncingComponents.Remove(transform);
                        }
                    }
                }
            }
        }

        private void SyncTransformHierarchy(GameObject current, TransformComponent currentTransform)
        {
            if(current.Children == null)
                return;

            var childList = current.GetChildGameObjects();

            foreach(var child in childList)
            {
                if(child == null)
                    continue;

                var childTransform = child.GetComponent<TransformComponent>();
                if(childTransform == null)
                    continue;

                var rawLocalFields = childTransform.LocalPosition;
                var targetWorldX = currentTransform.X + rawLocalFields.X;
                var targetWorldY = currentTransform.Y + rawLocalFields.Y;

                // 💡 FIX: Check if it actually needs an update before touching the property!
                if(Math.Abs(childTransform.X - targetWorldX) < 0.001f &&
                    Math.Abs(childTransform.Y - targetWorldY) < 0.001f)
                {
                    // Already perfectly in sync. Recurse down just in case grandchildren are dirty, 
                    // but skip modifying this child (which prevents event/log spam)
                    SyncTransformHierarchy(child, childTransform);
                    continue;
                }

                bool wasAdded = _activeSyncingComponents.Add(childTransform);
                try
                {
                    childTransform.WorldPosition = new Microsoft.Xna.Framework.Vector2(targetWorldX, targetWorldY);

                    SyncTransformHierarchy(child, childTransform);
                }
                finally
                {
                    if(wasAdded)
                        _activeSyncingComponents.Remove(childTransform);
                }
            }
        }

        private void HandleTransformModified(TransformComponent modifiedTransform)
        {
            var entity = modifiedTransform.gameObject;
            if(entity == null)
                return;

            // 💡 FIX: Check the guard first so down-sync updates don't spam the console
            if(_activeSyncingComponents.Contains(modifiedTransform))
                return;

            
            if(_activeSyncingComponents.Add(modifiedTransform))
            {
                try
                {
                    // If this object is a child of another GameObject, resolve the positioning adjustments
                    if(entity.Parent is GameObject parentGameObject)
                    {
                        var parentTransform = parentGameObject.GetComponent<TransformComponent>();
                        if(parentTransform != null)
                        {
                            if(modifiedTransform.IsSettingOffset)
                            {
                                // Changed via typing explicit local offsets into the Editor inspector -> update world coordinates
                                float newWorldX = parentTransform.X + modifiedTransform.XOffset;
                                float newWorldY = parentTransform.Y + modifiedTransform.YOffset;

                                modifiedTransform.WorldPosition = new Microsoft.Xna.Framework.Vector2(newWorldX, newWorldY);
                            }
                            else
                            {
                                // Moved via Gizmo drag, scene placements, or physics -> recalculate local layout offsets
                                modifiedTransform.XOffset = modifiedTransform.X - parentTransform.X;
                                modifiedTransform.YOffset = modifiedTransform.Y - parentTransform.Y;
                            }
                        }
                    }

                    // Push the updated positions all the way down to any attached sub-children
                    SyncTransformHierarchy(entity, modifiedTransform);
                }
                finally
                {
                    _activeSyncingComponents.Remove(modifiedTransform);
                }
            }
        }
    }
}