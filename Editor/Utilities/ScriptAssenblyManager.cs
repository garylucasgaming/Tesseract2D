using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Engine.Editor.Utilities
{
    public class ScriptAssemblyManager
    {
        private AssemblyLoadContext? _currentContext;
        public Assembly? CurrentAssembly
        {
            get; private set;
        }

        public void LoadGameplayAssembly(string dllPath)
        {
            // 1. Unload existing collectible context if present
            UnloadCurrentAssembly();

            string pdbPath = Path.ChangeExtension(dllPath, ".pdb");

            // 2. Read DLL and PDB bytes into memory streams (Prevents file locks on disk!)
            byte[] dllBytes = File.ReadAllBytes(dllPath);
            byte[]? pdbBytes = File.Exists(pdbPath) ? File.ReadAllBytes(pdbPath) : null;

            // 3. Create a collectible AssemblyLoadContext
            _currentContext = new AssemblyLoadContext($"GameplayContext_{Guid.NewGuid()}", isCollectible: true);

            using(MemoryStream dllStream = new MemoryStream(dllBytes))
            using(MemoryStream? pdbStream = pdbBytes != null ? new MemoryStream(pdbBytes) : null)
            {
                // Load assembly with debug symbols
                CurrentAssembly = _currentContext.LoadFromStream(dllStream, pdbStream);
            }

            Console.WriteLine($"[ScriptAssemblyManager] Successfully loaded {CurrentAssembly.FullName} into RAM!");
        }

        public void UnloadCurrentAssembly()
        {
            if(_currentContext != null)
            {
                CurrentAssembly = null;
                _currentContext.Unload();
                _currentContext = null;

                // Force Garbage Collection to clean up unloaded types
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}