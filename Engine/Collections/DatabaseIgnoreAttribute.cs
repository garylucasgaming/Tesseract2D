using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Collections
{
    /// <summary>
    /// Decorate properties with [DatabaseIgnore] to hide them from the DatabaseViewer grid.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class DatabaseIgnoreAttribute : Attribute
    {
    }
}
