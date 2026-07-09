using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using Engine.Core.ECS;
using Engine.Core.Serialization;
using Engine.Core.Utilities;
using SharpDX.WIC;
using Editor;
using Engine.Editor;

namespace WinFormsApp1
{

    public partial class Form1 : Form
    {
        // --- Windows Native API Hooks (Shell Icon & Theme Extraction) ---
        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);
        private readonly System.Collections.Generic.List<(LogSeverity Severity, string Message)> _masterLogHistory =
             new System.Collections.Generic.List<(LogSeverity, string)>();
        public static TreeView ActiveHierarchyTreeView
        {
            get; private set;
        }

        public static GroupBox ActiveInspectorPanel
        {
            get; private set;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }




        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

        private ImageList _systemImageList = new ImageList();

        public Form1()
        {

            Log.OnLogMessage += (severity, message) =>
            {
                AppendMessageToConsoleBox(severity, message);
            };
            InitializeComponent();

            Log.Info("[Editor UI] Initializing editor main form...");
            SetTreeViewTheme(ProjectFolderTreeView.Handle);
            InitializeExplorerIcons();

            InitializeProjectExplorerMenus();
            InitializeSceneHierarchyMenus();
            ActiveHierarchyTreeView = this.SceneHierarchyTreeView;
            ActiveInspectorPanel = this.PropertiesWindow;
            UpdateEditorTitle();
            InitializePropertiesToolstripEvents();


            Log.Print("Print test for color reading");
            Log.Info("Info test for color reading");
            Log.Warning("Warning test for color reading");
            Log.Error("Error test for color reading");

        }

        private void TextSearchBarControl1_TextChanged(object sender, EventArgs e)
        {

        }




        public static void SetTreeViewTheme(IntPtr treeHandle)
        {
            SetWindowTheme(treeHandle, "explorer", null);
        }

        private void UpdateEditorTitle()
        {
            if(EditorContextManager.IsProjectLoaded)
            {
                string projectName = Path.GetFileName(EditorContextManager.CurrentProjectRoot);
                this.Text = $"Custom 2D Game Engine Editor - Project: [{projectName}] ({EditorContextManager.CurrentProjectRoot})";
            }
            else
            {
                this.Text = "Custom 2D Game Engine Editor - No Project Active";
            }
        }

        private void OnProjectLoaded()
        {
            UpdateEditorTitle();

            if(EditorContextManager.IsProjectLoaded)
            {
                // 1. Build the path where your default scene's  file should live


                // TODO   set the context managers project manfiest to the loaded project manifest file.
                string targetScenePath = Path.Combine(EditorContextManager.CurrentProjectRoot, "Content", "Scenes", "Default Sandbox.yml");

                // 2. CHECK: If the file exists, load it! Otherwise, fall back to generating a clean slate template.
                if(File.Exists(targetScenePath))
                {
                    try
                    {
                        Log.Info($"[Editor UI] Found existing workspace state file. Deserializing active layout tree...");

                        // Read the file structure straight back into memory



                        // TODO parse the loaded scenes entities into loadedScene?



                        //toml serialization. 
                        var loadedScene = new GameScene();
                        loadedScene = SceneSerializer.LoadScene(targetScenePath);

                        //gism serialization
                        //GameScene loadedScene = GISMSceneSerializer.LoadScene(targetScenePath);


                        // Set the context and populate your UI nodes with the genuine saved data
                        EditorContextManager.ActiveLoadedScene = loadedScene;
                        UpdateSceneHierarchyTitle(EditorContextManager.ActiveLoadedScene.SceneName);
                        PopulateSceneHierarchyTree(SceneHierarchyTreeView, loadedScene);
                    }
                    catch(Exception ex)
                    {
                        Log.Error($"[Editor UI Error] Scene file was found but failed to deserialize. Falling back to default layout. Reason: {ex.Message}");
                        LoadDefaultSandboxScene();
                    }
                }
                else
                {
                    Log.Info("[Editor UI] No existing workspace scene file detected. Generating baseline sandbox template...");
                    LoadDefaultSandboxScene();
                }
            }
            PopulateProjectExplorerTree(ProjectFolderTreeView);
        }

        private void UpdateSceneHierarchyTitle(string sceneName)
        {
            SceneHierarchyPanel.Text = SceneHierarchyPanel.Text + ": '" + sceneName + "'";
        }

        public static void RefreshComponentInspector(object targetComponent)
        {
            if(targetComponent == null || ActiveInspectorPanel == null)
                return;

            // Call a recursive worker to handle any nested container depths safely
            FindAndRefreshGrid(ActiveInspectorPanel, targetComponent);
        }

        private static bool FindAndRefreshGrid(Control parent, object targetComponent)
        {
            foreach(Control child in parent.Controls)
            {
                // 1. Check if this control is a PropertyGrid and matches our instance
                if(child is PropertyGrid grid && grid.Tag == targetComponent)
                {
                    grid.Refresh();
                    return true; // Match found and repainted, bubble out!
                }

                // 2. If it's a container holding controls, drill down into it
                if(child.HasChildren)
                {
                    if(FindAndRefreshGrid(child, targetComponent))
                    {
                        return true; // Propagation short-circuit
                    }
                }
            }
            return false;
        }
        private string PromptUserForProjectName()
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Enter Project Name",
                StartPosition = FormStartPosition.CenterParent
            };
            Label textLabel = new Label() { Left = 20, Top = 20, Text = "Folder Name:", Width = 150 };
            TextBox textBox = new TextBox() { Left = 20, Top = 45, Width = 340 };
            Button confirmation = new Button() { Text = "Ok", Left = 280, Width = 80, Top = 80, DialogResult = DialogResult.OK };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : string.Empty;
        }

        // --- Core Global Menu Strip Items ---
        private void onCreateProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using(var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select the parent directory where you want to create your game project folder.";
                folderDialog.ShowNewFolderButton = true;

                if(folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string projectName = PromptUserForProjectName();
                    if(string.IsNullOrWhiteSpace(projectName))
                        return;

                    try
                    {
                        string projectRootPath = ProjectDirectoryFactory.CreateNewProject(folderDialog.SelectedPath, projectName);
                        EditorContextManager.OpenProjectContext(projectRootPath);
                        OnProjectLoaded();
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show($"Failed to initialize project: {ex.Message}", "Project Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OpenProjectSettings(object sender, EventArgs e)
        {
        }

        private void onLoadProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using(var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select your project root folder.";
                folderDialog.ShowNewFolderButton = false;

                if(folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string targetFolder = folderDialog.SelectedPath;
                    string manifestCheck = Path.Combine(targetFolder, "Content", "ProjectManifest.db");

                    if(!File.Exists(manifestCheck))
                    {
                        Log.Error("The selected folder is not a valid engine project.");
                        return;
                    }

                    EditorContextManager.OpenProjectContext(targetFolder);
                    OnProjectLoaded();
                }
            }
        }

        private void LoadScene()
        {
        }

        public static void SaveScene()
        {
            if(!EditorContextManager.IsProjectLoaded)
            {
                Log.Warning("No active project workspace is currently open.");
                return;
            }

            if(EditorContextManager.ActiveLoadedScene == null)
            {
                Log.Warning("[Editor UI] Save aborted: There is no active scene context loaded to persist.");
                return;
            }

            try
            {
                Log.Info("[Editor UI] Initiating scene hierarchy persistence pipeline...");

                // 1. Build the path matching your project context rules
                string sceneFileName = $"{EditorContextManager.ActiveLoadedScene.SceneName}.yml";
                //string GISMFileName = $"{EditorContextManager.ActiveLoadedScene.SceneName}.gism";
                //string GISMTargetScenePath = Path.Combine(EditorContextManager.CurrentProjectRoot, "Content", "Scenes", GISMFileName);
                string targetScenePath = Path.Combine(EditorContextManager.CurrentProjectRoot, "Content", "Scenes", sceneFileName);

                // 2. Ensure directories exist safely on disk
                string directoryCheck = Path.GetDirectoryName(targetScenePath);
                if(!string.IsNullOrEmpty(directoryCheck) && !Directory.Exists(directoryCheck))
                {
                    Directory.CreateDirectory(directoryCheck);
                }

                // 3.  EXECUTE YOUR EXACT NATIVE ENGINE SERIALIZER 
                // We pass the live scene layout and target destination directly
                //GISMSceneSerializer.SaveScene(EditorContextManager.ActiveLoadedScene, GISMTargetScenePath);
                SceneSerializer.SaveScene(EditorContextManager.ActiveLoadedScene, targetScenePath);

                Log.Info($"Project workspace and active scene layout saved successfully.");
            }
            catch(Exception ex)
            {
                // Failures during save are already logged by SceneSerializer, but this provides a UI safety fallback
                Log.Warning($"Failed to save project layout safely to disk:\n{ex.Message}");
            }
        }





        public void onSaveProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(!EditorContextManager.IsProjectLoaded)
            {
                Log.Warning("No active project workspace is currently open.");
                return;
            }

            if(EditorContextManager.ActiveLoadedScene == null)
            {
                Log.Warning("[Editor UI] Save aborted: There is no active scene context loaded to persist.");
                return;
            }

            try
            {
                SaveScene();
            }
            catch(Exception ex)
            {
                // Failures during save are already logged by SceneSerializer, but this provides a UI safety fallback
                MessageBox.Show($"Failed to save project layout safely to disk:\n{ex.Message}", "IO Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void AppendMessageToConsoleBox(LogSeverity severity, string formattedText)
        {
            if(ConsoleTextBox.InvokeRequired)
            {
                ConsoleTextBox.Invoke(new Action(() => AppendMessageToConsoleBox(severity, formattedText)));
                return;
            }

            // 1. Permanently keep track of this entry for subsequent search filtering
            // Note: Storing as a named tuple matching your historical layout
            _masterLogHistory.Add((Severity: severity, Message: formattedText));

            // 2. Only append directly to the view if it passes the current active search filter
            // FIX: Using the exposed .SearchQuery property from your TextSearchBarControl
            string activeFilter = consoleSearchBar.SearchQuery;

            if(string.IsNullOrEmpty(activeFilter) || formattedText.Contains(activeFilter, StringComparison.CurrentCultureIgnoreCase))
            {
                ConsoleTextBox.BeginUpdate();

                // 3. Select the color based on severity rules
                System.Drawing.Color logColor;
                switch(severity)
                {
                    case LogSeverity.Info:
                        logColor = System.Drawing.Color.DarkGreen; // Stands out nicely on dark/light themes
                        break;
                    case LogSeverity.Warning:
                        logColor = System.Drawing.Color.DarkGoldenrod;
                        break;
                    case LogSeverity.Error:
                        logColor = System.Drawing.Color.DarkRed;
                        break;
                    case LogSeverity.Print:
                    default:
                        logColor = ConsoleTextBox.ForeColor; // Default system text color
                        break;
                }

                // 4. Position selection at the end, assign color, and append the string
                ConsoleTextBox.SelectionStart = ConsoleTextBox.TextLength;
                ConsoleTextBox.SelectionLength = 0;
                ConsoleTextBox.SelectionColor = logColor;

                ConsoleTextBox.AppendText(formattedText + Environment.NewLine);

                // 5. Reset selection back to standard color so future non-colored text stays clean
                ConsoleTextBox.SelectionColor = ConsoleTextBox.ForeColor;

                // Auto-scroll mechanics
                ConsoleTextBox.SelectionStart = ConsoleTextBox.TextLength;
                ConsoleTextBox.ScrollToCaret();

                ConsoleTextBox.EndUpdate();
            }
        }

        // Tree Shared Searching Utilities
        private void ResetTreeNodes(TreeNodeCollection nodes)
        {
            foreach(TreeNode node in nodes)
            {
                node.ForeColor = SystemColors.WindowText;
                node.Collapse();
                ResetTreeNodes(node.Nodes);
            }
        }
        public static void RebuildInspectorPanel(GameObject targetGo)
        {
            if(ActiveInspectorPanel == null)
                return;

            ActiveInspectorPanel.SuspendLayout();

            FlowLayoutPanel? flowLayout = ActiveInspectorPanel.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
            if(flowLayout == null)
            {
                flowLayout = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    AutoScroll = true
                };
                ActiveInspectorPanel.Controls.Add(flowLayout);
            }

            flowLayout.SuspendLayout();
            flowLayout.Controls.Clear();
            Engine.Editor.WinFormsApp1.ComponentCardFactory.ClearSelection();

            if(targetGo != null)
            {
                // 💡 FIX: Draw the root GameObject's card at the very top first!
                Panel goCard = Engine.Editor.WinFormsApp1.ComponentCardFactory.CreateCard("GameObject Properties", targetGo, flowLayout.Width);
                flowLayout.Controls.Add(goCard);

                // Then loop through the ECS dictionary and build the rest of the component cards down the column
                foreach(var kvp in targetGo.Components)
                {
                    string name = kvp.Key.Name;
                    object instance = kvp.Value;

                    Panel card = Engine.Editor.WinFormsApp1.ComponentCardFactory.CreateCard(name, instance, flowLayout.Width);
                    flowLayout.Controls.Add(card);
                }
            }

            flowLayout.ResumeLayout(true);
            ActiveInspectorPanel.ResumeLayout(true);
        }

        // Simple context helper mapping your Hierarchy tree to live memory references
        private GameObject? GetSelectedGameObjectFromHierarchy()
        {
            if(SceneHierarchyTreeView.SelectedNode == null)
                return null;

            // Presuming you stored your GameObject reference inside the TreeNode's Tag property
            return SceneHierarchyTreeView.SelectedNode.Tag as GameObject;
        }


        private void InitializePropertiesToolstripEvents()
        {
            // Create the master context menu container once
            AddComponentButton.DropDown = new ContextMenuStrip();

            // 💡 Run this logic every single time the user clicks to open the dropdown menu
            AddComponentButton.DropDownOpening += (s, e) =>
            {
                AddComponentButton.DropDownItems.Clear();
                GameObject? selectedGo = GetSelectedGameObjectFromHierarchy();

                // 1. Gather all valid component types in the project via Reflection
                var componentTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => t.IsSubclassOf(typeof(GameComponent)) && !t.IsAbstract);

                foreach(var type in componentTypes)
                {
                    // Don't show the core Transform component since objects can't have duplicates or live without it
                    if(type == typeof(Engine.Core.ECS.Components.TransformComponent))
                        continue;

                    ToolStripMenuItem item = new ToolStripMenuItem(type.Name.Replace("Component", ""));
                    Type targetType = type; // Lock the closure context safely

                    // 💡 SMART FILTER: If a GameObject is selected, check if it already owns this component type
                    if(selectedGo != null && selectedGo.Components.ContainsKey(targetType))
                    {
                        item.Enabled = false; // Gray it out!
                        item.Text += " (Already Attached)";
                    }

                    // The click execution pipeline
                    item.Click += (subSender, subArgs) =>
                    {
                        if(selectedGo == null)
                        {
                            MessageBox.Show("Please select a GameObject in the hierarchy tree first.", "No Target Active");
                            return;
                        }

                        if(Activator.CreateInstance(targetType) is GameComponent newComp)
                        {
                            selectedGo.AddComponent(newComp);
                            Log.Info($"[Editor UI] Attached component '{targetType.Name}' to '{selectedGo.Name}'");

                            // Rebuild and refresh the card view layout panel
                            RebuildInspectorPanel(selectedGo);
                        }
                    };

                    AddComponentButton.DropDownItems.Add(item);
                }
            };

            // --- ➖ REMOVE COMPONENT BUTTON ---
            RemoveComponentButton.Click += (s, e) =>
            {
                GameObject? selectedGo = GetSelectedGameObjectFromHierarchy();
                object? activeComponent = Engine.Editor.WinFormsApp1.ComponentCardFactory.SelectedComponentInstance;

                if(selectedGo == null || activeComponent == null)
                {
                    MessageBox.Show("Please select both a GameObject and a specific component card to remove.", "Selection Missing");
                    return;
                }

                if(activeComponent is GameComponent componentInstance)
                {
                    if(componentInstance.GetType() == typeof(Engine.Core.ECS.Components.TransformComponent))
                    {
                        MessageBox.Show("The TransformComponent cannot be removed from a GameObject.", "Action Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    selectedGo.RemoveComponent(componentInstance);
                    Log.Info($"[Editor UI] Removed component '{componentInstance.GetType().Name}' from '{selectedGo.Name}'");

                    Engine.Editor.WinFormsApp1.ComponentCardFactory.ClearSelection();
                    RebuildInspectorPanel(selectedGo);
                }
            };
        }

        public void StartSimulationButton_Click(object sender, EventArgs e)
        {
            if(!EditorContextManager.IsProjectLoaded || EditorContextManager.ActiveLoadedScene == null)
            {
                Log.Warning("[Simulation Error] Cannot start simulation: No active project or scene is loaded.");
                return;
            }
            // 1. Save the current scene state before starting simulation
            SaveScene();
            // 3. Start the simulation in the MonoGame control
            mgWindowControl.StartSimulation();
        }

        public void PauseSimulationButton_Click(object sender, EventArgs e)
        {
            if(mgWindowControl.SimulationRunning)
            {
                mgWindowControl.pauseSimulation();

            }
            else
            {
                Log.Warning("[Simulation Error] Cannot pause/resume: Simulation is not currently running.");

            }
        }

        public void StopSimulationButton_Click(object sender, EventArgs e)
        {
            if(mgWindowControl.SimulationRunning)
            {
                mgWindowControl.StopSimulation();

                // Optionally reload the clean scene to reset the state
                LoadCleanScene();
            }
            else
            {
                Log.Warning("[Simulation Error] Cannot stop: Simulation is not currently running.");
            }
        }

        public void LoadCleanScene()
        {
            if(EditorContextManager.IsProjectLoaded && EditorContextManager.ActiveLoadedScene != null)
            {
                try
                {
                    string targetScenePath = Path.Combine(
                        EditorContextManager.CurrentProjectRoot,
                        "Content",
                        "Scenes",
                        $"{EditorContextManager.ActiveLoadedScene.SceneName}.toml"
                    );

                    if(File.Exists(targetScenePath))
                    {
                        // Silently load without prompts
                        GameScene revertedScene = SceneSerializer.LoadScene(targetScenePath);
                        EditorContextManager.ActiveLoadedScene = revertedScene;

                        // Repopulate the main tree view panel UI
                        if(Form1.ActiveHierarchyTreeView != null)
                        {
                            Form1.ActiveHierarchyTreeView.BeginInvoke(new Action(() =>
                            {
                                // 💡 FIX: Find the live instance of Form1 that owns this TreeView control
                                if(Form1.ActiveHierarchyTreeView.FindForm() is Form1 mainForm)
                                {
                                    mainForm.PopulateSceneHierarchyTree(Form1.ActiveHierarchyTreeView, revertedScene);
                                }
                                else
                                {
                                    Log.Error("[Simulation Error] Could not locate the running Form1 instance to update the UI.");
                                }
                            }));
                        }
                        Log.Info("[Simulation] Workspace state successfully restored.");
                    }
                }
                catch(Exception ex)
                {
                    Log.Error($"[Simulation Error] Failed to auto-reload pre-simulation snapshot context: {ex.Message}");
                }
            }
        }

        private bool FilterTreeNodes(TreeNodeCollection nodes, string filter)
        {
            bool anyChildVisible = false;
            foreach(TreeNode node in nodes)
            {
                bool isChildVisible = FilterTreeNodes(node.Nodes, filter);
                bool isCurrentMatch = node.Text.Contains(filter, StringComparison.CurrentCultureIgnoreCase);

                if(isCurrentMatch || isChildVisible)
                {
                    node.ForeColor = SystemColors.WindowText;
                    if(isChildVisible)
                        node.Expand();
                    anyChildVisible = true;
                }
                else
                {
                    node.ForeColor = SystemColors.WindowText;
                    node.Collapse();
                }
            }
            return anyChildVisible;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void toolStripComboBox1_Click(object sender, EventArgs e)
        {

        }

        private void textSearchBarControl1_Load(object sender, EventArgs e)
        {

        }

        private void consoleSearchBar_Load(object sender, EventArgs e)
        {

        }

        private void consoleSearchBar_SearchTextChanged(object sender, string e)
        {
            string filterText = e; // Lowercase sanitized string passed directly from the event payload

            ConsoleTextBox.BeginUpdate();
            ConsoleTextBox.Clear();

            foreach(var log in _masterLogHistory)
            {
                if(string.IsNullOrEmpty(filterText) || log.Message.Contains(filterText, StringComparison.CurrentCultureIgnoreCase))
                {
                    // Reapply colors during the historical filter stream build
                    System.Drawing.Color logColor = log.Severity switch
                    {
                        LogSeverity.Info => System.Drawing.Color.DarkGreen,
                        LogSeverity.Warning => System.Drawing.Color.DarkGoldenrod,
                        LogSeverity.Error => System.Drawing.Color.DarkRed,
                        _ => ConsoleTextBox.ForeColor
                    };

                    ConsoleTextBox.SelectionStart = ConsoleTextBox.TextLength;
                    ConsoleTextBox.SelectionColor = logColor;
                    ConsoleTextBox.AppendText(log.Message + Environment.NewLine);
                }
            }

            ConsoleTextBox.SelectionStart = ConsoleTextBox.TextLength;
            ConsoleTextBox.SelectionColor = ConsoleTextBox.ForeColor; // Reset

            ConsoleTextBox.EndUpdate();
        }
    }
}