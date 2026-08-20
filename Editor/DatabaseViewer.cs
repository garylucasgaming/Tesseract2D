using Engine.Core.Collections;
using Engine.Core.ECS.Components;
using Engine.Core.Runtime;
using Engine.Core.Serialization;
using Engine.Core.Utilities;
using Engine.Editor.Utilities;
using Engine.Editor.WinFormsApp1;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private Type _activeComponentType;

        // Store a permanent reference to the sidebar container
        private Control _inspectorContainer;

        public DatabaseViewer()
        {
            InitializeComponent();
            DatabaseGridView.AllowUserToDeleteRows = false;
            DatabaseGridView.AllowUserToAddRows = false;

            // Cache the parent container panel before clearing any design-time controls
            _inspectorContainer = DatabasePropertyGrid?.Parent;

            SetupViewerEvents();
            RefreshDatabaseLookups();
        }

        private void SetupViewerEvents()
        {
            DatabaseGridView.SelectionChanged += DataGridView1_SelectionChanged;
            DatabaseToolStripComboBox.SelectedIndexChanged += CmbDatabases_SelectedIndexChanged;

            // Ensure cell edits in the grid refresh the sidebar card view
            DatabaseGridView.CellValueChanged += (s, e) => UpdatePropertyInspector();
        }

        private void btnRemoveRow_Click(object sender, EventArgs e)
        {
            if(_activeDatabase == null || _gridBindingList == null)
                return;

            if(DatabaseGridView.CurrentRow?.DataBoundItem is DataComponent componentToDelete)
            {
                string confirmMsg = $"Are you sure you want to delete '{componentToDelete.DisplayName}'?\nThis cannot be undone.";
                var result = MessageBox.Show(confirmMsg, "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if(result == DialogResult.Yes)
                {
                    DatabaseGridView.DataSource = null;

                    _activeDatabase.ComponentDatabase.Remove(componentToDelete.AssetID);
                    _gridBindingList.Remove(componentToDelete);

                    DatabaseGridView.DataSource = _gridBindingList;
                    ConfigureGridViewColumns(_activeComponentType);

                    UpdatePropertyInspector();
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

            string dbFolder = EditorContextManager.DatabasePath;
            _dbManager.LoadAllDatabasesFromFolder(dbFolder);

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

                _activeComponentType = compType;

                Type rawListType = typeof(List<>).MakeGenericType(compType);
                System.Collections.IList internalList = (System.Collections.IList) Activator.CreateInstance(rawListType);

                foreach(var component in _activeDatabase.ComponentDatabase.Values)
                {
                    internalList.Add(component);
                }

                Type bindingListType = typeof(BindingList<>).MakeGenericType(compType);
                _gridBindingList = (System.Collections.IList) Activator.CreateInstance(bindingListType, internalList);

                DatabaseGridView.DataSource = null;
                DatabaseGridView.DataSource = _gridBindingList;

                ConfigureGridViewColumns(_activeComponentType);

                DatabaseToolstripLabel.Text = _activeDatabase.Name;

                // Initial inspector render on database switch
                UpdatePropertyInspector();
            }
        }

        private void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            UpdatePropertyInspector();
        }

        /// <summary>
        /// Updates the property inspector panel with a ComponentCard matching the currently selected row.
        /// </summary>
        private void UpdatePropertyInspector()
        {
            if(_inspectorContainer == null)
                return;

            if(_inspectorContainer is ScrollableControl scrollable)
            {
                scrollable.AutoScroll = true;
            }

            _inspectorContainer.Controls.Clear();

            if(DatabaseGridView.CurrentRow?.DataBoundItem is DataComponent componentRow)
            {
                int cardWidth = _inspectorContainer.ClientSize.Width - 10;
                if(cardWidth < 120)
                    cardWidth = 200;

                Panel card = ComponentCardFactory.CreateCard(
                    componentRow.GetType().Name,
                    componentRow,
                    cardWidth,
                    ComponentCardFactory.SelectedComponentInstance
                );

                card.Dock = DockStyle.Top;

                // Hook property edits inside the card so the DataGridView cells refresh automatically
                HookCardPropertyGridEvents(card);

                _inspectorContainer.Controls.Add(card);
            }
        }

        /// <summary>
        /// Wire up PropertyGrid value changes inside the card to repaint the DataGridView row.
        /// </summary>
        private void HookCardPropertyGridEvents(Control parent)
        {
            foreach(Control child in parent.Controls)
            {
                if(child is PropertyGrid propGrid)
                {
                    propGrid.PropertyValueChanged += (s, e) =>
                    {
                        DatabaseGridView.Refresh();
                    };
                }
                if(child.HasChildren)
                {
                    HookCardPropertyGridEvents(child);
                }
            }
        }

        private void ConfigureGridViewColumns(Type compType)
        {
            if(DatabaseGridView.Columns.Count == 0 || compType == null)
                return;

            DatabaseGridView.SuspendLayout();

            var columnsToProcess = DatabaseGridView.Columns.Cast<DataGridViewColumn>().ToList();

            foreach(var col in columnsToProcess)
            {
                if(string.IsNullOrEmpty(col.DataPropertyName))
                    continue;

                PropertyInfo prop = compType.GetProperty(col.DataPropertyName);
                if(prop == null)
                    continue;

                // 💡 HIDE properties decorated with [DatabaseIgnore]
                if(Attribute.IsDefined(prop, typeof(DatabaseIgnoreAttribute)))
                {
                    col.Visible = false;
                    continue;
                }

                Type propType = prop.PropertyType;
                Type underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;

                col.HeaderText = $"{prop.Name} ({underlyingType.Name})";

                if(underlyingType.IsEnum)
                {
                    int colIndex = col.Index;
                    string dataPropName = col.DataPropertyName;
                    string headerText = col.HeaderText;

                    DatabaseGridView.Columns.Remove(col);

                    var comboCol = new DataGridViewComboBoxColumn
                    {
                        DataPropertyName = dataPropName,
                        HeaderText = headerText,
                        ValueType = propType,
                        DataSource = Enum.GetValues(underlyingType),
                        DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
                        FlatStyle = FlatStyle.Flat
                    };

                    DatabaseGridView.Columns.Insert(colIndex, comboCol);
                }
            }

            DatabaseGridView.ResumeLayout();
        }

        private void btnAddRow_Click(object sender, EventArgs e)
        {
            DatabaseGridView.EndEdit();

            if(_activeDatabase == null || _gridBindingList == null)
                return;

            string typeName = _activeDatabase.DatabaseType;
            Type compType = _activeComponentType;

            if(compType == null)
            {
                compType = Type.GetType($"Engine.Core.ECS.Components.{typeName}, Engine.Core");
                if(compType == null)
                {
                    foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        compType = assembly.GetType(typeName) ?? assembly.GetType($"Game.Scripts.{typeName}");
                        if(compType != null)
                            break;
                    }
                }
            }

            if(compType != null && Activator.CreateInstance(compType) is DataComponent newAsset)
            {
                newAsset.DisplayName = $"New {typeName} Entry";

                DatabaseGridView.DataSource = null;

                _activeDatabase.ComponentDatabase.Add(newAsset.AssetID, newAsset);
                _gridBindingList.Add(newAsset);

                DatabaseGridView.DataSource = _gridBindingList;
                ConfigureGridViewColumns(_activeComponentType);

                if(DatabaseGridView.Rows.Count > 0)
                {
                    DatabaseGridView.CurrentCell = DatabaseGridView.Rows[DatabaseGridView.Rows.Count - 1].Cells[0];
                }

                UpdatePropertyInspector();
            }
        }

        private void btnSaveDatabase_Click(object sender, EventArgs e)
        {
            if(_activeDatabase == null)
                return;

            string destinationFile = Path.Combine(EditorContextManager.DatabasePath, $"{_activeDatabase.Name}.database");
            _dbManager.SaveDatabase(_activeDatabase, destinationFile);

            MessageBox.Show($"Successfully saved database configuration to:\n{destinationFile}", "Save Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnNewDatabase_Click(object sender, EventArgs e)
        {
            // 1. Force-load project assemblies into the AppDomain before reflection scan
            ScriptAssemblyManager.ReloadProjectAssemblies();

            List<Type> availableComponents = new List<Type>();

            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    // Use IsAssignableFrom to find all classes inheriting from DataComponent
                    var found = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && typeof(DataComponent).IsAssignableFrom(t));

                    availableComponents.AddRange(found);
                }
                catch(ReflectionTypeLoadException ex)
                {
                    // Safely fetch types that succeeded loading even if some failed
                    var found = ex.Types
                        .Where(t => t != null && t.IsClass && !t.IsAbstract && typeof(DataComponent).IsAssignableFrom(t));

                    availableComponents.AddRange(found!);
                }
                catch
                {
                    // Ignore non-reflectable system or dynamic assemblies
                }
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

                    Database newDb = new Database
                    {
                        ID = Guid.NewGuid(),
                        DatabaseType = dialog.SelectedComponentType.Name,
                        Name = dialog.DatabaseFileName
                    };

                    string targetPath = Path.Combine(EditorContextManager.DatabasePath, $"{dialog.DatabaseFileName}.database");
                    _dbManager.SaveDatabase(newDb, targetPath);

                    RefreshDatabaseLookups();
                    DatabaseToolStripComboBox.SelectedItem = newDb.DatabaseType;
                    if(this.Owner is Form1 mainForm)
                    {
                        mainForm.RefreshProjectFolderView();
                    }
                }
            }
        }

        /// <summary>
        /// Scans the current project's build folder and loads compiled game/script DLLs into memory[cite: 1858, 1860].
                    /// </summary>
        private void EnsureProjectAssembliesLoaded()
        {
            if(string.IsNullOrEmpty(EditorContextManager.CurrentProjectRoot))
                return;

            string binPath = Path.Combine(EditorContextManager.CurrentProjectRoot, "bin");
            if(!Directory.Exists(binPath))
                return;

            // Retrieve all compiled assembly files in the project's output path [cite: 1860]
            foreach(var dllPath in Directory.GetFiles(binPath, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    var assemblyName = System.Reflection.AssemblyName.GetAssemblyName(dllPath);

                    // Check if assembly isn't loaded into AppDomain yet [cite: 1861]
                    if(!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName == assemblyName.FullName))
                    {
                        System.Reflection.Assembly.LoadFrom(dllPath); // Load into memory [cite: 1861]
                    }
                }
                catch
                {
                    // Ignore non-.NET or unreadable DLLs
                }
            }
        }
    }
}