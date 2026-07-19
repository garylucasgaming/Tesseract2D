using Engine.Core.Collections;
using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using Engine.Core.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Engine.Core.Runtime
{
    public class DatabaseManager
    {
        public GameScene? ContextScene
        {
            get; set;
        }
        public List<Database> Databases { get; set; } = new List<Database>();

        public void SaveDatabase(Database db, string absoluteFilePath)
        {
            DatabaseSerializer.SaveDatabase(db, absoluteFilePath);
        }

        public Database? LoadDatabase(string absoluteFilePath)
        {
            var database = DatabaseSerializer.LoadDatabase(absoluteFilePath);
            if(database == null)
                return null;

            // Use LINQ to check if this database ID is already tracked in our list
            var existingDb = Databases.FirstOrDefault(db => db.ID == database.ID);
            if(existingDb != null)
            {
                // Already loaded! Return the tracking pointer to keep memory references synchronized
                return existingDb;
            }

            Databases.Add(database);
            return database;
        }

        /// <summary>
        /// Scans a directory path, loading every valid YAML database found into tracking memory.
        /// </summary>
        public void LoadAllDatabasesFromFolder(string folderPath)
        {
            if(!Directory.Exists(folderPath))
                return;

            string[] files = Directory.GetFiles(folderPath, "*.database");
            foreach(string file in files)
            {
                LoadDatabase(file);
            }
        }

   

        public Database? GetDatabaseByTypeName(string dataTypeString)
        {
            return Databases.FirstOrDefault(db => db.DatabaseType.Equals(dataTypeString, StringComparison.OrdinalIgnoreCase));
        }

        public Database? GetDatabaseByName(string dataTypeString)
        {
            return Databases.FirstOrDefault(db => db.Name.Equals(dataTypeString, StringComparison.OrdinalIgnoreCase));
        }
    }
}