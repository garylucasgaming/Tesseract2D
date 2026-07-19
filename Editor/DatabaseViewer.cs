using Engine.Core.Collections;
using Engine.Core.ECS.Components;
using Engine.Core.Runtime;
using Engine.Core.Serialization;
using Engine.Core.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp1;



namespace Engine.Editor
{
    public partial class DatabaseViewer : Form
    {

        private DatabaseManager _dbManager = new DatabaseManager();
        private Database _activeDatabase;
        private System.Collections.IList _gridBindingList;

    
        public DatabaseViewer()
        {
            InitializeComponent();
            DatabaseGridView.AllowUserToDeleteRows = false;
            DatabaseGridView.AllowUserToAddRows = false;
            
            SetupViewerEvents();
            RefreshDatabaseLookups();
        }


        private void SetupViewerEvents()
        {
            // Wire up the control triggers
            DatabaseGridView.SelectionChanged += DataGridView1_SelectionChanged;
            DatabaseToolStripComboBox.SelectedIndexChanged += CmbDatabases_SelectedIndexChanged;
        }

        private void btnRemoveRow_Click(object sender, EventArgs e)
        {
            if(_activeDatabase == null || _gridBindingList == null)
                return;

            // 1. Extract the raw object reference from the currently highlighted row
            if(DatabaseGridView.CurrentRow?.DataBoundItem is DataComponent componentToDelete)
            {
                // 2. Friendly confirmation check to prevent accidental keystroke deletions
                string confirmMsg = $"Are you sure you want to delete '{componentToDelete.DisplayName}'?\nThis cannot be undone.";
                var result = MessageBox.Show(confirmMsg, "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if(result == DialogResult.Yes)
                {
                    // 3. CRITICAL: Sever the data link before altering memory arrays
                    DatabaseGridView.DataSource = null;

                    // 4. Purge from both the file serialization dictionary and the UI tracking list
                    _activeDatabase.ComponentDatabase.Remove(componentToDelete.AssetID);
                    _gridBindingList.Remove(componentToDelete);

                    // 5. Reattach data source to cleanly draw the remaining collection
                    DatabaseGridView.DataSource = _gridBindingList;

                    // 6. Reset the property sidebar so it isn't pointing at a deleted asset pointer
                    DatabasePropertyGrid.SelectedObject = null;
                }
            }
            else
            {
                MessageBox.Show("Please select a row in the grid to remove.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void RefreshDatabaseLookups()
        {
            DatabaseToolStripComboBox.Items.Clear();

            // 1. Ask Editor Context where files live, load them into manager tracking memory
            string dbFolder = EditorContextManager.DatabasePath;
            _dbManager.LoadAllDatabasesFromFolder(dbFolder);

            // 2. Load tracked databases straight into dropdown select option list
            foreach(var db in _dbManager.Databases)
            {
                DatabaseToolStripComboBox.Items.Add(db.Name);
            }

            if(DatabaseToolStripComboBox.Items.Count > 0)
                DatabaseToolStripComboBox.SelectedIndex = 0;
        }

        private void CmbDatabases_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedTypeString = DatabaseToolStripComboBox.SelectedItem?.ToString();
            if(string.IsNullOrEmpty(selectedTypeString))
                return;

            _activeDatabase = _dbManager.GetDatabaseByName(selectedTypeString);

            if(_activeDatabase != null)
            {
                string typeName = _activeDatabase.DatabaseType;
                Type compType = Type.GetType($"Engine.Core.ECS.Components.{typeName}, Engine.Core");

                if(compType == null)
                {
                    foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        compType = assembly.GetType(typeName) ?? assembly.GetType($"Game.Scripts.{typeName}");
                        if(compType != null)
                            break;
                    }
                }

                if(compType == null)
                {
                    MessageBox.Show($"Could not resolve type definition for {typeName}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 2. Dynamically create a concrete List<YourDerivedType> via reflection
                Type rawListType = typeof(List<>).MakeGenericType(compType);
                System.Collections.IList internalList = (System.Collections.IList) Activator.CreateInstance(rawListType);
                // 3. Populate it with our existing components
                foreach(var component in _activeDatabase.ComponentDatabase.Values)
                {
                    internalList.Add(component);
                }

                // 4. Wrap that list inside a dynamic BindingList<YourDerivedType> 
                Type bindingListType = typeof(BindingList<>).MakeGenericType(compType);
                _gridBindingList = (System.Collections.IList) Activator.CreateInstance(bindingListType, internalList);

                // 5. Force the DataGridView to completely clear old columns and regenerate new ones
                DatabaseGridView.DataSource = null;
                DatabaseGridView.DataSource = _gridBindingList;

               
                DatabaseToolstripLabel.Text = _activeDatabase.Name;
            }
        }

        private void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            // If the user highlights a row on the spreadsheet, bind its raw reference to the property sidebar
            if(DatabaseGridView.CurrentRow?.DataBoundItem is DataComponent componentRow)
            {
                DatabasePropertyGrid.SelectedObject = componentRow;
            }
        }

        private void btnAddRow_Click(object sender, EventArgs e)
        {
            // 1. Force the grid to finalize any lingering user cell focus
            DatabaseGridView.EndEdit();

            if(_activeDatabase == null || _gridBindingList == null)
                return;

            // Resolve target system class runtime description out of metadata strings
            string typeName = _activeDatabase.DatabaseType;
            Type compType = Type.GetType($"Engine.Core.ECS.Components.{typeName}, Engine.Core");

            if(compType == null)
            {
                foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    compType = assembly.GetType(typeName) ?? assembly.GetType($"Game.Scripts.{typeName}");
                    if(compType != null)
                        break;
                }
            }

            if(compType != null && Activator.CreateInstance(compType) is DataComponent newAsset)
            {
                newAsset.DisplayName = $"New {typeName} Entry";

                // 2. CRITICAL: Sever the UI link to safely mutate memory without a currency manager conflict
                DatabaseGridView.DataSource = null;

                // 3. Append to your database core dictionary and runtime list layout
                _activeDatabase.ComponentDatabase.Add(newAsset.AssetID, newAsset);
                _gridBindingList.Add(newAsset);

                // 4. Re-link the data source. WinForms will cleanly build the new row from scratch
                DatabaseGridView.DataSource = _gridBindingList;

                // 5. UX Polish: Automatically scroll to highlight your newly injected row
                if(DatabaseGridView.Rows.Count > 0)
                {
                    DatabaseGridView.CurrentCell = DatabaseGridView.Rows[DatabaseGridView.Rows.Count - 1].Cells[0];
                }
            }
        }

        private void btnSaveDatabase_Click(object sender, EventArgs e)
        {
            if(_activeDatabase == null)
                return;

            // Construct proper save context destination path using its type signature
            string destinationFile = Path.Combine(EditorContextManager.DatabasePath, $"{_activeDatabase.Name}.database");
            _dbManager.SaveDatabase(_activeDatabase, destinationFile);

            MessageBox.Show($"Successfully saved database configuration to:\n{destinationFile}", "Save Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnNewDatabase_Click(object sender, EventArgs e)
        {
            // Locate concrete custom component properties inside game assemblies
            List<Type> availableComponents = new List<Type>();
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var found = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(DataComponent).IsAssignableFrom(t));
                    availableComponents.AddRange(found);
                }
                catch { }
            }

            using(var dialog = new DatabaseDialog(availableComponents))
            {
                if(dialog.ShowDialog() == DialogResult.OK)
                {
                    if(string.IsNullOrEmpty(dialog.DatabaseFileName))
                    {
                        MessageBox.Show("Database file name cannot be blank.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // 1. Create instance tracking structures
                    Database newDb = new Database
                    {
                        ID = Guid.NewGuid(),
                        DatabaseType = dialog.SelectedComponentType.Name,
                        Name = dialog.DatabaseFileName
                    };

                    // 2. Instantly save structural stub layout file out on local disk
                    string targetPath = Path.Combine(EditorContextManager.DatabasePath, $"{dialog.DatabaseFileName}.database");
                    _dbManager.SaveDatabase(newDb, targetPath);

                    // 3. Re-scan directory contents to smoothly reload view entries inside dropdown list options
                    RefreshDatabaseLookups();
                    DatabaseToolStripComboBox.SelectedItem = newDb.DatabaseType;
                    if(this.Owner is Form1 mainForm)
                    {
                        
                        mainForm.RefreshProjectFolderView();
                    }
                }
            }

            


        }

       

    }
}
