using Editor;
using Engine.Content.Builder;
using Engine.Core.ECS;
using Engine.Core.ECS.Systems;
using Engine.Core.GamePlay;
using Engine.Core.Runtime;
using Engine.Core.Serialization;
using Engine.Core.Utilities;
using Engine.Editor;
using Engine.Editor.Theming;
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



        private Panel tilesetCanvasPanel;
        private PictureBox tilesetPictureBox;
        private NumericUpDown tileValueNumeric;
        private Label selectedTileLabel;
        private int selectedTileIndex = -1;
        private System.Drawing.Bitmap currentTilesetBitmap = null;
        private readonly System.Collections.Generic.List<string> _uiScreenNames = new System.Collections.Generic.List<string>();
        private ScriptAssemblyManager _scriptManager = new ScriptAssemblyManager();
        private DateTime _lastBuildTimestamp = DateTime.MinValue;
        private bool _isCompiling = false;
        public static TreeView ActiveHierarchyTreeView
        {
            get; private set;
        }

        private static GameObject? _currentInspectedGameObject = null;

        public static GroupBox ActiveInspectorPanel
        {
            get; private set;
        }

        private static bool _needsToBeSaved = false;
        private bool _isSuprressingDirtyFlag = false;
        private GameScene? _trackedScene;
        private FileSystemWatcher? _scriptWatcher;
        private MGWindowControl mgWindow;
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

            //ControlThemeExtensions.ApplySynthwaveTheme(this);

            Log.Info("[Editor UI] Initializing editor main form...");
            SetTreeViewTheme(ProjectFolderTreeView.Handle);
            InitializeExplorerIcons();
            mgWindow = mgWindowControl;
            InitializeProjectExplorerMenus();
            InitializeSceneHierarchyMenus();
            ActiveHierarchyTreeView = this.SceneHierarchyTreeView;
            ActiveInspectorPanel = this.PropertiesWindow;
            UpdateEditorTitle();
            InitializePropertiesToolstripEvents();
            InitializeSceneManagementTabs();
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

        private void InitializeScriptWatcher()
        {
            // Clean up any existing watcher if switching projects
            _scriptWatcher?.Dispose();
            _scriptWatcher = null;

            if(string.IsNullOrEmpty(EditorContextManager.CurrentProjectRoot))
                return;

            string sourceFolder = Path.Combine(EditorContextManager.CurrentProjectRoot, "Source");
            if(!Directory.Exists(sourceFolder))
                return;

            _scriptWatcher = new FileSystemWatcher(sourceFolder, "*.cs")
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
            };

            // Debounce timer or flag to prevent rapid-fire multiple compilation triggers on a single save action
            DateTime lastTriggerTime = DateTime.MinValue;

            _scriptWatcher.Changed += (s, e) => TriggerScriptRebuildThrottled(ref lastTriggerTime);
            _scriptWatcher.Created += (s, e) => TriggerScriptRebuildThrottled(ref lastTriggerTime);
            _scriptWatcher.Renamed += (s, e) => TriggerScriptRebuildThrottled(ref lastTriggerTime);

            _scriptWatcher.EnableRaisingEvents = true;
            Log.Info("[Script Manager] Background file system watcher active on Source scripts.");
        }

        private void TriggerScriptRebuildThrottled(ref DateTime lastTriggerTime)
        {
            // Prevent multiple rapid triggers if an IDE saves a file in multiple passes (e.g., temp files)
            lock(this)
            {
                if((DateTime.Now - lastTriggerTime).TotalMilliseconds < 1000)
                    return;
                lastTriggerTime = DateTime.Now;
            }

            // Safely invoke the compilation pipeline back on the UI thread
            this.BeginInvoke(new Action(async () =>
            {
                await CompileAndReloadScriptsAsync();
            }));
        }

        private async Task CompileAndReloadScriptsAsync()
        {
            if(_isCompiling || string.IsNullOrEmpty(EditorContextManager.CurrentProjectRoot))
                return;

            _isCompiling = true;
            UpdateProgressText("Code changes detected. Compiling in background...");
            Log.Info("Code changes detected. Compiling in background...");

            try
            {
                BuildResult result = await ScriptCompiler.CompileGameplayProjectAsync(
                    EditorContextManager.CurrentProjectRoot,
                    EditorContextManager.CurrentProjectName
                );

                if(result.Success)
                {
                    _scriptManager.LoadGameplayAssembly(result.AssemblyPath);
                    _lastBuildTimestamp = DateTime.Now;

                    RefreshEditor();

                    UpdateProgressText("Compilation successful! Hot-reloaded gameplay scripts.");
                    Log.Info("Compilation successful! Hot-reloaded gameplay scripts.");
                }
                else
                {
                    UpdateProgressText("Build Error! Check console output.");
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

       

        private static void PropertyGrid_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            NeedsToBeSaved = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public void RefreshEditor()
        {
            PopulateSceneHierarchyTree(SceneHierarchyTreeView, EditorContextManager.ActiveLoadedScene);
            RefreshProjectFolderView();
            RebuildInspectorPanel(GetSelectedGameObjectFromHierarchy());
            RefreshMapsTab();
            RefreshManagersTab();
            RefreshSystemsTab();


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
                        AttachSceneEvents(loadedScene);
                        UpdateSceneTextBox(EditorContextManager.ActiveLoadedScene.SceneName);
                        RunContentBuilder();
                        UpdateSceneHierarchyTitle(EditorContextManager.ActiveLoadedScene.SceneName);
                        RefreshEditor();
                        InitializeScriptWatcher();

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
                        RefreshEditor();
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
                AttachSceneEvents(loadedScene);
                UpdateSceneTextBox(EditorContextManager.ActiveLoadedScene.SceneName);
                EditorContextManager.ActiveLoadedScene.resetContextSceneInManagers();
                UpdateSceneHierarchyTitle(EditorContextManager.ActiveLoadedScene.SceneName);
                RefreshEditor();
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

        private void AttachSceneEvents(GameScene scene)
        {
            if(_trackedScene != null)
            {
                DetachSceneEvents(_trackedScene);
            }

            _trackedScene = scene;
            if(_trackedScene?.Entities != null)
            {
                _trackedScene.Entities.OnEntityCreated += OnEngineEntityCreated;
                _trackedScene.Entities.OnEntityRemoved += OnEngineEntityRemoved;
            }
           
        }

       

        private void DetachSceneEvents(GameScene scene)
        {
            if(scene?.Entities != null)
            {
                scene.Entities.OnEntityCreated -= OnEngineEntityCreated;
                scene.Entities.OnEntityRemoved -= OnEngineEntityRemoved;
            }
        }

        private void OnEngineEntityCreated(GameObject entity)
        {
            // Ensure execution happens on the UI thread since code spawning can occur off-thread
            if(SceneHierarchyTreeView.InvokeRequired)
            {
                SceneHierarchyTreeView.BeginInvoke(new Action(() => OnEngineEntityCreated(entity)));
                return;
            }

            GameScene activeScene = EditorContextManager.ActiveLoadedScene;
            if(activeScene != null)
            {
                PopulateSceneHierarchyTree(SceneHierarchyTreeView, activeScene);
                
            }
        }

        private void OnEngineEntityRemoved(GameObject entity)
        {
            if(SceneHierarchyTreeView.InvokeRequired)
            {
                SceneHierarchyTreeView.BeginInvoke(new Action(() => OnEngineEntityRemoved(entity)));
                return;
            }

            GameScene activeScene = EditorContextManager.ActiveLoadedScene;
            if(activeScene != null)
            {
                PopulateSceneHierarchyTree(SceneHierarchyTreeView, activeScene);
            }
        }
        public static void RebuildInspectorPanel(GameObject targetGo, bool forceRebuild = false)
        {
            if(ActiveInspectorPanel == null)
                return;

            // 💡 OPTIMIZATION: Short-circuit only if it's the same object AND a forced rebuild isn't requested
            if(!forceRebuild && targetGo == _currentInspectedGameObject && ActiveInspectorPanel.Controls.OfType<FlowLayoutPanel>().Any())
            {
                var existingFlowLayout = ActiveInspectorPanel.Controls.OfType<FlowLayoutPanel>().First();
                RefreshAllPropertyGrids(existingFlowLayout);
                return;
            }

            _currentInspectedGameObject = targetGo;

            if(targetGo == null)
            {
                ActiveInspectorPanel.Controls.Clear();
                return;
            }

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

            // --- Destructive Rebuild Area (Only hit when switching to a DIFFERENT GameObject) ---
            flowLayout.SuspendLayout();

            int previousScrollY = Math.Abs(flowLayout.AutoScrollPosition.Y);
            object? previouslySelected = Engine.Editor.WinFormsApp1.ComponentCardFactory.SelectedComponentInstance;

            Engine.Editor.WinFormsApp1.ComponentCardFactory.ClearSelection();
            previouslySelected = null;

            flowLayout.Controls.Clear();

            int cardWidth = flowLayout.ClientSize.Width - 10;
            if(cardWidth < 120)
                cardWidth = flowLayout.Width - 10;
            if(cardWidth < 120)
                cardWidth = 200;

            // Draw GameObject properties card at the top
            Panel goCard = Engine.Editor.WinFormsApp1.ComponentCardFactory.CreateCard(
                "GameObject Properties",
                targetGo,
                cardWidth,
                previouslySelected
            );
            goCard.Tag = targetGo;
            flowLayout.Controls.Add(goCard);

            // Populate component cards
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

            flowLayout.ResumeLayout(true);
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


        private void InitializeSceneManagementTabs()
        {
            InitializeMapsTab();
            InitializeManagersTab();
            InitializeSystemsTab();
            InitializeTilesetMetadataTab();
        }
        private void InitializeMapsTab()
        {
            // Configure DataGridView for Map List
            MapGridDataView.AutoGenerateColumns = false;
            MapGridDataView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            MapGridDataView.MultiSelect = false;
            MapGridDataView.ReadOnly = false;

            MapGridDataView.Columns.Clear();

            // 1. Map Name Column
            MapGridDataView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "MapName",
                HeaderText = "Map Name",
                Name = "ColMapName"
            });

            //2. Enabled Column (CheckBox)
            MapGridDataView.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "IsEnabled",
                HeaderText = "Enabled",
                Name = "ColIsEnabled"
            });

            // 3. Layer Order Column
            MapGridDataView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "LayerOrder",
                HeaderText = "Layer Order",
                Name = "ColLayerOrder"
            });

            // 4. Width Column
            MapGridDataView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Width",
                HeaderText = "Width (Tiles)",
                Name = "ColWidth"
            });

            // 5. Height Column
            MapGridDataView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Height",
                HeaderText = "Height (Tiles)",
                Name = "ColHeight"
            });

            // 6. Tile Size Column
            MapGridDataView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "TileSize",
                HeaderText = "Tile Size",
                Name = "ColTileSize"
            });


            // 7. Tileset Path ComboBox Column
            var tilesetColumn = new DataGridViewComboBoxColumn
            {
                DataPropertyName = "TileSetPath",
                HeaderText = "Tileset",
                Name = "ColTilesetPath",
                DisplayMember = "DisplayName",
                ValueMember = "FilePath"
            };
            MapGridDataView.Columns.Add(tilesetColumn);


            // hookup events
            AddMapButton.Click += AddMapButton_Click;
            RemoveMapButton.Click += RemoveMapButton_Click;
            MapGridDataView.SelectionChanged += MapGridDataView_SelectionChanged;

            MapGridDataView.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if(MapGridDataView.IsCurrentCellDirty)
                {
                    MapGridDataView.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };

            // Mark project as dirty and refresh viewport whenever any cell value is modified
            MapGridDataView.CellValueChanged += (s, e) =>
            {
                NeedsToBeSaved = true;
                mgWindowControl?.Invalidate();

                // Refresh the tileset preview and metadata panel when the tileset dropdown changes
                if(e.ColumnIndex >= 0 && MapGridDataView.Columns[e.ColumnIndex].Name == "ColTilesetPath")
                {
                   
                    RefreshTilesetMetadataPanel();
                }
            };
        }
        private void MapGridDataView_SelectionChanged(object sender, EventArgs e)
        {
            var scene = EditorContextManager.ActiveLoadedScene;
            if(scene == null || MapGridDataView.SelectedRows.Count == 0)
                return;

            if(MapGridDataView.SelectedRows[0].DataBoundItem is Map selectedMap)
            {
                scene.SceneMap = selectedMap;

                // Optional: Force a viewport repaint or refresh inspector properties if needed
                mgWindowControl?.Invalidate();
            }
            RefreshTilesetMetadataPanel();
        }
        private void RefreshMapsTab()
        {
            var scene = EditorContextManager.ActiveLoadedScene;
            if(scene == null)
                return;

            // Scan content directory for available tileset image files
            string contentDirectory = EditorContextManager.ContentPath;
            var tilesetOptions = new System.Collections.Generic.List<object>();

            // Add a default blank/none option to prevent binding errors
            tilesetOptions.Add(new
            {
                DisplayName = "(None)",
                FilePath = string.Empty
            });

            if(Directory.Exists(contentDirectory))
            {
                string[] imageFiles = Directory.GetFiles(contentDirectory, "*.png", SearchOption.AllDirectories);
                foreach(string filePath in imageFiles)
                {
                    string relativePath = Path.GetRelativePath(contentDirectory, filePath).Replace('\\', '/');
                    string fileName = Path.GetFileName(filePath); // Displays just file name[cite: 16]
                    tilesetOptions.Add(new
                    {
                        DisplayName = fileName,
                        FilePath = relativePath
                    });
                }
            }

            // Populate the ComboBox column data source
            if(MapGridDataView.Columns["ColTilesetPath"] is DataGridViewComboBoxColumn cbColumn)
            {
                cbColumn.DataSource = tilesetOptions;
                cbColumn.DisplayMember = "DisplayName";
                cbColumn.ValueMember = "FilePath";
            }

            var currentSelectedMap = GetSelectedMap();

            // Bind the SceneMaps list to the DataGridView
            MapGridDataView.DataSource = null;
            MapGridDataView.DataSource = scene.SceneMaps;

            // Restore previous selection if possible
            if(currentSelectedMap != null)
            {
                foreach(DataGridViewRow row in MapGridDataView.Rows)
                {
                    if(row.DataBoundItem == currentSelectedMap)
                    {
                        row.Selected = true;
                        break;
                    }
                }
            }
        }
        private void AddMapButton_Click(object sender, EventArgs e)
        {
            var scene = EditorContextManager.ActiveLoadedScene;
            if(scene == null)
                return;

            int width = 25;
            int height = 19;
            if(ShowMapSizeInputDialog(ref width, ref height))
            {
                var newMap = new Map(width, height);
                scene.SceneMaps.Add(newMap);
                NeedsToBeSaved = true;
                RefreshMapsTab();
            }
        }
        private void RemoveMapButton_Click(object sender, EventArgs e)
        {
            var scene = EditorContextManager.ActiveLoadedScene;
            if(scene == null || MapGridDataView.SelectedRows.Count == 0)
                return;

            var selectedMap = MapGridDataView.SelectedRows[0].DataBoundItem as Map;
            if(selectedMap != null)
            {
                if(scene.SceneMaps.Count <= 1)
                {
                    MessageBox.Show("A scene must contain at least one map.", "Action Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                scene.SceneMaps.Remove(selectedMap);
                if(scene.SceneMap == selectedMap)
                {
                    scene.SceneMap = scene.SceneMaps[0]; // Fallback to another map
                }
                NeedsToBeSaved = true;
                RefreshMapsTab();
            }
        }
        private void InitializeManagersTab()
        {
            // managersListView setup
            ManagerListView.View = View.Details;
            ManagerListView.FullRowSelect = true;
            ManagerListView.Columns.Clear();
            ManagerListView.Columns.Add("Manager Type", 200, HorizontalAlignment.Left);
            ManagerListView.Columns.Add("Assembly", 250, HorizontalAlignment.Left);

            // Wire up Add Manager Dropdown
            if(AddManagerDropdownButton != null)
            {
                AddManagerDropdownButton.DropDown = new ContextMenuStrip();
                AddManagerDropdownButton.DropDownOpening += (s, e) =>
                {
                    AddManagerDropdownButton.DropDownItems.Clear();
                    var scene = EditorContextManager.ActiveLoadedScene;
                    if(scene == null)
                        return;

                    var managerTypes = new System.Collections.Generic.List<Type>();

                    // Scan core and user assemblies for GameManager subclasses
                    var coreAssembly = typeof(GameManager).Assembly;
                    managerTypes.AddRange(coreAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(GameManager)) && !t.IsAbstract));

                    if(_scriptManager.CurrentAssembly != null)
                    {
                        try
                        {
                            var userTypes = _scriptManager.CurrentAssembly.GetTypes()
                                .Where(t => t.IsSubclassOf(typeof(GameManager)) && !t.IsAbstract);
                            managerTypes.AddRange(userTypes);
                        }
                        catch(Exception ex)
                        {
                            Log.Error($"[Manager Scan Error]: {ex.Message}");
                        }
                    }

                    foreach(var type in managerTypes)
                    {
                        var item = new ToolStripMenuItem(type.Name);
                        bool alreadyExists = scene.Managers.GetRegisteredManagers().Any(m => m.GetType() == type);

                        if(alreadyExists)
                        {
                            item.Enabled = false;
                            item.Text += " (Already Added)";
                        }

                        item.Click += (subSender, subArgs) =>
                        {
                            if(Activator.CreateInstance(type) is GameManager newManager)
                            {
                                newManager.ContextScene = scene;
                                scene.Managers.AddManager(newManager);
                                NeedsToBeSaved = true;
                                RefreshManagersTab();
                                Log.Info($"[Editor UI] Added manager '{type.Name}' to scene.");
                            }
                        };

                        AddManagerDropdownButton.DropDownItems.Add(item);
                    }
                };
            }

            // Wire up Remove Manager Button
            if(RemoveManagerButton != null)
            {
                RemoveManagerButton.Click += (s, e) =>
                {
                    var scene = EditorContextManager.ActiveLoadedScene;
                    if(scene == null || ManagerListView.SelectedItems.Count == 0)
                        return;

                    var managerInstance = ManagerListView.SelectedItems[0].Tag as GameManager;
                    if(managerInstance != null)
                    {
                        scene.Managers.RemoveManager(managerInstance);
                        NeedsToBeSaved = true;
                        RefreshManagersTab();
                        Log.Info($"[Editor UI] Removed manager '{managerInstance.GetType().Name}' from scene.");
                    }
                };
            }
        }
        private void RefreshManagersTab()
        {
            if(EditorContextManager.ActiveLoadedScene == null)
                return;

            ManagerListView.Items.Clear();
            var managers = EditorContextManager.ActiveLoadedScene.Managers.GetRegisteredManagers();

            foreach(var manager in managers)
            {
                var listViewItem = new ListViewItem(manager.GetType().Name);
                listViewItem.SubItems.Add(manager.GetType().Assembly.GetName().Name ?? "Unknown");
                listViewItem.Tag = manager;
                ManagerListView.Items.Add(listViewItem);
            }
        }
        private void InitializeSystemsTab()
        {
            // systemsListView setup
            SystemListView.View = View.Details;
            SystemListView.FullRowSelect = true;
            SystemListView.Columns.Clear();
            SystemListView.Columns.Add("System Type", 200, HorizontalAlignment.Left);
            SystemListView.Columns.Add("Update Policy", 120, HorizontalAlignment.Left);

            // Wire up Add System Dropdown
            if(AddSystemDropdownButton != null)
            {
                AddSystemDropdownButton.DropDown = new ContextMenuStrip();
                AddSystemDropdownButton.DropDownOpening += (s, e) =>
                {
                    AddSystemDropdownButton.DropDownItems.Clear();
                    var scene = EditorContextManager.ActiveLoadedScene;
                    if(scene == null)
                        return;

                    var systemTypes = new System.Collections.Generic.List<Type>();

                    var coreAssembly = typeof(GameSystem).Assembly;
                    systemTypes.AddRange(coreAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(GameSystem)) && !t.IsAbstract));

                    if(_scriptManager.CurrentAssembly != null)
                    {
                        try
                        {
                            var userTypes = _scriptManager.CurrentAssembly.GetTypes()
                                .Where(t => t.IsSubclassOf(typeof(GameSystem)) && !t.IsAbstract);
                            systemTypes.AddRange(userTypes);
                        }
                        catch(Exception ex)
                        {
                            Log.Error($"[System Scan Error]: {ex.Message}");
                        }
                    }

                    foreach(var type in systemTypes)
                    {
                        var item = new ToolStripMenuItem(type.Name);
                        bool alreadyExists = scene.Systems._systemEntityCache.Keys.Any(s => s.GetType() == type);

                        if(alreadyExists)
                        {
                            item.Enabled = false;
                            item.Text += " (Already Added)";
                        }

                        item.Click += (subSender, subArgs) =>
                        {
                            if(Activator.CreateInstance(type) is GameSystem newSystem)
                            {
                                newSystem.ContextScene = scene;
                                scene.Systems.AddSystem(newSystem);
                                NeedsToBeSaved = true;
                                RefreshSystemsTab();
                                Log.Info($"[Editor UI] Added system '{type.Name}' to scene.");
                            }
                        };

                        AddSystemDropdownButton.DropDownItems.Add(item);
                    }
                };
            }

            // Wire up Remove System Button
            if(RemoveSystemButton != null)
            {
                RemoveSystemButton.Click += (s, e) =>
                {
                    var scene = EditorContextManager.ActiveLoadedScene;
                    if(scene == null || SystemListView.SelectedItems.Count == 0)
                        return;

                    var systemInstance = SystemListView.SelectedItems[0].Tag as GameSystem;
                    if(systemInstance != null)
                    {
                        // Prevent removing core engine structural systems if desired
                        if(systemInstance is TransformSystem || systemInstance is SpriteRenderSystem || systemInstance is PhysicsSystem || systemInstance is ScriptComponentSystem || systemInstance is UIInputSystem || systemInstance is UILayoutSystem || systemInstance is UIRenderSystem)
                        {
                            MessageBox.Show("Core engine systems cannot be removed.", "Action Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        scene.Systems.RemoveSystem(systemInstance);
                        NeedsToBeSaved = true;
                        RefreshSystemsTab();
                        Log.Info($"[Editor UI] Removed system '{systemInstance.GetType().Name}' from scene.");
                    }
                };
            }
        }

        private void RefreshSystemsTab()
        {
            if(EditorContextManager.ActiveLoadedScene == null)
                return;

            SystemListView.Items.Clear();
            var systems = EditorContextManager.ActiveLoadedScene.Systems._systemEntityCache.Keys;

            foreach(var system in systems)
            {
                var listViewItem = new ListViewItem(system.GetType().Name);
                listViewItem.SubItems.Add(system.UpdatePolicy.ToString());
                listViewItem.Tag = system;
                SystemListView.Items.Add(listViewItem);
            }
        }

        private void InitializeTilesetMetadataTab()
        {
            splitContainer4.Panel2.Controls.Clear();

            // Main layout container for Panel2 (Split into Image Viewer on left, Controls on right)
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));

            // Left Side: Scrollable Canvas for the Tileset Image
            tilesetCanvasPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = System.Drawing.Color.FromArgb(45, 45, 48)
            };

            tilesetPictureBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.AutoSize,
                BackColor = System.Drawing.Color.Transparent
            };
            tilesetPictureBox.Paint += TilesetPictureBox_Paint;
            tilesetPictureBox.MouseClick += TilesetPictureBox_MouseClick;

            tilesetCanvasPanel.Controls.Add(tilesetPictureBox);
            layout.Controls.Add(tilesetCanvasPanel, 0, 0);

            // Right Side: Property Editing Controls
            var propPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            propPanel.Controls.Add(new Label { Text = "Tileset Tile Properties", Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold), AutoSize = true, ForeColor = System.Drawing.Color.White });

            // FIX: Assign to the class-level field instead of declaring a local variable with 'var'
            selectedTileLabel = new Label { Text = "Selected Tile: None", AutoSize = true, ForeColor = System.Drawing.Color.Black };
            propPanel.Controls.Add(selectedTileLabel);

            var valueLabel = new Label { Text = "Custom Int Value:", AutoSize = true, ForeColor = System.Drawing.Color.Black };
            propPanel.Controls.Add(valueLabel);

            // FIX: Assign to the class-level field instead of declaring a local variable with 'var'
            tileValueNumeric = new NumericUpDown
            {
                Minimum = -9999,
                Maximum = 9999,
                Width = 80,
                Enabled = false
            };
            tileValueNumeric.ValueChanged += TileValueNumeric_ValueChanged;
            propPanel.Controls.Add(tileValueNumeric);

            layout.Controls.Add(propPanel, 1, 0);
            splitContainer4.Panel2.Controls.Add(layout);
        }
        private void RefreshTilesetMetadataPanel()
        {
            var map = GetSelectedMap();
            if(map == null || string.IsNullOrEmpty(map.TileSetPath))
            {
                if(currentTilesetBitmap != null)
                {
                    currentTilesetBitmap.Dispose();
                    currentTilesetBitmap = null;
                }
                tilesetPictureBox.Image = null;
                selectedTileIndex = -1;
                selectedTileLabel.Text = "Selected Tile: None";
                tileValueNumeric.Enabled = false;
                return;
            }

            string fullPath = Path.Combine(EditorContextManager.ContentPath, map.TileSetPath);
            if(File.Exists(fullPath))
            {
                try
                {
                    // Load bitmap safely without locking the file handle
                    using(var tempBmp = new System.Drawing.Bitmap(fullPath))
                    {
                        currentTilesetBitmap?.Dispose();
                        currentTilesetBitmap = new System.Drawing.Bitmap(tempBmp);
                    }
                    tilesetPictureBox.Image = currentTilesetBitmap;
                }
                catch(Exception ex)
                {
                    Log.Error($"[Editor] Failed to load tileset image: {ex.Message}");
                    tilesetPictureBox.Image = null;
                }
            }
            else
            {
                tilesetPictureBox.Image = null;
            }

            selectedTileIndex = -1;
            selectedTileLabel.Text = "Selected Tile: None";
            tileValueNumeric.Enabled = false;
            tilesetPictureBox.Invalidate();
        }

        private bool _isSuppressingTileValueChange = false;

        private void TilesetPictureBox_Paint(object sender, PaintEventArgs e)
        {
            var map = GetSelectedMap();
            if(map == null || currentTilesetBitmap == null || map.TileSize <= 0)
                return;

            int tileSize = map.TileSize;
            int cols = currentTilesetBitmap.Width / tileSize;
            int rows = currentTilesetBitmap.Height / tileSize;

            using(var gridPen = new Pen(System.Drawing.Color.FromArgb(100, 255, 255, 255), 1))
            {
                // Draw grid lines
                for(int x = 0; x <= currentTilesetBitmap.Width; x += tileSize)
                {
                    e.Graphics.DrawLine(gridPen, x, 0, x, currentTilesetBitmap.Height);
                }
                for(int y = 0; y <= currentTilesetBitmap.Height; y += tileSize)
                {
                    e.Graphics.DrawLine(gridPen, 0, y, currentTilesetBitmap.Width, y);
                }
            }

            // Highlight selected tile if valid
            if(selectedTileIndex >= 0)
            {
                int col = selectedTileIndex % cols;
                int row = selectedTileIndex / cols;
                var rect = new System.Drawing.Rectangle(col * tileSize, row * tileSize, tileSize, tileSize);

                using(var highlightPen = new Pen(System.Drawing.Color.Yellow, 2))
                {
                    e.Graphics.DrawRectangle(highlightPen, rect);
                }

                // Key = Tile Index, Value = Assigned Custom Int
                int assignedVal = 0;
                bool hasVal = false;
                if(map.TileProperties != null && map.TileProperties.TryGetValue(selectedTileIndex, out int val))
                {
                    assignedVal = val;
                    hasVal = true;
                }

                if(hasVal)
                {
                    using(var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Yellow))
                    {
                        e.Graphics.DrawString(assignedVal.ToString(), System.Drawing.SystemFonts.DefaultFont, brush, rect.X + 2, rect.Y + 2);
                    }
                }
            }
        }

        private void TilesetPictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            var map = GetSelectedMap();
            if(map == null || currentTilesetBitmap == null || map.TileSize <= 0)
                return;

            int tileSize = map.TileSize;
            int cols = currentTilesetBitmap.Width / tileSize;
            int maxCols = currentTilesetBitmap.Width / tileSize;
            int maxRows = currentTilesetBitmap.Height / tileSize;

            int clickedCol = e.X / tileSize;
            int clickedRow = e.Y / tileSize;

            if(clickedCol >= 0 && clickedCol < maxCols && clickedRow >= 0 && clickedRow < maxRows)
            {
                selectedTileIndex = clickedRow * cols + clickedCol;
                selectedTileLabel.Text = $"Selected Tile Index: {selectedTileIndex}";

                tileValueNumeric.Enabled = true;

                // Key = Tile Index, Value = Assigned Custom Int
                int existingVal = 0;
                if(map.TileProperties != null && map.TileProperties.TryGetValue(selectedTileIndex, out int val))
                {
                    existingVal = val;
                }

                _isSuppressingTileValueChange = true;
                tileValueNumeric.Value = existingVal;
                _isSuppressingTileValueChange = false;

                tilesetPictureBox.Invalidate();
            }
        }

        private void TileValueNumeric_ValueChanged(object sender, EventArgs e)
        {
            if(_isSuppressingTileValueChange)
                return;

            var map = GetSelectedMap();
            if(map == null || selectedTileIndex < 0)
                return;

            int val = (int) tileValueNumeric.Value;

            if(map.TileProperties == null)
            {
                map.TileProperties = new Dictionary<int, int>();
            }

            // Store the value directly in the dictionary, including 0!
            map.TileProperties[selectedTileIndex] = val;

            NeedsToBeSaved = true;
            tilesetPictureBox.Invalidate();
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
                            //NeedsToBeSaved = true;

                            // Rebuild and refresh the card view layout panel
                            RebuildInspectorPanel(selectedGo, forceRebuild: true);
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
                    RebuildInspectorPanel(selectedGo, forceRebuild : true);
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

                        // 💡 FIX: Re-attach the scene events to the new scene's entity manager!
                        AttachSceneEvents(revertedScene);

                        // Repopulate the main tree view panel UI
                        if(Form1.ActiveHierarchyTreeView != null)
                        {
                            Form1.ActiveHierarchyTreeView.BeginInvoke(new Action(() =>
                            {
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

        private Map GetSelectedMap()
        {
            if(MapGridDataView.SelectedRows.Count > 0 &&
                MapGridDataView.SelectedRows[0].DataBoundItem is Map selectedMap)
            {
                return selectedMap;
            }

            return null;
        }


        private bool ShowIntegerInputDialog(string title, string promptText, ref int value)
        {
            Form inputForm = new Form()
            {
                Width = 300,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label label = new Label() { Left = 20, Top = 20, Text = promptText, AutoSize = true };
            NumericUpDown numUpDown = new NumericUpDown() { Left = 20, Top = 50, Width = 240, Minimum = 4, Maximum = 256, Value = value };
            Button confirmation = new Button() { Text = "OK", Left = 100, Width = 80, Top = 85, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Cancel", Left = 185, Width = 75, Top = 85, DialogResult = DialogResult.Cancel };

            confirmation.Click += (sender, e) => { inputForm.Close(); };
            cancel.Click += (sender, e) => { inputForm.Close(); };

            inputForm.Controls.Add(label);
            inputForm.Controls.Add(numUpDown);
            inputForm.Controls.Add(confirmation);
            inputForm.Controls.Add(cancel);
            inputForm.AcceptButton = confirmation;
            inputForm.CancelButton = cancel;

            if(inputForm.ShowDialog() == DialogResult.OK)
            {
                value = (int) numUpDown.Value;
                return true;
            }
            return false;
        }

        // Helper to prompt for Map Dimensions (Width & Height)
        private bool ShowMapSizeInputDialog(ref int width, ref int height)
        {
            Form inputForm = new Form()
            {
                Width = 320,
                Height = 190,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Resize Map",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblWidth = new Label() { Left = 20, Top = 20, Text = "Width (Tiles):", AutoSize = true };
            NumericUpDown numWidth = new NumericUpDown() { Left = 120, Top = 18, Width = 150, Minimum = 1, Maximum = 500, Value = width };

            Label lblHeight = new Label() { Left = 20, Top = 55, Text = "Height (Tiles):", AutoSize = true };
            NumericUpDown numHeight = new NumericUpDown() { Left = 120, Top = 53, Width = 150, Minimum = 1, Maximum = 500, Value = height };

            Button confirmation = new Button() { Text = "OK", Left = 110, Width = 80, Top = 105, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Cancel", Left = 195, Width = 75, Top = 105, DialogResult = DialogResult.Cancel };

            inputForm.Controls.Add(lblWidth);
            inputForm.Controls.Add(numWidth);
            inputForm.Controls.Add(lblHeight);
            inputForm.Controls.Add(numHeight);
            inputForm.Controls.Add(confirmation);
            inputForm.Controls.Add(cancel);
            inputForm.AcceptButton = confirmation;
            inputForm.CancelButton = cancel;

            if(inputForm.ShowDialog() == DialogResult.OK)
            {
                width = (int) numWidth.Value;
                height = (int) numHeight.Value;
                return true;
            }
            return false;
        }
    }
}
