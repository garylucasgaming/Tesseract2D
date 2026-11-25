using System;
using Engine.Core.Utilities;

namespace Engine.Core.Serialization
{
    /// <summary>
    /// Shared schema version handling for the YAML DTOs. A missing Version member
    /// deserializes to Current, so legacy files stay loadable without a migration.
    /// </summary>
    public static class SerializationVersion
    {
        public const int Current = 1;

        public static void Validate(int version, string format, string filePath)
        {
            if(version > Current)
            {
                throw new SerializationVersionException(
                    $"[Serialization] {format} '{filePath}' uses Version {version}; this build supports up to Version {Current}.");
            }

            if(version < Current)
            {
                Log.Warning($"[Serialization] {format} '{filePath}' uses Version {version} (current is {Current}); loading with current rules.");
            }
        }
    }
}
