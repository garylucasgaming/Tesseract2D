using Engine.Core.Runtime;
using Engine.Core.Serialization;
using Engine.Core.Utilities;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Engine.Editor.Utilities
{
    public class ScriptAssemblyManager
    {
        private AssemblyLoadContext? _currentContext;

        private static List<Assembly> _loadedProjectAssemblies = new List<Assembly>();

        public static IReadOnlyList<Assembly> LoadedProjectAssemblies => _loadedProjectAssemblies;
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

            TypeResolver.ClearCache();

            Console.WriteLine($"[ScriptAssemblyManager] Successfully loaded {CurrentAssembly.FullName} into RAM!");
        }

        public static void ReloadProjectAssemblies()
        {
            if(string.IsNullOrEmpty(EditorContextManager.CurrentProjectRoot))
                return;

            string projectRoot = EditorContextManager.CurrentProjectRoot!;
            string projectName = EditorContextManager.CurrentProjectName
                ?? Path.GetFileName(projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            string outputPath = ProjectPaths.GetGameplayOutputDirectory(projectRoot, projectName);
            if(!Directory.Exists(outputPath))
                return;

            _loadedProjectAssemblies.Clear();

            foreach(var dllPath in Directory.GetFiles(outputPath, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    // Read bytes directly into memory to prevent file lock
                    byte[] assemblyBytes = File.ReadAllBytes(dllPath);

                    // If symbol file (.pdb) exists, load it too for full stack trace debugging
                    string pdbPath = Path.ChangeExtension(dllPath, ".pdb");
                    Assembly loadedAssembly;

                    if(File.Exists(pdbPath))
                    {
                        byte[] pdbBytes = File.ReadAllBytes(pdbPath);
                        loadedAssembly = Assembly.Load(assemblyBytes, pdbBytes);
                    }
                    else
                    {
                        loadedAssembly = Assembly.Load(assemblyBytes);
                    }

                    _loadedProjectAssemblies.Add(loadedAssembly);
                }
                catch
                {
                    // Ignore non-.NET or unreadable DLLs
                }
            }

            TypeResolver.ClearCache();
        }


        public void UnloadCurrentAssembly()
        {
            if(_currentContext != null)
            {
                CurrentAssembly = null;
                TypeResolver.ClearCache();
                _currentContext.Unload();
                _currentContext = null;

                // Force Garbage Collection to clean up unloaded types
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}