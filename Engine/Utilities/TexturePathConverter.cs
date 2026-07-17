using System;
using System.ComponentModel;

namespace Engine.Core.Utilities
{
    public class TexturePathConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            // Now querying specifically for Texture subfolder keys!
            var availableTextures = AssetManager.GetAvailableKeys(AssetType.Texture);
            return new StandardValuesCollection(availableTextures);
        }
    }
}