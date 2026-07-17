using Engine.Core.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Components
{
    public class ScriptComponent : GameComponent
    {
      
        [Browsable(false)]
        public bool hasStarted = false;
        [Browsable(true)]
        public string ScriptTypeName { get; set; }
        [Browsable(false)]

        public string ScriptFilePath { get; set; }
       

        /// <summary>
        /// called the first frame after initialize
        /// </summary>
        public virtual void Start()
        {
        }

        /// <summary>
        /// called every frame if gameobject is active
        /// </summary>
        public virtual void Update()
        {
        }

        /// <summary>
        /// run once when the gameobject is set to active
        /// </summary>
        public virtual void OnEnable()
        {
        }

        /// <summary>
        /// run once when the gameobject is set to inactive
        /// </summary>
        public virtual void OnDisable()
        {

        }

        public GameObject? Instantiate(GameObject go)
        {

            if(gameObject?.ContextScene == null || go == null)
                return null;

            // Assuming your duplicate pass handles serialization deep-copying safely
            // Adjust this to match your deep cloning setup from earlier!
            gameObject.ContextScene.AddGameObject(go);
            return go;

        }
        
       

    }
}
