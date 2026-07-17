using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.ComponentModel.TypeConverter;

namespace Engine.Core.Utilities
{
    public class AudioPathConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            // Instantly hooks into the Audio folder!
            var availableAudio = AssetManager.GetAvailableKeys(AssetType.Audio);
            return new StandardValuesCollection(availableAudio);
        }
    }
}
