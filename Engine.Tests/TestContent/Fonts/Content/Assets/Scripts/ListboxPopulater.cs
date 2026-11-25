using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using Engine.Core.ECS.Components.UI;
using Engine.Core.Runtime;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    public class ListboxPopulater : ScriptComponent
    {

        public List<string> names = new List<string>();
    public ListBoxComponent? listbox
    {
        get;
        set;
    } = null;
       
        public override void OnEnable()
        {
           
        }

        //this is called whenever the script component is set to not active. 
        public override void OnDisable()
        {
           
        }


        //this is called the first frame of update. you can also check the field HasStarted if you'd like. 
        public override void Start()
        {
           populateNames();
            listbox = gameObject.ContextScene.Entities.FindByTag("Listbox")?.GetComponent<ListBoxComponent>();
            if(listbox != null)
            {
            listbox.Clear();
               foreach(var name in names)
                {
                    listbox.AddItem(name);
                }
            }

    }

        //this is called every frame, in deltatime. 
        public override void Update()
        {
            
        }

    public void populateNames()
    {
        names.Add("John");
        names.Add("Jane");
        names.Add("Alice");
        names.Add("Bob");
        names.Add("Charlie");
    }


    }

