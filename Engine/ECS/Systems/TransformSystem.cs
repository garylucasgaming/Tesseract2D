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

        private readonly HashSet<TransformComponent> _hookedComponents = new HashSet<TransformComponent>();


        // 👇 FIX: Reentrancy guard to prevent recursive event loops
        private bool _isInternalSyncRunning = false;

        public TransformSystem()
        {
            RequiredComponents = Query.Has<TransformComponent>();
            UsedInEditor = true;

            
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
            // 1. Check the guard immediately to reject recursive cascading events
            if(_isInternalSyncRunning)
                return;

            var entity = modifiedTransform.gameObject;
            if(entity == null)
                return;

            // 2. Raise the shield BEFORE modifying any properties (offsets) on this component
            _isInternalSyncRunning = true;

            try
            {
                // If this entity has a parent, adjust its local offsets based on its new world position
                if(entity.Parent != null)
                {
                    var parentTransform = entity.Parent.GetComponent<TransformComponent>();
                    if(parentTransform != null)
                    {
                        // 👇 Modifying these now safely triggers NotifyChange(), 
                        // but the guard at Step 1 will instantly block the recursive echo!
                        modifiedTransform.XOffset = modifiedTransform.X - parentTransform.X;
                        modifiedTransform.YOffset = modifiedTransform.Y - parentTransform.Y;
                    }
                }

                // 3. Immediately propagate these changes top-down to any grandchildren
                SyncTransformHierarchy(entity, modifiedTransform);
            }
            finally
            {
                // 4. Safely drop the shield when this specific interaction wave is finished
                _isInternalSyncRunning = false;
            }
        }
    }
}