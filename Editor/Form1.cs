using Editor;
using Engine.Content.Builder;
using Engine.Core.ECS;
using Engine.Core.Runtime;
using Engine.Core.Serialization;
using Engine.Core.Utilities;
using Engine.Editor;
using Engine.Editor.Utilities;
using Microsoft.Xna.Framework.Content.Pipeline;
using MonoGame.Framework.Content.Pipeline.Builder;
using SharpDX.MediaFoundation;
using SharpDX.WIC;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static System.Environment;

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


        private readonly System.Collections.Generic.List<string> _uiScreenNames = new System.Collections.Generic.List<string>();
        private ScriptAssemblyManager _scriptManager = new ScriptAssemblyManager();
        private DateTime _lastBuildTimestamp = DateTime.MinValue;
        private bool _isCompiling = false;
        public static TreeView ActiveHierarchyTreeView
        {
            get; private set;
        }

        public static GroupBox ActiveInspectorPanel
        {
            get; private set;
        }

        private static bool _needsToBeSaved = false;
        private bool _isSuprressingDirtyFlag = false;

        public static bool NeedsToBeSaved
        {
            get => _needsToBeSaved;
            set
            {
                if(_needsToBeSaved != value)
                {
                    _needsToBeSaved = value;

                    var mainForm = Application.OpenForms.OfType<Form1>().FirstOrDefault();
                    mainForm?.UpdateSceneTextBox(EditorContextManager.ActiveLoadedScene.SceneName);
                    mainForm?.UpdateSceneHierarchyTitle(EditorContextManager.ActiveLoadedScene.SceneName);
                }
            }
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
            this.Activated += Form1_Activated;

            Log.Info("[Editor UI] Initializing editor main form...");
            SetTreeViewTheme(ProjectFolderTreeView.Handle);
            InitializeExplorerIcons();

            InitializeProjectExplorerMenus();
            InitializeSceneHierarchyMenus();
            ActiveHierarchyTreeView = this.SceneHierarchyTreeView;
            ActiveInspectorPanel = this.PropertiesWindow;
            UpdateEditorTitle();
            InitializePropertiesToolstripEvents();
        }

        private void TextSearchBarControl1_TextChanged(object sender, EventArgs e)
        {

        }


        private void CreateNewScene()
        {
            if(!EditorContextManager.IsProjectLoaded)
            {
                MessageBox.Show("No Project is Selected. Cannot load or Create new scenes");
                return;
            }

            string sceneName = PromptUserForSceneName();
            var newScene = new GameScene();
            newScene.SceneName = sceneName;
            string sceneFileName = sceneName + ".scene";
            string scenePath = Path.Combine(EditorContextManager.CurrentProjectRoot, "Content", "Assets", "Scenes", sceneFileName);

            SceneSerializer.SaveScene(newScene, scenePath);
            LoadScene(newScene, scenePath);
            PopulateProjectExplorerTree(ProjectFolderTreeView);


        }

        public void RefreshProjectFolderView()
        {

            PopulateProjectExplorerTree(ProjectFolderTreeView);
        }
        public static void SetTreeViewTheme(IntPtr treeHandle)
        {
            SetWindowTheme(treeHandle, "explorer", null);
        }

        private void UpdateEditorTitle()
        {
            string dirtyIndicator = NeedsToBeSaved ? " *" : "";
            if(EditorContextManager.IsProjectLoaded)
            {
                string projectName = Path.GetFileName(EditorContextManager.CurrentProjectRoot);
                this.Text = $"Custom 2D Game Engine Editor - Project: [{projectName}] ({EditorContextManager.CurrentProjectRoot}{dirtyIndicator})";
            }
            else
            {
                this.Text = "Custom 2D Game Engine Editor - No Project Active";
            }
        }

        private static void HookPropertyGridChanges(Control parent)
        {
            foreach(Control child in parent.Controls)
            {
                if(child is PropertyGrid grid)
                {
                    // Unsubscribe first to prevent double-subscription issues
                    grid.PropertyValueChanged -= PropertyGrid_PropertyValueChanged;
                    grid.PropertyValueChanged += PropertyGrid_PropertyValueChanged;
                }
                if(child.HasChildren)
                {
                    HookPropertyGridChanges(child);
                }
            }
        }

        private async void Form1_Activated(object? sender, EventArgs e)
        {
            // Avoid triggering multiple compiles if one is already running
            if(_isCompiling || string.IsNullOrEmpty(EditorContextManager.CurrentProjectRoot))
                return;

            string sourceFolder = Path.Combine(EditorContextManager.CurrentProjectRoot, "Source");
            if(!Directory.Exists(sourceFolder))
                return;

            // 1. Check if any .cs file was modified after our last successful build
            bool hasModifiedScripts = Directory.GetFiles(sourceFolder, "*.cs", SearchOption.AllDirectories)
                .Any(filePath => File.GetLastWriteTime(filePath) > _lastBuildTimestamp);

            if(!hasModifiedScripts)
                return; // Nothing changed, no need to rebuild!

            // 2. Lock compiling flag and record timestamp
            _isCompiling = true;
            _lastBuildTimestamp = DateTime.Now;

            // Update status UI
            UpdateProgressText("Code changes detected. Compiling in background...");
            Log.Info("Code changes detected. Compiling in background...");

            try
            {
                // 3. Trigger background build
                BuildResult result = await ScriptCompiler.CompileGameplayProjectAsync(
                    EditorContextManager.CurrentProjectRoot,
                    EditorContextManager.CurrentProjectName
                );

                if(result.Success)
                {
                    // 4. Hot-swap assembly in RAM
                    _scriptManager.LoadGameplayAssembly(result.AssemblyPath);

                    // Refresh editor type inspectors / registries
                    RebuildInspectorPanel(GetSelectedGameObjectFromHierarchy() ?? null);
                    RefreshProjectFolderView();

                    UpdateProgressText("Compilation successful! Hot-reloaded gameplay scripts.");
                    Log.Info("Compilation successful! Hot-reloaded gameplay scripts.");
                }
                else
                {
                    UpdateProgressText("Build Error! Check console output.");
                    // Print errors to your editor's console panel
                    Log.Error(result.OutputLog);
                }
            }
            catch(Exception ex)
            {
                Log.Error($"Compilation exception: {ex.Message}");
            }
            finally
            {
                _isCompiling = false;
            }
        }

        private static void PropertyGrid_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            NeedsToBeSaved = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
           
        }

        public void UpdateProgressText(string text)
        {
            progressBarTextBox.Text = text;
        }

        public void OnProjectLoaded()
        {
            UpdateEditorTitle();

            if(EditorContextManager.IsProjectLoaded)
            {
                InitializeAndLoadGameplayAssembly();
                // TODO  set the loaded scene to the project manifests last used scene, default to first scene in scene folder if one exists, if one doesn't exist create a base scene file

                string absoluteBinContentPath = Path.Combine(EditorContextManager.BinPath, "Content");
                mgWindowControl.Editor.Content.RootDirectory = absoluteBinContentPath;

                string targetScenePath = Path.Combine(EditorContextManager.ScenesPath, "Main.scene");

                // 2. CHECK: If the file exists, load it! Otherwise, fall back to generating a clean slate template.
                if(File.Exists(targetScenePath))
                {
                    _isSuprressingDirtyFlag = true;
                    try
                    {
                        Log.Info($"[Editor UI] Found existing workspace state file. Deserializing active layout tree...");

                        // Read the file structure straight back into memory



                        // TODO parse the loaded scenes entities into loadedScene?



                        //yaml serialization. 
                        var loadedScene = new GameScene();
                        loadedScene = SceneSerializer.LoadScene(targetScenePath);
                        loadedScene.resetContextSceneInManagers();
                        //gism serialization
                        //GameScene loadedScene = GISMSceneSerializer.LoadScene(targetScenePath);


                        // Set the context and populate your UI nodes with the genuine saved data
                        EditorContextManager.ActiveLoadedScene = loadedScene;
                        UpdateSceneTextBox(EditorContextManager.ActiveLoadedScene.SceneName);
                        RunContentBuilder();
                        UpdateSceneHierarchyTitle(EditorContextManager.ActiveLoadedScene.SceneName);
                        PopulateSceneHierarchyTree(SceneHierarchyTreeView, loadedScene);

                    }
                    catch(Exception ex)
                    {
                        Log.Error($"[Editor UI Error] Scene file was found but failed to deserialize. Falling back to default layout. Reason: {ex.Message}");
                        LoadDefaultSandboxScene();
                    }
                    finally
                    {
                        _isSuprressingDirtyFlag = false;
                        NeedsToBeSaved = false;
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


      
    

        public void UpdateSceneTextBox(string sceneName)
        {
           
            SceneNameBox.Clear();

            SceneNameBox.Text = sceneName;
        }


        private void RunContentBuilder()
        {
            Log.Info("[Content Builder] Attempting to run content builder");
            Task.Run(() =>
            {
                try
                {
                    Log.Info("[Content Builder] Running Content Builder");
                    var builder = new DynamicBuilder();
                    builder.Logger = new EngineContentLogger();
                    var args = new ContentBuilderParams
                    {
                        Mode = ContentBuilderMode.Builder,
                        WorkingDirectory = EditorContextManager.AssetsPath,
                        SourceDirectory = EditorContextManager.AssetsPath,
                        OutputDirectory = Path.Combine(EditorContextManager.ContentPath, "Bin"),
                        Platform = TargetPlatform.DesktopGL

                    };

                    builder.Run(args);

                    // Invoke back to the UI thread if you need to update a status bar or "Ready" icon
                    this.Invoke(new Action(() =>
                    {
                        Log.Info("[Content Builder] Content build complete.");
                        // Update UI status here
                        ProjectFolderTreeView.Refresh();
                    }));
                }
                catch(Exception ex)
                {
                    Log.Error($"[Content Build Error] {ex.Message}");
                }
            });

        }


        private void UpdateSceneHierarchyTitle(string sceneName)
        {
            string dirtyIndicator = NeedsToBeSaved ? " * " : "";
            SceneHierarchyPanel.ResetText();
            SceneHierarchyPanel.Text = SceneHierarchyPanel.Text + ": '" + sceneName + dirtyIndicator + "  '";
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

        private string PromptUserForSceneName()
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Enter Scene Name",
                StartPosition = FormStartPosition.CenterParent
            };
            Label textLabel = new Label() { Left = 20, Top = 20, Text = "Scene Name:", Width = 150 };
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
            if(!EditorContextManager.IsProjectLoaded)
            {
                MessageBox.Show("No project is loaded. Cannot create or load scenes");
                return;
            }
            using(var folderDialog = new OpenFileDialog())
            {
                folderDialog.Multiselect = false;
                folderDialog.InitialDirectory = Path.Combine(EditorContextManager.ContentPath, "Assets", "Scenes");
                folderDialog.Filter = "Scene files (*.scene)|*.scene|All files (*.*)|*.*";
                folderDialog.RestoreDirectory = true;


                if(folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string targetScenePath = folderDialog.FileName;
                    UpdateEditorTitle();
                    _isSuprressingDirtyFlag = true;
                    try
                    {
                        Log.Info($"[Editor UI] Found existing Scene file. Deserializing active layout tree...");

                        //yaml serialization. 
                        var loadedScene = new GameScene();
                        loadedScene = SceneSerializer.LoadScene(targetScenePath);


                        // Set the context and populate your UI nodes with the genuine saved data
                        EditorContextManager.ActiveLoadedScene = loadedScene;
                        UpdateSceneTextBox(EditorContextManager.ActiveLoadedScene.SceneName);
                        EditorContextManager.ActiveLoadedScene.resetContextSceneInManagers();
                        UpdateSceneHierarchyTitle(EditorContextManager.ActiveLoadedScene.SceneName);
                        PopulateSceneHierarchyTree(SceneHierarchyTreeView, loadedScene);
                    }
                    catch(Exception ex)
                    {
                        Log.Error($"[Editor UI Error] Scene file was found but failed to deserialize. Falling back to default layout. Reason: {ex.Message}");
                        LoadDefaultSandboxScene();
                    }
                    finally
                    {
                        _isSuprressingDirtyFlag = false;
                        NeedsToBeSaved = false; // Reset to clean state
                    }
                }
            }
        }

        private void LoadScene(GameScene sceneToLoad, string targetScenePath)
        {


            UpdateEditorTitle();
            try
            {
                Log.Info($"[Editor UI] Found existing Scene file. Deserializing active layout tree...");

                //yaml serialization. 
                var loadedScene = sceneToLoad;
                loadedScene = SceneSerializer.LoadScene(targetScenePath);


                // Set the context and populate your UI nodes with the genuine saved data
                EditorContextManager.ActiveLoadedScene = loadedScene;
                UpdateSceneTextBox(EditorContextManager.ActiveLoadedScene.SceneName);
                EditorContextManager.ActiveLoadedScene.resetContextSceneInManagers();
                UpdateSceneHierarchyTitle(EditorContextManager.ActiveLoadedScene.SceneName);
                PopulateSceneHierarchyTree(SceneHierarchyTreeView, loadedScene);
            }
            catch(Exception ex)
            {
                Log.Error($"[Editor UI Error] Scene file was found but failed to deserialize. Falling back to default layout. Reason: {ex.Message}");
                LoadDefaultSandboxScene();
            }


        }

        public static void SaveScene()
        {
            if(!EditorContextManager.IsProjectLoaded)
            {
                MessageBox.Show("No project is loaded. Cannot save Scene.");
                return;
            }

            if(EditorContextManager.ActiveLoadedScene == null)
            {
                MessageBox.Show("No Scene Loaded. Cannot Save Scene.");
                return;
            }

            try
            {
                Log.Info("[Editor UI] Initiating scene hierarchy persistence pipeline...");

                // 1. Build the path matching your project context rules
                string sceneFileName = $"{EditorContextManager.ActiveLoadedScene.SceneName}.scene";
                //string GISMFileName = $"{EditorContextManager.ActiveLoadedScene.SceneName}.gism";
                //string GISMTargetScenePath = Path.Combine(EditorContextManager.CurrentProjectRoot, "Content", "Scenes", GISMFileName);
                string targetScenePath = Path.Combine(EditorContextManager.CurrentProjectRoot, "Content", "Assets", "Scenes", sceneFileName);

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

                _needsToBeSaved = false;
            }
            catch(Exception ex)
            {
                // Failures during save are already logged by SceneSerializer, but this provides a UI safety fallback
                Log.Warning($"Failed to save project layout safely to disk:\n{ex.Message}");
            }
        }





        public void onSaveProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {


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

        private void LaunchGumEditorButton_Click(object sender, EventArgs e)
        {
            if(!EditorContextManager.IsProjectLoaded)
            {
                MessageBox.Show("No project is loaded. Cannot launch the Gum editor.", "Project Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 1. Locate the Gum project directory within the active workspace
            string gumProjectDir = Path.Combine(EditorContextManager.CurrentProjectRoot, "Content", "GumProject");
            if(!Directory.Exists(gumProjectDir))
            {
                Directory.CreateDirectory(gumProjectDir);
            }

            // 2. Find the .gumx project file
            string[] gumFiles = Directory.GetFiles(gumProjectDir, "*.gumx");
            string gumFilePath = string.Empty;

            if(gumFiles.Length > 0)
            {
                gumFilePath = gumFiles[0];
            }
            else
            {
                // Fallback: Generate a default .gumx if none exists yet
                string projectName = EditorContextManager.CurrentProjectName ?? Path.GetFileName(EditorContextManager.CurrentProjectRoot);
                gumFilePath = Path.Combine(gumProjectDir, $"{projectName}.gumx");

                string gumProjectXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<GumProjectSave xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Version>1</Version>
  <Screens />
  <Components />
  <StandardElements />
  <CustomBehaviors />
</GumProjectSave>";

                File.WriteAllText(gumFilePath, gumProjectXml);
                Log.Info($"[Gum Editor] Generated missing .gumx project file at: {gumFilePath}");
            }

            try
            {
                // 3. Launch the Gum editor via shell execution (opens the .gumx file directly in the standalone Gum application)
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = gumFilePath,
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(startInfo);
                Log.Info($"[Gum Editor] Successfully launched Gum editor with project: {gumFilePath}");
            }
            catch(Exception ex)
            {
                Log.Error($"[Gum Editor Error] Failed to launch Gum editor: {ex.Message}");
                MessageBox.Show($"Could not launch the Gum editor automatically.\nEnsure the Gum tool is installed and `.gumx` files are registered on your system.\n\nError: {ex.Message}", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if(targetGo == null)
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

                // 💡 Automatically stretch cards to fill width when the Inspector panel is resized
                flowLayout.Resize += (s, e) =>
                {
                    flowLayout.SuspendLayout();
                    int cardWidth = flowLayout.ClientSize.Width - flowLayout.Margin.Horizontal - 10;
                    if(cardWidth > 50)
                    {
                        foreach(Control c in flowLayout.Controls)
                        {
                            c.Width = cardWidth;
                        }
                    }
                    flowLayout.ResumeLayout(false);
                };

                ActiveInspectorPanel.Controls.Add(flowLayout);
            }

            int expectedCount = 1; // Start at 1 for the root GameObject Properties card
            if(targetGo != null)
            {
                expectedCount += targetGo.Components.Count;
            }

            // Check if the panel is already showing this exact GameObject
            if(flowLayout.Controls.Count == expectedCount && flowLayout.Controls.Count > 0 && flowLayout.Controls[0].Tag == targetGo)
            {
                RefreshAllPropertyGrids(flowLayout);
                ActiveInspectorPanel.ResumeLayout(true);
                return;
            }

            // --- Destructive Rebuild Area (Only hit on additions, removals, or shifting targets) ---
            flowLayout.SuspendLayout();

            // 1. CAPTURE SCROLL POSITION
            int previousScrollY = Math.Abs(flowLayout.AutoScrollPosition.Y);

            // 2. CAPTURE SELECTION
            object? previouslySelected = Engine.Editor.WinFormsApp1.ComponentCardFactory.SelectedComponentInstance;

            // 3. IF TARGET CHANGED: Wipe selection completely. If same target, keep reference.
            bool targetChanged = flowLayout.Controls.Count > 0 && flowLayout.Controls[0].Tag != targetGo;
            if(targetChanged)
            {
                Engine.Editor.WinFormsApp1.ComponentCardFactory.ClearSelection();
                previouslySelected = null;
            }

            flowLayout.Controls.Clear();

            if(targetGo != null)
            {
                // Calculate actual usable client width inside the flow panel
                int cardWidth = flowLayout.ClientSize.Width - 10;
                if(cardWidth < 120)
                    cardWidth = flowLayout.Width - 10;
                if(cardWidth < 120)
                    cardWidth = 200;

                // Draw GameObject properties at the top
                Panel goCard = Engine.Editor.WinFormsApp1.ComponentCardFactory.CreateCard(
                    "GameObject Properties",
                    targetGo,
                    cardWidth,
                    previouslySelected
                );
                goCard.Tag = targetGo;
                flowLayout.Controls.Add(goCard);

                // Populate components
                foreach(var kvp in targetGo.Components)
                {
                    string name = kvp.Key.Name;
                    object instance = kvp.Value;

                    Panel card = Engine.Editor.WinFormsApp1.ComponentCardFactory.CreateCard(
                        name,
                        instance,
                        cardWidth,
                        previouslySelected
                    );
                    flowLayout.Controls.Add(card);
                }
            }

            flowLayout.ResumeLayout(true);

            // 4. RESTORE SCROLL POSITION
            flowLayout.AutoScrollPosition = new Point(0, previousScrollY);
            HookPropertyGridChanges(ActiveInspectorPanel);
            ActiveInspectorPanel.ResumeLayout(true);
        }
        /// <summary>
        /// Recursively finds and refreshes all PropertyGrid controls inside the inspector's panel.
        /// </summary>
        private static void RefreshAllPropertyGrids(Control parent)
        {
            foreach(Control child in parent.Controls)
            {
                if(child is PropertyGrid grid)
                {
                    grid.Refresh();
                }
                if(child.HasChildren)
                {
                    RefreshAllPropertyGrids(child);
                }
            }
        }
        // Simple context helper mapping your Hierarchy tree to live memory references
        private GameObject? GetSelectedGameObjectFromHierarchy()
        {
            if(SceneHierarchyTreeView.SelectedNode == null)
                return null;

            // Presuming you stored your GameObject reference inside the TreeNode's Tag property
            return SceneHierarchyTreeView.SelectedNode.Tag as GameObject;
        }

        private void InitializeAndLoadGameplayAssembly()
        {
            if(string.IsNullOrEmpty(EditorContextManager.CurrentProjectRoot))
                return;

            string sourceFolder = Path.Combine(EditorContextManager.CurrentProjectRoot, "Source");
            if(!Directory.Exists(sourceFolder))
                return;

            UpdateProgressText("Compiling and loading gameplay scripts...");
            Log.Info("[Script Manager] Building gameplay assembly for project startup...");

            try
            {
                // Temporarily clear the SynchronizationContext to prevent UI thread deadlocks when calling .Result
                var oldContext = System.Threading.SynchronizationContext.Current;
                BuildResult result;
                try
                {
                    System.Threading.SynchronizationContext.SetSynchronizationContext(null);

                    // Execute and wait synchronously and safely
                    result = ScriptCompiler.CompileGameplayProjectAsync(
                        EditorContextManager.CurrentProjectRoot,
                        EditorContextManager.CurrentProjectName
                    ).Result;
                }
                finally
                {
                    System.Threading.SynchronizationContext.SetSynchronizationContext(oldContext);
                }

                if(result.Success && !string.IsNullOrEmpty(result.AssemblyPath))
                {
                    _scriptManager.LoadGameplayAssembly(result.AssemblyPath);
                    _lastBuildTimestamp = DateTime.Now;
                    Log.Info("[Script Manager] Gameplay assembly successfully loaded. Custom components are ready.");
                }
                else
                {
                    Log.Error($"[Script Manager Startup Build Error]:\n{result.OutputLog}");
                }
            }
            catch(Exception ex)
            {
                Log.Error($"[Script Manager Startup Exception]: {ex.Message}");
            }
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

                // 1. Gather component types using a scoped, memory-safe assembly lookup
                var componentTypes = new System.Collections.Generic.List<Type>();

                // Add core engine components
                var coreAssembly = typeof(GameComponent).Assembly;
                componentTypes.AddRange(coreAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(GameComponent)) && !t.IsAbstract));

                // Add user script components from the current hot-loaded assembly only
                if(_scriptManager.CurrentAssembly != null)
                {
                    try
                    {
                        var userTypes = _scriptManager.CurrentAssembly.GetTypes()
                            .Where(t => t.IsSubclassOf(typeof(GameComponent)) && !t.IsAbstract);
                        componentTypes.AddRange(userTypes);
                    }
                    catch(Exception ex)
                    {
                        Log.Error($"[Script Manager] Failed to retrieve types from active gameplay assembly: {ex.Message}");
                    }
                }

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
                            NeedsToBeSaved = true;

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
                    NeedsToBeSaved = true;

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
                        EditorContextManager.AssetsPath,
                        "Scenes",
                        $"{EditorContextManager.ActiveLoadedScene.SceneName}.scene"
                    );

                    if(File.Exists(targetScenePath))
                    {
                        // Silently load without prompts
                        GameScene revertedScene = SceneSerializer.LoadScene(targetScenePath);

                        EditorContextManager.ActiveLoadedScene = revertedScene;
                        EditorContextManager.ActiveLoadedScene.resetContextSceneInManagers();
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

        private void SceneNameBox_TextChanged(object sender, EventArgs e)
        {
            if(_isSuprressingDirtyFlag)
                return;

            if(EditorContextManager.ActiveLoadedScene != null)
            {
                if(EditorContextManager.ActiveLoadedScene.SceneName != SceneNameBox.Text)
                {
                    EditorContextManager.ActiveLoadedScene.SceneName = SceneNameBox.Text;
                    NeedsToBeSaved = true;
                }
            }
        }

        private void LoadSceneButton_Click(object sender, EventArgs e)
        {
            LoadScene();
        }

        private void CreateNewSceneButton_Click(object sender, EventArgs e)
        {
            CreateNewScene();
        }

        private void databaseViewerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var viewer = new DatabaseViewer();
            viewer.Show(this);
        }
    }
}