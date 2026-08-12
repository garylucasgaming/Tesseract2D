using Engine.Core.GamePlay;
using Engine.Core.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Gameplay
{
    public class TileMapRenderSystem
    {
        // Cache loaded textures to prevent reloading them every frame
        private readonly Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();


        public void Render(SpriteBatch sb, ContentManager cm, IEnumerable<Map> maps)
        {
            if(maps == null || sb == null || cm == null)
                return;

            var sortedMaps = maps.OrderBy(m => m.LayerOrder).ToList();

            foreach(var map in sortedMaps)
            {
                if(string.IsNullOrEmpty(map.TileSetPath) || map.Grid == null || !map.IsEnabled)
                    continue;

                Texture2D tilesetTexture = GetOrLoadTexture(cm, map.TileSetPath);
                if(tilesetTexture == null)
                    continue;

                int tileSize = map.TileSize > 0 ? map.TileSize : 32;
                int textureColumns = tilesetTexture.Width / tileSize;
                if(textureColumns <= 0)
                    textureColumns = 1;

                int width = map.Width;
                int height = map.Height;

                for(int x = 0; x < width; x++)
                {
                    for(int y = 0; y < height; y++)
                    {
                        // The grid stores the logical Custom Int value
                        int gridVal = map.Grid[x, y];

                        // No fallback: initialize to an invalid tile index
                        int tileIndex = -1;

                        // Iterate through TileProperties where Key = Tile Index and Value = Custom Int
                        if(map.TileProperties != null)
                        {
                            foreach(var kvp in map.TileProperties)
                            {
                                // Find the entry where the Value matches the grid's custom int
                                if(kvp.Value == gridVal)
                                {
                                    tileIndex = kvp.Key; // The Key is the actual spritesheet tile index to draw
                                    break;
                                }
                            }
                        }

                        // If no matching mapping was found, draw nothing for this grid cell
                        if(tileIndex < 0)
                            continue;

                        int tileX = (tileIndex % textureColumns) * tileSize;
                        int tileY = (tileIndex / textureColumns) * tileSize;

                        var sourceRect = new Rectangle(tileX, tileY, tileSize, tileSize);
                        var destRect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);

                        sb.Draw(tilesetTexture, destRect, sourceRect, Color.White);
                    }
                }
            }
        }
        private Texture2D GetOrLoadTexture(ContentManager cm, string assetPath)
        {
            if(string.IsNullOrEmpty(assetPath))
                return null;

            // MonoGame ContentManager expects just the asset name without folder paths or extensions[cite: 20]
            string cleanPath = Path.GetFileNameWithoutExtension(assetPath);
            var relativePath = AssetManager.GetContentRelativePath(cleanPath, AssetType.Texture);

            if(_textureCache.TryGetValue(relativePath, out var cachedTexture))
            {
                return cachedTexture;
            }

            try
            {
                var texture = cm.Load<Texture2D>(relativePath);
                _textureCache[relativePath] = texture;
                return texture;
            }
            catch(Exception ex)
            {
                Log.Error($"[TileMapRenderSystem] Failed to load tileset '{relativePath}': {ex.Message}");
                return null;
            }
        }
    }
}

