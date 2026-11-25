using System;

namespace Engine.Core.Serialization
{
    /// <summary>
    /// Raised when a file advertises a schema version this build cannot load.
    /// Never swallowed by the loader's best-effort recovery catches.
    /// </summary>
    public sealed class SerializationVersionException : InvalidOperationException
    {
        public SerializationVersionException(string message) : base(message)
        {
        }
    }
}
