using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Editor.Utilities
{
    public static class CodeTemplateGenerator
    {
        public static string CreateTemplate(string destinationDirectory, string language, string templateType, string className)
        {
            className = Path.GetFileNameWithoutExtension(className.Trim());
            string extension = language.ToLower() == "lua" ? ".lua" : ".cs";
            string fileName = $"{className}{extension}";
            string fullFilePath = Path.Combine(destinationDirectory, fileName);

            // 1. Locate the physical template file on disk
            string engineRoot = GetEngineSourceRoot();
            string templateFileName = $"{templateType}.tmpl";
            string templatePath = Path.Combine(engineRoot, "Editor", "Templates", language, templateFileName);

            string fileContent;

            if(File.Exists(templatePath))
            {
                // 2. Read the template file and replace placeholders
                fileContent = File.ReadAllText(templatePath);
                fileContent = fileContent.Replace("{ClassName}", className);
            }
            else
            {
                // Fallback basic text if the template file is missing
                fileContent = "";
            }

            // 3. Write out the final generated file
            File.WriteAllText(fullFilePath, fileContent);
            return fullFilePath;
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
    }
}
