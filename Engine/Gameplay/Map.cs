using Engine.Core.Collections;
using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using Engine.Core.Runtime;
using Engine.Core.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Engine.Core.GamePlay
{
    public class Map
    {
        private int[,] _grid;
        private int _tileSize = 32;
        private string _tileSetPath = string.Empty;
        private string _MapName = "Untitled Map";
        private int _LayerOrder = 0;
        private bool _isEnabled = true;
        private Database _tileDatabase;
        private string _tileDatabaseName = string.Empty;
        private GameScene _contextScene;


        [YamlIgnore]
        private DataComponent[,] _tileDataGrid;

        [YamlIgnore]
        private List<int> _gridFlattened = new List<int>();

        private Dictionary<int, int> _tileProperties = new Dictionary<int, int>();

        private Dictionary<int, DataComponent> _tileIndexDataDictionary = new Dictionary<int, DataComponent>();


        public GameScene ContextScene
        {
            get => _contextScene;
            set
            {
                _contextScene = value;
                ResolveDatabase(_contextScene);
            }
        }
        public string MapName
        {
            get => _MapName;
            set => _MapName = value;
        }
        public int LayerOrder
        {
            get => _LayerOrder;
            set => _LayerOrder = value;
        }

        [Browsable(false)]
        [DatabaseIgnore]
        public int[,] Grid
        {
            get => _grid;
            set => _grid = value;
        }
        [Browsable(false)]
        [DatabaseIgnore]
        public DataComponent[,] TileDataGrid
        {
            get => _tileDataGrid;
            set => _tileDataGrid = value;
        }

        public int Width
        {
            get => _grid.GetLength(0);
            set => Resize(value, Height);
        }
        public int Height
        {
            get => _grid.GetLength(1);
            set => Resize(Width, value);
        }

        public int TileSize
        {
            get => _tileSize;
            set => _tileSize = value;
        }
        public string TileSetPath
        {
            get => _tileSetPath ?? string.Empty;
            set => _tileSetPath = value ?? string.Empty;
        }
        public string TileDatabaseName
        {
            get => _tileDatabaseName ?? string.Empty;
            set
            {
                _tileDatabaseName = value ?? string.Empty;
                ResolveDatabase(_contextScene);
            }
        }

        [Browsable(false)]
        [DatabaseIgnore]
        public Dictionary<int, int> TileProperties
        {
            get => _tileProperties;
            set => _tileProperties = value;
        }
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }


        [Browsable (false)]
        [DatabaseIgnore]
        public Database? TileDatabase
        {
            get => _tileDatabase;
            set => _tileDatabase = value;
        }

        [Browsable(false)]
        [DatabaseIgnore]
        public List<int> GridFlattened
        {
            get
            {
                _gridFlattened.Clear();
                int w = Width;
                int h = Height;

                if(_grid == null || _grid.GetLength(0) != w || _grid.GetLength(1) != h)
                {
                    _grid = new int[w, h];
                }

                // Flatten in Row-Major order (Row by Row)
                for(int y = 0; y < h; y++)
                {
                    for(int x = 0; x < w; x++)
                    {
                        _gridFlattened.Add(_grid[x, y]);
                    }
                }
                return _gridFlattened;
            }
            set
            {
                _gridFlattened = value ?? new List<int>();

                int w = Width;
                int h = Height;

                if(w > 0 && h > 0)
                {
                    _grid = new int[w, h];
                    int index = 0;

                    for(int y = 0; y < h; y++)
                    {
                        for(int x = 0; x < w; x++)
                        {
                            if(index < _gridFlattened.Count)
                            {
                                _grid[x, y] = _gridFlattened[index++];
                            }
                            else
                            {
                                _grid[x, y] = 0; // Default fill if array ends early
                            }
                        }
                    }
                }
            }
        }

        [Browsable(false)]
        [DatabaseIgnore]
        public Dictionary<int, DataComponent> TileIndexDataDictionary
        {
            get => _tileIndexDataDictionary;
            set
            {
                _tileIndexDataDictionary = value ?? new Dictionary<int, DataComponent>();
                RebuildTileDataGrid();
            }
        }

        public Map(int width, int height)
        {
            _tileSize = GameWorldManager.TileSize;
            _grid = new int[width, height];
            _tileDataGrid = new DataComponent[width, height];
        }

        public void Resize(int newWidth, int newHeight)
        {
            var newGrid = new int[newWidth, newHeight];
            var newDataGrid = new DataComponent[newWidth, newHeight];
            int minWidth = Math.Min(_grid.GetLength(0), newWidth);
            int minHeight = Math.Min(_grid.GetLength(1), newHeight);

            for(int x = 0; x < minWidth; x++)
            {
                for(int y = 0; y < minHeight; y++)
                {
                    newGrid[x, y] = _grid[x, y];
                    newDataGrid[x, y] = _tileDataGrid[x, y];
                }
            }
            _grid = newGrid;
            _tileDataGrid = newDataGrid;
        }

        public int GetGridValue(int x, int y)
        {
            if(x >= 0 && x < Width && y >= 0 && y < Height)
                return _grid[x, y];
            return 0;
        }

        public void SetGridValue(int x, int y, int value)
        {
            if(x >= 0 && x < Width && y >= 0 && y < Height)
                _grid[x, y] = value;
        }

        /// <summary>
        /// Queries the grid value at (x, y) and looks up the corresponding tile index key in TileProperties.
        /// </summary>
        public int GetTileAt(int x, int y)
        {
            int gridVal = GetGridValue(x, y);
            foreach(var kvp in _tileProperties)
            {
                if(kvp.Value == gridVal)
                {
                    return kvp.Key;
                }
            }
            return gridVal;
        }

        public Guid GetTileDataId(int x, int y)
        {
            if(x >= 0 && x < Width && y >= 0 && y < Height)
            {
                if(_tileDataGrid[x, y] != null)
                {
                    return _tileDataGrid[x, y].AssetID;
                }
            }
            return Guid.Empty;
        }

        public void SetTileData(int x, int y, DataComponent data)
        {
            if(TileDatabase == null)
                return;
            TileDataGrid [x, y] = data;
        }

     

        public DataComponent? GetTileData(int x, int y )
        {
            Guid dataId = GetTileDataId(x, y);
            if(dataId == Guid.Empty || TileDatabase == null)
                return null;

            return TileDatabase.GetComponent(dataId);
        }

        public void RebuildTileDataGrid()
        {
            

            int width = Width;
            int height = Height;

            // Ensure TileDataGrid matches the int grid dimensions
            if(TileDataGrid == null || TileDataGrid.GetLength(0) != width || TileDataGrid.GetLength(1) != height)
            {
                TileDataGrid = new DataComponent[width, height];
            }

            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    // 1. Get custom int value from map.Grid
                    int customInt = GetGridValue(x, y);

                    // 2. Resolve custom int to Tile Index via TileProperties
                    int tileIndex = customInt;
                    if(TileProperties != null)
                    {
                        foreach(var kvp in TileProperties)
                        {
                            if(kvp.Value == customInt)
                            {
                                tileIndex = kvp.Key;
                                break;
                            }
                        }
                    }

                    // 3. Query TileIndexDataDictionary for assigned DataComponent
                    if(TileIndexDataDictionary != null && TileIndexDataDictionary.TryGetValue(tileIndex, out var templateComponent))
                    {
                        // Instantiate a fresh per-tile copy so each tile can be modified independently at runtime
                        SetTileData(x, y, (DataComponent) templateComponent.Clone());
                    }
                    else
                    {
                       SetTileData(x, y, null);
                    }
                }
            }
        }

        public void ResolveDatabase(GameScene scene = null)
        {
            var activeScene = scene ?? EditorContextManager.ActiveLoadedScene;

            if(activeScene?.Database?.Databases == null || string.IsNullOrEmpty(TileDatabaseName))
            {
                TileDatabase = null;
                return;
            }

            // Extract clean name if TileDatabaseName is a relative path like "Databases/ItemDb.database"
            string cleanName = Path.GetFileNameWithoutExtension(TileDatabaseName).Trim();

            TileDatabase = activeScene.Database.Databases.FirstOrDefault(db =>
                db.Name.Trim().Equals(cleanName, StringComparison.OrdinalIgnoreCase));
            RebuildTileDataGrid();
        }

    

        public int GetCustomValueForTile(int tileIndex)
        {
            if(_tileProperties.ContainsKey(tileIndex))
            {
                return _tileProperties[tileIndex];
            }
            return tileIndex;
        }
    }
}

