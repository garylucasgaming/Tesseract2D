using Engine.Core.Serialization;
using Engine.Core.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.ComponentModel.TypeConverter;

namespace Engine.Editor.PropertyWrappers
{
    public class SpriteFontReferenceConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true; // Restricts typing to existing fonts only

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<object> fontChoices = new List<object> { "None" };

            if(EditorContextManager.IsProjectLoaded)
            {
                try
                {
                    string rootPath = EditorContextManager.CurrentProjectRoot!;

                    if(Directory.Exists(rootPath))
                    {
                        // Recursively find all .spritefont files anywhere in the project
                        var fontFiles = Directory.GetFiles(rootPath, "*.spritefont", SearchOption.AllDirectories);
                        foreach(var file in fontFiles)
                        {
                            // Extract just the file name without extension (e.g., "myfont" from "Content/UI/Fonts/myfont.spritefont")
                            string fontName = Path.GetFileNameWithoutExtension(file);

                            if(!fontChoices.Contains(fontName))
                            {
                                fontChoices.Add(fontName);
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    Log.Error($"[Font Converter Error] Failed to scan project fonts: {ex.Message}");
                }
            }

            return new StandardValuesCollection(fontChoices);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if(destinationType == typeof(string))
            {
                if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
                    return "None";

                string valStr = value.ToString()!;

                // Ensure we strip the extension if an absolute or raw path ever gets passed in
                if(valStr.EndsWith(".spritefont", StringComparison.OrdinalIgnoreCase))
                {
                    valStr = valStr.Substring(0, valStr.Length - ".spritefont".Length);
                }

                return valStr;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if(value is string str)
            {
                if(string.IsNullOrEmpty(str) || str.Equals("None", StringComparison.OrdinalIgnoreCase))
                    return string.Empty;

                // Strip extension on selection/assignment as well
                if(str.EndsWith(".spritefont", StringComparison.OrdinalIgnoreCase))
                {
                    str = str.Substring(0, str.Length - ".spritefont".Length);
                }

                return str;
            }

            return string.Empty;
        }
    }
}
