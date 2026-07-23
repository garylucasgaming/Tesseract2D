using Engine.Core.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Engine.Core.ECS.Components
{
    public abstract class DataComponent : GameComponent
    {

        public abstract string DisplayName
        {
            get; set;
        }

        [Browsable(false)]
        public Guid AssetID
        {
            get; set;
        } = Guid.NewGuid();


        // ==========================================
        // DATA LINK REFERENCE FIELDS
        // ==========================================

        
        [DisplayName("Database Reference")]
        public Database DatabaseReference
        {
            get;
            set;
        }

       
        [DisplayName("Data Reference")]
        public DataComponent? DataReference
        {
            get; set;
        }


    }
}
