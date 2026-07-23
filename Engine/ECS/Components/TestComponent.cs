using Engine.Core.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Components
{
    public class TestComponent : GameComponent
    {
        private GameObject _object;
        private GameComponent _component;
        private DataLinkComponent _tile;
        
        public GameObject objectReference
        {
            get => _object;
            set => _object = value;
        }

        public DataLinkComponent tileData
        {
            get => _tile;
            set => _tile = value;
        }
        
        public GameComponent componentReference
        {
            get => _component;
            set=> _component = value;
        }


        public List<GameObject> gameObjectList
        {
            get; set;
        } = [];

        public List<GameComponent> gameComponentList
        {
            get; set;
        } = [];


    }
}
