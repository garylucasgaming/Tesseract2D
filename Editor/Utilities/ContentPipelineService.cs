using Engine.Core.Serialization;
using Engine.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Editor.Utilities
{
    public class ContentPipelineService
    {

        public static void ProcessAsset(string sourceFilePath)
        {
            string ext = Path.GetExtension(sourceFilePath);
            string processor = GetProcessorForExtension(ext);

            if(processor == "None")
                return;

            // Add entry to MGCB
            AddToMgcb(sourceFilePath, processor);

            // Build the project
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"mgcb /platform:DesktopGL /build:Content/Content.mgcb",
                WorkingDirectory = EditorContextManager.CurrentProjectRoot,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using(var process = System.Diagnostics.Process.Start(psi))
            {
                process.WaitForExit();
                Log.Info($"[Content Pipeline] Build complete for: {Path.GetFileName(sourceFilePath)}");
            }
        }


        private static string GetProcessorForExtension(string extension)
        {
            return extension.ToLower() switch
            {
                ".png" or ".jpg" or ".tga" => "TextureProcessor",
                ".wav" or ".ogg" or ".mp3" => "SoundEffectProcessor",
                ".ttf" or ".otf" => "FontDescriptionProcessor",
                _ => "None"
            };
        }

        private static void AddToMgcb(string sourceFilePath, string processor)
        {
            string mgcbPath = Path.Combine(EditorContextManager.CurrentProjectRoot, "Content", "Content.mgcb");
            string relativePath = Path.GetRelativePath(Path.GetDirectoryName(mgcbPath), sourceFilePath);

            // Read the current file content to check for duplicates
            string currentContent = File.ReadAllText(mgcbPath);
            if(currentContent.Contains(relativePath))
                return; // Already registered, skip it

            string outputPath = $"Content/Bin/{Path.GetFileNameWithoutExtension(sourceFilePath)}.xnb";

            string entry = $@"
                #begin {relativePath}
                /importer:{GetImporterForExtension(Path.GetExtension(sourceFilePath))}
                /processor:{processor}
                /processorParam:ColorKeyColor=255,0,255,255
                /processorParam:ColorKeyEnabled=True
                /build:{outputPath}
";
            File.AppendAllText(mgcbPath, entry);
        }

        private static List<string> _pendingFiles = new List<string>();

        public static void QueueAsset(string sourceFilePath)
        {
            _pendingFiles.Add(sourceFilePath);
        }

        public static void RunBatchBuild()
        {
            if(_pendingFiles.Count == 0)
                return;

            foreach(var file in _pendingFiles)
            {
                string ext = Path.GetExtension(file).ToLower();
                string processor = GetProcessorForExtension(ext);

                if(processor == "None")
                {
                    Log.Warning($"[Content Pipeline] No known processor for extension: {ext}. Skipping {Path.GetFileName(file)}");
                    continue;
                }

                AddToMgcb(file, processor);
            }

            ExecuteBuild();
            _pendingFiles.Clear();
        }

        private static void ExecuteBuild()
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "mgcb /platform:DesktopGL /build:Content/Content.mgcb",
                WorkingDirectory = EditorContextManager.CurrentProjectRoot,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using(var process = System.Diagnostics.Process.Start(psi))
            {
                process.WaitForExit();
                Log.Info("[Content Pipeline] Batch build complete.");
            }
        }

        // (Add the AddToMgcb and GetProcessorForExtension methods here)


        private static string GetImporterForExtension(string extension)
        {
            return extension.ToLower() switch
            {
                // Textures
                ".png" or ".jpg" or ".jpeg" or ".tga" or ".bmp" or ".dds" or ".hdr" or ".pfm" or ".ppm" => "TextureImporter",

                // Audio
                ".wav" => "WavImporter",

                ".mp3" => "Mp3Importer",

                ".ogg" => "OggImporter",

                ".wma" => "WmaImporter",

                //video
                ".wmv" => "WmvImporter",

                ".mp4" => "H264Importer",

                // Fonts
                ".spritefont" => "FontDescriptionImporter",

                //autodesk 3d model importer
                ".fbx" => "FbxImporter",

                //other 3D Models
                ".3ds" or ".blend" or ".dae" or ".obj" => "OpenAssetImporter",

                // Effects (Shaders)
                ".fx" => "EffectImporter",

                // XML Data
                ".xml" => "XmlImporter",

                //direct X
                ".x" => "XImporter",

                // Fallback: If you don't want to process it, you can sometimes use 
                // a specific processor that just copies the file ("PassThrough")
                _ => "TextureImporter"
            };
        }

    }
}
