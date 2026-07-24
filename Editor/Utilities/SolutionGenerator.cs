using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Engine.Core.Serialization
{
    public static class SolutionGenerator
    {
        public static void GenerateUserSolution(string projectRoot, string projectName, List<string> selectedPlatforms, string assetsPath, string scriptsPath)
        {
            // 1. Locate the engine root source folder
            string engineInstallDir = GetEngineSourceRoot();

            // 2. Create the Source/Gameplay project directory
            string gameplayDir = Path.Combine(projectRoot, "Source", $"{projectName}.Gameplay");
            Directory.CreateDirectory(gameplayDir);

            // 3. Create Gameplay.csproj referencing Engine.Core.csproj and linking Content/Assets
            string engineCorePath = FindProjectFile(engineInstallDir, "Engine.Core.csproj");
            string relativeEngineCore = Path.GetRelativePath(gameplayDir, engineCorePath);

            // Compute relative paths from the gameplay project directory to the passed asset directories
            string relativeAssetsPath = Path.GetRelativePath(gameplayDir, assetsPath);

            string gameplayCsprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include=""{relativeEngineCore}"" />
  </ItemGroup>

  <!-- Include Content/Assets files so they show up under the project hierarchy in Visual Studio -->
  <ItemGroup>
    <Compile Include=""{relativeAssetsPath}/**/*.cs"" />
  </ItemGroup>
</Project>";

            string gameplayCsprojPath = Path.Combine(gameplayDir, $"{projectName}.Gameplay.csproj");
            File.WriteAllText(gameplayCsprojPath, gameplayCsprojContent);

            // 4. Generate the Master .sln File
            string slnPath = Path.Combine(projectRoot, $"{projectName}.sln");
            StringBuilder slnBuilder = new StringBuilder();

            slnBuilder.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
            slnBuilder.AppendLine("# Visual Studio Version 17");
            slnBuilder.AppendLine("VisualStudioVersion = 17.0.31903.59");
            slnBuilder.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");

            void AddProjectToSln(string displayName, string csprojFileName, string guid)
            {
                string absolutePath = FindProjectFile(engineInstallDir, csprojFileName);

                if(!File.Exists(absolutePath))
                {
                    Console.WriteLine($"[SolutionGenerator Warning] Could not locate project file '{csprojFileName}' under: {engineInstallDir}");
                    return;
                }

                string relPath = Path.GetRelativePath(projectRoot, absolutePath);
                slnBuilder.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{displayName}\", \"{relPath}\", \"{{{guid}}}\"");
                slnBuilder.AppendLine("EndProject");
            }

            void AddLocalProjectToSln(string displayName, string absolutePath, string guid)
            {
                if(!File.Exists(absolutePath))
                {
                    Console.WriteLine($"[SolutionGenerator Warning] Could not locate local project file at: {absolutePath}");
                    return;
                }

                string relPath = Path.GetRelativePath(projectRoot, absolutePath);
                slnBuilder.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{displayName}\", \"{relPath}\", \"{{{guid}}}\"");
                slnBuilder.AppendLine("EndProject");
            }

            // Add Engine and User projects to solution
            AddProjectToSln("Engine.Core", "Engine.Core.csproj", Guid.NewGuid().ToString());
            AddProjectToSln("Engine.Content", "Engine.Content.csproj", Guid.NewGuid().ToString());
            AddLocalProjectToSln($"{projectName}.Gameplay", gameplayCsprojPath, Guid.NewGuid().ToString());

            if(selectedPlatforms.Contains("Desktop"))
                AddProjectToSln("Engine.Desktop", "Engine.Desktop.csproj", Guid.NewGuid().ToString());

            if(selectedPlatforms.Contains("Android"))
                AddProjectToSln("Engine.Android", "Engine.Android.csproj", Guid.NewGuid().ToString());

            if(selectedPlatforms.Contains("iOS"))
                AddProjectToSln("Engine.Ios", "Engine.Ios.csproj", Guid.NewGuid().ToString());

            if(selectedPlatforms.Contains("Web"))
                AddProjectToSln("Engine.Web", "Engine.Web.csproj", Guid.NewGuid().ToString());

            File.WriteAllText(slnPath, slnBuilder.ToString());
        }

        private static string GetEngineSourceRoot()
        {
            DirectoryInfo? dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            while(dir != null)
            {
                if(File.Exists(Path.Combine(dir.FullName, "Tesseract2D.sln")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }

            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private static string FindProjectFile(string rootDir, string csprojFileName)
        {
            var matches = Directory.GetFiles(rootDir, csprojFileName, SearchOption.AllDirectories);
            if(matches.Length > 0)
            {
                return matches[0];
            }

            return Path.Combine(rootDir, csprojFileName);
        }
    }
}