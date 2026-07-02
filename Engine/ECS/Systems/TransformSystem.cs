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
        public override SystemUpdatePolicy UpdatePolicy => SystemUpdatePolicy.FrameUpdate;

        private readonly HashSet<TransformComponent> _hookedComponents = new();

        // 👇 FIX: Reentrancy guard to prevent recursive event loops
        private bool _isInternalSyncRunning = false;

        public TransformSystem()
        {
            RequiredComponents = Query.Has<TransformComponent>();
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

                if(entity.Parent == null)
                {
                    // 👇 Raise the shield before updating the hierarchy
                    _isInternalSyncRunning = true;
                    try
                    {
                        SyncTransformHierarchy(entity, transform);
                    }
                    finally
                    {
                        _isInternalSyncRunning = false;
                    }
                }
            }
        }

        private void SyncTransformHierarchy(GameObject current, TransformComponent currentTransform)
        {
            if(current.Children == null)
                return;

            foreach(var child in current.Children)
            {
                var childTransform = child.GetComponent<TransformComponent>();
                if(childTransform == null)
                    continue;

                var rawLocalFields = childTransform.LocalPosition;

                var targetWorldX = currentTransform.X + rawLocalFields.X;
                var targetWorldY = currentTransform.Y + rawLocalFields.Y;

                // Modifying this property triggers OnTransformChanged, but the guard will catch it now!
                childTransform.WorldPosition = new Microsoft.Xna.Framework.Vector2(targetWorldX, targetWorldY);

                SyncTransformHierarchy(child, childTransform);
            }
        }

        private void HandleTransformModified(TransformComponent modifiedTransform)
        {
            // 👇 FIX: If the system is currently calculating changes, ignore the reactive ripple
            if(_isInternalSyncRunning)
                return;

            var entity = modifiedTransform.gameObject;
            if(entity == null)
                return;

            if(entity.Parent != null)
            {
                var parentTransform = entity.Parent.GetComponent<TransformComponent>();
                if(parentTransform != null)
                {
                    modifiedTransform.XOffset = modifiedTransform.X - parentTransform.X;
                    modifiedTransform.YOffset = modifiedTransform.Y - parentTransform.Y;
                }
            }

            // Raise the shield while propagating manual adjustments from editor grids downwards
            _isInternalSyncRunning = true;
            try
            {
                SyncTransformHierarchy(entity, modifiedTransform);
            }
            finally
            {
                _isInternalSyncRunning = false;
            }
        }
    }
}