using Microsoft.Xna.Framework.Content.Pipeline;
using MonoGame.Framework.Content.Pipeline.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Content.Builder
{
    public class DynamicBuilder : ContentBuilder
    {
        private string _targetAssetFolder;

        public void Initialize(string projectAssetFolder)
        {
            _targetAssetFolder = projectAssetFolder;
        }

        public ContentBuilderParams contentCollectionArgs = new ContentBuilderParams()
        {
            Mode = ContentBuilderMode.Builder,
            WorkingDirectory = $"{AppContext.BaseDirectory}../../../", // path to where your content folder can be located
            SourceDirectory = "Engine.Content/Assets", // Not actually needed as this is the default, but added for reference
            Platform = TargetPlatform.DesktopGL

        };

        public override IContentCollection GetContentCollection()
        {
            var content = new ContentCollection();
            // Use the dynamically injected path
            content.Include<WildcardRule>("*");
            //start by including everything
            content.Include<WildcardRule>("");
            content.Include<WildcardRule>("*.spritefont", file => Path.GetFileNameWithoutExtension(file));

            //exlcudes
            content.Exclude<WildcardRule>("*.git");
            content.Exclude<WildcardRule>("*.svn");

            //include copy
            content.IncludeCopy<WildcardRule>("*.yml");
            

            content.IncludeCopy<WildcardRule>("*.cs");

            //content.IncludeCopy<WildcardRule>("*.lua");

            content.IncludeCopy<WildcardRule>("*.scene");

            content.IncludeCopy<WildcardRule>("*.prefab");

            content.IncludeCopy<WildcardRule>("*.data");
            return content;
        }
    }
    
}
