using Engine.Core.ECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS
{
    public abstract class GameComponent
    {

        public GameObject? Owner
        {
            get; internal set;
        }
    }
}
