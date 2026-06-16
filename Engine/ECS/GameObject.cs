using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS
{
    public class GameObject
    {

        public Guid id { get;  set; } = Guid.NewGuid();
        public string name { get; set; } = "Game Object";
        public bool isActive { get; set; } = true;
        public List<GameComponent> Components { get; set; } = new List<GameComponent>();

        public void AddComponent<T>(T component) where T : GameComponent
        {
       
        }

        public T? GetComponent<T>() where T : GameComponent
        {
            return Components.OfType<T>().FirstOrDefault();
        }

        public bool HasComponent<T>() where T : GameComponent
        {
            return Components.OfType<T>().Any();
        }

        public void RemoveComponent(GameComponent component)
        {
            if(Components.Contains(component))
            {
                component.Owner = null;
                Components.Remove(component);
            }
        }
    }
}

