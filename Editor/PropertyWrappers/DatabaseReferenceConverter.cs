using Engine.Core.Collections;
using Engine.Core.Serialization;
using Engine.Editor.WinFormsApp1;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.ComponentModel.TypeConverter;

namespace Engine.Editor.PropertyWrappers
{
    public class DatabaseReferenceConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<object> options = new List<object> { null };

            object realTarget = GetRealInstance(context);
            Type? componentType = realTarget?.GetType();

            if(EditorContextManager.ActiveLoadedScene?.Database != null)
            {
                var dbManager = EditorContextManager.ActiveLoadedScene.Database;
                foreach(var db in dbManager.Databases)
                {
                    bool isMatch = false;

                    if(componentType != null)
                    {
                        if(!string.IsNullOrEmpty(db.DatabaseType) &&
                            (db.DatabaseType.Equals(componentType.Name, StringComparison.OrdinalIgnoreCase) ||
                             db.DatabaseType.Equals(componentType.FullName, StringComparison.OrdinalIgnoreCase)))
                        {
                            isMatch = true;
                        }
                        else if(db.ComponentDatabase.Values.Any(comp => comp != null && componentType.IsAssignableFrom(comp.GetType())))
                        {
                            isMatch = true;
                        }
                    }

                    if(isMatch || string.IsNullOrEmpty(db.DatabaseType))
                    {
                        if(!options.Contains(db))
                        {
                            options.Add(db);
                        }
                    }
                }
            }

            return new StandardValuesCollection(options);
        }

        private object GetRealInstance(ITypeDescriptorContext context)
        {
            object instance = context?.Instance;
            if(instance is FilteredPropertyWrapper wrapper)
                return wrapper.Target;
            return instance;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if(destinationType == typeof(string))
            {
                if(value is Database db)
                {
                    return string.IsNullOrWhiteSpace(db.Name) ? $"Database ({db.ID})" : db.Name;
                }
                return "None (Database)";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if(value is string str)
            {
                str = str.Trim();
                if(string.IsNullOrEmpty(str) || str.StartsWith("None", StringComparison.OrdinalIgnoreCase))
                    return null;

                var choices = GetStandardValues(context);
                foreach(object choice in choices)
                {
                    if(choice is Database db)
                    {
                        string dbName = string.IsNullOrWhiteSpace(db.Name) ? $"Database ({db.ID})" : db.Name;
                        if(dbName.Equals(str, StringComparison.OrdinalIgnoreCase) || (db.Name != null && db.Name.Contains(str, StringComparison.OrdinalIgnoreCase)))
                            return db;
                    }
                }

                return null;
            }
            return null;
        }
    }

}
