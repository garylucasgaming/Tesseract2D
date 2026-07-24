using Engine.Core.ECS;
using Engine.Core.Runtime;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Engine.Core.Runtime
{


    public class GameEvent
    {
        public GameObject? TargetGameObject
        {
            get; set;
        }
        public string TargetComponentTypeName { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;

        
        private Action? _cachedDelegate;

        public void Invoke()
        {
            if(TargetGameObject == null)
                return;

            if(_cachedDelegate != null)
            {
                try
                {
                    _cachedDelegate();
                }
                catch(Exception ex)
                {
                    Utilities.Log.Error($"[GameEvent] Error invoking cached event '{MethodName}': {ex.Message}");
                    ClearCache();
                }
                return;
            }

            if(string.IsNullOrEmpty(TargetComponentTypeName) || string.IsNullOrEmpty(MethodName))
                return;

            try
            {
                var targetComponent = TargetGameObject.Components.Values
                    .FirstOrDefault(c => c.GetType().FullName == TargetComponentTypeName || c.GetType().Name == TargetComponentTypeName);

                if(targetComponent == null)
                    return;

                MethodInfo? methodInfo = targetComponent.GetType().GetMethod(
                    MethodName,
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    Type.EmptyTypes,
                    null
                );

                if(methodInfo != null && methodInfo.ReturnType == typeof(void))
                {
                    _cachedDelegate = (Action) Delegate.CreateDelegate(typeof(Action), targetComponent, methodInfo);
                    _cachedDelegate();
                }
            }
            catch(Exception ex)
            {
                Utilities.Log.Error($"[GameEvent Reflection Error] Failed to bind event method '{MethodName}': {ex.Message}");
            }
        }

        public void ClearCache()
        {
            _cachedDelegate = null;
        }
    }
}
