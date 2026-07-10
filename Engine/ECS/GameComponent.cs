using Engine.Core.ECS;
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


        
    }
}
