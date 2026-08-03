using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Engine.Core.ECS
{
    public abstract class GameComponent
    {

        private bool _isEnabled = false;

        [Browsable(false)]
        public GameObject? gameObject
        {
            get;  set;
        }

        [Browsable(false)]
        public GameObject? Parent
        {
            get
            {
                if(gameObject != null)
                {
                    return gameObject.Parent;
                }
                else
                {
                    return null;
                }

            }
            set
            {
            }
        }

        [Browsable(false)]
        [Ignore]
        public bool isEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                if(_isEnabled == true)
                {
                    OnEnabled();
                }
                else if(_isEnabled == false)
                {
                    OnDisabled();
                }
                   
            }
        }

    


        public virtual void OnEnabled()
        {
        }

        public virtual void OnDisabled()
        {
        }



    }
}
