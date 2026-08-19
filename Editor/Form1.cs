using Editor;
using Engine.Content.Builder;
using Engine.Core.Collections;
using Engine.Core.ECS;
using Engine.Core.ECS.Components;
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
using System.Text.Json;
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

        private ComboBox tileDatabaseComboBox;
        private ComboBox tileDataComponentComboBox;
        private bool _isSuppressingDatabaseChange = false;
        private bool _isSuppressingComponentChange = false;

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

            DateTime lastTriggerTime = DateTime.MinValue;

            _scriptWatcher.Changed += (s, e) => TriggerScriptRebuildThrottled(ref lastTriggerTime);
            _scriptWatcher.Created += (s, e) => TriggerScriptRebuildThrottled(ref lastTriggerTime);
            _scriptWatcher.Renamed += (s, e) => TriggerScriptRebuildThrottled(ref lastTriggerTime);

            _scriptWatcher.EnableRaisingEvents = true;
            Log.Info("[Script Manager] Background file system watcher active on Source scripts.");
        }

        private void TriggerScriptRebuildThrottled(ref DateTime lastTriggerTime)
        {
            lock(this)
            {
                if((DateTime.Now - lastTriggerTime).TotalMilliseconds < 1000)
                    return;
                lastTriggerTime = DateTime.Now;
            }

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

                string absoluteBinContentPath = Path.Combine(EditorContextManager.BinPath, "Content");
                mgWindowControl.Editor.Content.RootDirectory = absoluteBinContentPath;

                string targetScenePath = Path.Combine(EditorContextManager.ScenesPath, "Main.scene");

                if(File.Exists(targetScenePath))
                {
                    _isSuprressingDirtyFlag = true;
                    try
                    {
                        Log.Info($"[Editor UI] Found existing workspace state file. Deserializing active layout tree...");

                        var loadedScene = new GameScene();
                        loadedScene = SceneSerializer.LoadScene(targetScenePath);
                        loadedScene.resetContextSceneInManagers();

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

                    this.Invoke(new Action(() =>
                    {
                        Log.Info("[Content Builder] Content build complete.");
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

            FindAndRefreshGrid(ActiveInspectorPanel, targetComponent);
        }

        private static bool FindAndRefreshGrid(Control parent, object targetComponent)
        {
            foreach(Control child in parent.Controls)
            {
                if(child is PropertyGrid grid && grid.Tag == targetComponent)
                {
                    grid.Refresh();
                    return true;
                }

                if(child.HasChildren)
                {
                    if(FindAndRefreshGrid(child, targetComponent))
                    {
                        return true;
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

                        var loadedScene = new GameScene();
                        loadedScene = SceneSerializer.LoadScene(targetScenePath);

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
                        NeedsToBeSaved = false;
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

                var loadedScene = sceneToLoad;
                loadedScene = SceneSerializer.LoadScene(targetScenePath);

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

                string sceneFileName = $"{EditorContextManager.ActiveLoadedScene.SceneName}.scene";
                string targetScenePath = Path.Combine(EditorContextManager.CurrentProjectRoot, "Content", "Assets", "Scenes", sceneFileName);

                string directoryCheck = Path.GetDirectoryName(targetScenePath);
                if(!string.IsNullOrEmpty(directoryCheck) && !Directory.Exists(directoryCheck))
                {
                    Directory.CreateDirectory(directoryCheck);
                }

                SceneSerializer.SaveScene(EditorContextManager.ActiveLoadedScene, targetScenePath);

                Log.Info($"Project workspace and active scene layout saved successfully.");

                _needsToBeSaved = false;
            }
            catch(Exception ex)
            {
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

            _masterLogHistory.Add((Severity: severity, Message: formattedText));

            string activeFilter = consoleSearchBar.SearchQuery;

            if(string.IsNullOrEmpty(activeFilter) || formattedText.Contains(activeFilter, StringComparison.CurrentCultureIgnoreCase))
            {
                ConsoleTextBox.BeginUpdate();

                System.Drawing.Color logColor;
                switch(severity)
                {
                    case LogSeverity.Info:
                        logColor = System.Drawing.Color.DarkGreen;
                        break;
                    case LogSeverity.Warning:
                        logColor = System.Drawing.Color.DarkGoldenrod;
                        break;
                    case LogSeverity.Error:
                        logColor = System.Drawing.Color.DarkRed;
                        break;
                    case LogSeverity.Print:
                    default:
                        logColor = ConsoleTextBox.ForeColor;
                        break;
                }

                ConsoleTextBox.SelectionStart = ConsoleTextBox.TextLength;
                ConsoleTextBox.SelectionLength = 0;
                ConsoleTextBox.SelectionColor = logColor;

                ConsoleTextBox.AppendText(formattedText + Environment.NewLine);

                ConsoleTextBox.SelectionColor = ConsoleTextBox.ForeColor;

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

            string gumProjectDir = Path.Combine(EditorContextManager.CurrentProjectRoot, "Content", "GumProject");
            if(!Directory.Exists(gumProjectDir))
            {
                Directory.CreateDirectory(gumProjectDir);
            }

            string[] gumFiles = Directory.GetFiles(gumProjectDir, "*.gumx");
            string gumFilePath = string.Empty;

            if(gumFiles.Length > 0)
            {
                gumFilePath = gumFiles[0];
            }
            else
            {
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

            Panel goCard = Engine.Editor.WinFormsApp1.ComponentCardFactory.CreateCard(
                "GameObject Properties",
                targetGo,
                cardWidth,
                previouslySelected
            );
            goCard.Tag = targetGo;
            flowLayout.Controls.Add(goCard);

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

        private GameObject? GetSelectedGameObjectFromHierarchy()
        {
            if(SceneHierarchyTreeView.SelectedNode == null)
                return null;

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
                var oldContext = System.Threading.SynchronizationContext.Current;
                BuildResult result;
                try
                {
                    System.Threading.SynchronizationContext.SetSynchronizationContext(null);

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
            MapGridDataView.AutoGenerateColumns = false;
            MapGridDataView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            MapGridDataView.MultiSelect = false;
            MapGridDataView.ReadOnly = false;

            MapGridDataView.DataError += (sender, e) =>
            {
                // Suppress default WinForms modal error popups
                e.ThrowException = false;
            };

            MapGridDataView.Columns.Clear();

            MapGridDataView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "MapName",
                HeaderText = "Map Name",
                Name = "ColMapName"
            });

            MapGridDataView.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "IsEnabled",
                HeaderText = "Enabled",
                Name = "ColIsEnabled"
            });

            MapGridDataView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "LayerOrder",
                HeaderText = "Layer Order",
                Name = "ColLayerOrder"
            });

            MapGridDataView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Width",
                HeaderText = "Width (Tiles)",
                Name = "ColWidth"
            });

            MapGridDataView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Height",
                HeaderText = "Height (Tiles)",
                Name = "ColHeight"
            });

            MapGridDataView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "TileSize",
                HeaderText = "Tile Size",
                Name = "ColTileSize"
            });

            // --- Tile Database ComboBox Column (Bound to String Name/Path) ---
            var tileDatabaseColumn = new DataGridViewComboBoxColumn
            {
                DataPropertyName = "TileDatabaseName", // Bound to string instead of Database object
                HeaderText = "Tile Database",
                Name = "ColTileDatabase",
                DisplayMember = "DisplayName",
                ValueMember = "FilePath"
            };

            var dbOptions = new List<DatabaseOption>();
            dbOptions.Add(new DatabaseOption { DisplayName = "(None)", FilePath = string.Empty });

            string contentDirectory = EditorContextManager.ContentPath;
            if(Directory.Exists(contentDirectory))
            {
                string[] dbFiles = Directory.GetFiles(contentDirectory, "*.database", SearchOption.AllDirectories);
                foreach(string file in dbFiles)
                {
                    string relPath = Path.GetRelativePath(contentDirectory, file).Replace('\\', '/');
                    // Strip .database extension for the stored string name
                    if(relPath.EndsWith(".database", StringComparison.OrdinalIgnoreCase))
                    {
                        relPath = relPath.Substring(0, relPath.Length - 9);
                    }
                    string name = Path.GetFileName(file);
                    dbOptions.Add(new DatabaseOption { DisplayName = name, FilePath = relPath });
                }
            }
            tileDatabaseColumn.DataSource = dbOptions;
            MapGridDataView.Columns.Add(tileDatabaseColumn);

            // --- Tileset ComboBox Column ---
            var tilesetColumn = new DataGridViewComboBoxColumn
            {
                DataPropertyName = "TileSetPath",
                HeaderText = "Tileset",
                Name = "ColTilesetPath",
                DisplayMember = "DisplayName",
                ValueMember = "FilePath"
            };
            MapGridDataView.Columns.Add(tilesetColumn);

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

            // Automatically resolve and assign the Database reference whenever TileDatabaseName changes
            MapGridDataView.CellValueChanged += (s, e) =>
            {
                NeedsToBeSaved = true;
                mgWindowControl?.Invalidate();

                if(e.ColumnIndex >= 0 && MapGridDataView.Columns[e.ColumnIndex].Name == "ColTileDatabase")
                {
                    if(MapGridDataView.Rows[e.RowIndex].DataBoundItem is Map map)
                    {
                        var scene = EditorContextManager.ActiveLoadedScene;
                        if(scene?.Database != null)
                        {
                            map.TileDatabase = scene.Database.Databases.FirstOrDefault(db =>
                                db.Name.Equals(map.TileDatabaseName, StringComparison.OrdinalIgnoreCase));
                        }
                    }

                    if(e.ColumnIndex >= 0 && MapGridDataView.Columns[e.ColumnIndex].Name == "ColTilesetPath")
                    {
                        RefreshTilesetMetadataPanel();
                    }
                }
            };
            
        }
            
        private void MapGridDataView_SelectionChanged(object sender, EventArgs e)
        {
            var scene = EditorContextManager.ActiveLoadedScene;
            if(scene == null || MapGridDataView == null)
                return;

            if(MapGridDataView.SelectedRows.Count == 0 || MapGridDataView.CurrentRow == null || MapGridDataView.CurrentRow.Index < 0)
                return;

            if(MapGridDataView.SelectedRows[0].DataBoundItem is Map selectedMap)
            {
                int index = scene.SceneMaps.IndexOf(selectedMap);
                if(index >= 0)
                {
                    scene.ActiveMapIndex = index;
                }
                mgWindowControl?.Invalidate();
            }

            RefreshTilesetMetadataPanel();
        }

        private void RefreshMapsTab()
        {
            var scene = EditorContextManager.ActiveLoadedScene;
            if(scene == null)
                return;

            // 1. Re-populate Database Dropdown Options
            var dbOptions = new List<DatabaseOption>
    {
        new DatabaseOption { DisplayName = "(None)", FilePath = string.Empty }
    };

            string contentDir = EditorContextManager.ContentPath;
            if(Directory.Exists(contentDir))
            {
                string[] dbFiles = Directory.GetFiles(contentDir, "*.database", SearchOption.AllDirectories);
                foreach(string file in dbFiles)
                {
                    string relPath = Path.GetRelativePath(contentDir, file).Replace('\\', '/');
                    if(relPath.EndsWith(".database", StringComparison.OrdinalIgnoreCase))
                    {
                        relPath = relPath.Substring(0, relPath.Length - 9);
                    }
                    string name = Path.GetFileNameWithoutExtension(file);
                    dbOptions.Add(new DatabaseOption { DisplayName = name, FilePath = relPath });
                }
            }

            if(MapGridDataView.Columns["ColTileDatabase"] is DataGridViewComboBoxColumn dbColumn)
            {
                dbColumn.DataSource = dbOptions;
                dbColumn.DisplayMember = "DisplayName";
                dbColumn.ValueMember = "FilePath";
            }

            // 2. Re-populate Tileset Dropdown Options
            var tilesetOptions = new List<object>
    {
        new { DisplayName = "(None)", FilePath = string.Empty }
    };

            if(Directory.Exists(contentDir))
            {
                string[] imageFiles = Directory.GetFiles(contentDir, "*.png", SearchOption.AllDirectories);
                foreach(string file in imageFiles)
                {
                    string relPath = Path.GetRelativePath(contentDir, file).Replace('\\', '/');
                    string fileName = Path.GetFileName(file);
                    tilesetOptions.Add(new
                    {
                        DisplayName = fileName,
                        FilePath = relPath
                    });
                }
            }

            if(MapGridDataView.Columns["ColTilesetPath"] is DataGridViewComboBoxColumn tilesetColumn)
            {
                tilesetColumn.DataSource = tilesetOptions;
                tilesetColumn.DisplayMember = "DisplayName";
                tilesetColumn.ValueMember = "FilePath";
            }

            // 3. Ensure existing map values exist in dropdown options to prevent mismatch errors
            foreach(var map in scene.SceneMaps)
            {
                if(!string.IsNullOrEmpty(map.TileDatabaseName) && !dbOptions.Any(o => o.FilePath.Equals(map.TileDatabaseName, StringComparison.OrdinalIgnoreCase)))
                {
                    dbOptions.Add(new DatabaseOption { DisplayName = map.TileDatabaseName, FilePath = map.TileDatabaseName });
                }
            }

            // 4. Bind main grid DataSource LAST
            MapGridDataView.DataSource = null;
            MapGridDataView.DataSource = scene.SceneMaps;
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
                NeedsToBeSaved = true;
                RefreshMapsTab();
            }
        }

        private void InitializeManagersTab()
        {
            ManagerListView.View = View.Details;
            ManagerListView.FullRowSelect = true;
            ManagerListView.Columns.Clear();
            ManagerListView.Columns.Add("Manager Type", 200, HorizontalAlignment.Left);
            ManagerListView.Columns.Add("Assembly", 250, HorizontalAlignment.Left);

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
            SystemListView.View = View.Details;
            SystemListView.FullRowSelect = true;
            SystemListView.Columns.Clear();
            SystemListView.Columns.Add("System Type", 200, HorizontalAlignment.Left);
            SystemListView.Columns.Add("Update Policy", 120, HorizontalAlignment.Left);

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

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));

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

            var propPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };

            propPanel.Controls.Add(new Label { Text = "Tileset Tile Properties", Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold), AutoSize = true, ForeColor = System.Drawing.Color.White });

            selectedTileLabel = new Label { Text = "Selected Tile: None", AutoSize = true, ForeColor = System.Drawing.Color.Black };
            propPanel.Controls.Add(selectedTileLabel);

            var valueLabel = new Label { Text = "Custom Int Value:", AutoSize = true, ForeColor = System.Drawing.Color.Black };
            propPanel.Controls.Add(valueLabel);

            tileValueNumeric = new NumericUpDown
            {
                Minimum = -9999,
                Maximum = 9999,
                Width = 80,
                Enabled = false
            };
            tileValueNumeric.ValueChanged += TileValueNumeric_ValueChanged;
            propPanel.Controls.Add(tileValueNumeric);

            // --- Database & DataComponent Assignment Controls ---
            propPanel.Controls.Add(new Label { Text = "Tile Database:", AutoSize = true, ForeColor = System.Drawing.Color.Black, Margin = new Padding(0, 10, 0, 0) });
            tileDatabaseComboBox = new ComboBox
            {
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            tileDatabaseComboBox.SelectedIndexChanged += TileDatabaseComboBox_SelectedIndexChanged;
            propPanel.Controls.Add(tileDatabaseComboBox);

            propPanel.Controls.Add(new Label { Text = "Data Component:", AutoSize = true, ForeColor = System.Drawing.Color.Black, Margin = new Padding(0, 5, 0, 0) });
            tileDataComponentComboBox = new ComboBox
            {
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            tileDataComponentComboBox.SelectedIndexChanged += TileDataComponentComboBox_SelectedIndexChanged;
            propPanel.Controls.Add(tileDataComponentComboBox);

            layout.Controls.Add(propPanel, 1, 0);
            splitContainer4.Panel2.Controls.Add(layout);
        }

        private void PopulateDatabaseDropdown(Map map)
        {
            _isSuppressingDatabaseChange = true;
            tileDatabaseComboBox.Items.Clear();
            tileDatabaseComboBox.Items.Add(new DatabaseOption { DisplayName = "(None)", FilePath = string.Empty });

            var scene = EditorContextManager.ActiveLoadedScene;
            if(scene?.Database?.Databases != null)
            {
                foreach(var db in scene.Database.Databases)
                {
                    tileDatabaseComboBox.Items.Add(new DatabaseOption { DisplayName = db.Name, FilePath = db.Name });
                }
            }

            tileDatabaseComboBox.SelectedIndex = 0;
            _isSuppressingDatabaseChange = false;
        }

        private void RefreshTileDataComponentDropdown(Map map)
        {
            _isSuppressingComponentChange = true;
            tileDataComponentComboBox.Items.Clear();
            tileDataComponentComboBox.Items.Add("(None)");

            if(map != null && map.TileDatabase != null)
            {
                try
                {
                    var prop = map.TileDatabase.GetType().GetProperty("Components") ?? map.TileDatabase.GetType().GetProperty("Items");
                    if(prop != null && prop.GetValue(map.TileDatabase) is System.Collections.IEnumerable enumerable)
                    {
                        foreach(var comp in enumerable)
                        {
                            tileDataComponentComboBox.Items.Add(comp);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Log.Error($"[Metadata Panel] Failed to extract components from database: {ex.Message}");
                }
            }

            tileDataComponentComboBox.DisplayMember = "Name";
            if(tileDataComponentComboBox.Items.Count > 0)
            {
                tileDataComponentComboBox.SelectedIndex = 0;
            }
            _isSuppressingComponentChange = false;
        }

        private void TileDatabaseComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(_isSuppressingDatabaseChange)
                return;

            var map = GetSelectedMap();
            if(map == null)
                return;

            if(tileDatabaseComboBox.SelectedItem is DatabaseOption dbItem)
            {
                if(string.IsNullOrEmpty(dbItem.FilePath))
                {
                    map.TileDatabase = null;
                    map.TileDatabaseName = string.Empty;
                }
                else
                {
                    map.TileDatabaseName = dbItem.FilePath; // Stored without .database
                    string fullPath = Path.Combine(EditorContextManager.ContentPath, dbItem.FilePath + ".database");
                    try
                    {
                        if(File.Exists(fullPath))
                        {
                            string json = File.ReadAllText(fullPath);
                            map.TileDatabase = JsonSerializer.Deserialize<Database>(json);
                        }
                    }
                    catch(Exception ex)
                    {
                        Log.Error($"[Database] Failed to deserialize database: {ex.Message}");
                        map.TileDatabase = null;
                    }
                }

                NeedsToBeSaved = true;
                RefreshTileDataComponentDropdown(map);
            }
        }

        private void TileDataComponentComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(_isSuppressingComponentChange)
                return;

            var map = GetSelectedMap();
            if(map == null || selectedTileIndex < 0)
                return;

            if(tileDataComponentComboBox.SelectedItem is DataComponent selectedComponent)
            {
                if(map.TileIndexDataDictionary == null)
                {
                    map.TileIndexDataDictionary = new Dictionary<int, DataComponent>();
                }

                map.TileIndexDataDictionary[selectedTileIndex] = selectedComponent;
                NeedsToBeSaved = true;
                tilesetPictureBox.Invalidate();
            }
            else
            {
                // Selected "(None)" item
                if(map.TileIndexDataDictionary != null && map.TileIndexDataDictionary.ContainsKey(selectedTileIndex))
                {
                    map.TileIndexDataDictionary.Remove(selectedTileIndex);
                    NeedsToBeSaved = true;
                    tilesetPictureBox.Invalidate();
                }
            }
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
                EditorContextManager.SelectedTileIndex = selectedTileIndex;
                selectedTileLabel.Text = "Selected Tile: None";
                tileValueNumeric.Enabled = false;
                tileDatabaseComboBox.Enabled = false;
                tileDataComponentComboBox.Enabled = false;
                tilesetPictureBox.Invalidate();
                return;
            }

            string fullPath = Path.Combine(EditorContextManager.ContentPath, map.TileSetPath);
            if(File.Exists(fullPath))
            {
                try
                {
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

            // Populate Database list options
            PopulateDatabaseDropdown(map);

            if(selectedTileIndex >= 0)
            {
                selectedTileLabel.Text = $"Selected Tile Index: {selectedTileIndex}";
                tileValueNumeric.Enabled = true;
                tileDatabaseComboBox.Enabled = true;
                tileDataComponentComboBox.Enabled = true;

                int existingVal = 0;
                if(map.TileProperties != null && map.TileProperties.TryGetValue(selectedTileIndex, out int val))
                {
                    existingVal = val;
                }

                _isSuppressingTileValueChange = true;
                tileValueNumeric.Value = existingVal;
                _isSuppressingTileValueChange = false;

                // Populate and select assigned DataComponent if present
                RefreshTileDataComponentDropdown(map);
                if(map.TileIndexDataDictionary != null && map.TileIndexDataDictionary.TryGetValue(selectedTileIndex, out var assignedComp))
                {
                    _isSuppressingComponentChange = true;
                    tileDataComponentComboBox.SelectedItem = assignedComp;
                    _isSuppressingComponentChange = false;
                }
                else
                {
                    _isSuppressingComponentChange = true;
                    if(tileDataComponentComboBox.Items.Count > 0)
                        tileDataComponentComboBox.SelectedIndex = 0;
                    _isSuppressingComponentChange = false;
                }
            }
            else
            {
                selectedTileLabel.Text = "Selected Tile: None";
                tileValueNumeric.Enabled = false;
                tileDatabaseComboBox.Enabled = false;
                tileDataComponentComboBox.Enabled = false;
            }

            tilesetPictureBox.Invalidate();
        }
        private bool _isSuppressingTileValueChange = false;

        private void TilesetPictureBox_Paint(object sender, PaintEventArgs e)
        {
            var map = GetSelectedMap();
            if(map == null || currentTilesetBitmap == null || map.TileSize <= 0 || selectedTileIndex < 0)
                return;

            int tileSize = map.TileSize;
            int cols = currentTilesetBitmap.Width / tileSize;
            int rows = currentTilesetBitmap.Height / tileSize;

            using(var gridPen = new Pen(System.Drawing.Color.FromArgb(100, 255, 255, 255), 1))
            {
                for(int x = 0; x <= currentTilesetBitmap.Width; x += tileSize)
                {
                    e.Graphics.DrawLine(gridPen, x, 0, x, currentTilesetBitmap.Height);
                }
                for(int y = 0; y <= currentTilesetBitmap.Height; y += tileSize)
                {
                    e.Graphics.DrawLine(gridPen, 0, y, currentTilesetBitmap.Width, y);
                }
            }

            if(selectedTileIndex >= 0)
            {
                int col = selectedTileIndex % cols;
                int row = selectedTileIndex / cols;
                var rect = new System.Drawing.Rectangle(col * tileSize, row * tileSize, tileSize, tileSize);

                using(var highlightPen = new Pen(System.Drawing.Color.Yellow, 2))
                {
                    e.Graphics.DrawRectangle(highlightPen, rect);
                }

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

        private class DatabaseOption
        {
            public string DisplayName { get; set; } = "";
            public string FilePath { get; set; } = "";
            public override string ToString() => DisplayName;
        }

        private void TilesetPictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            var map = GetSelectedMap();
            if(map == null || currentTilesetBitmap == null || map.TileSize <= 0)
                return;

            int tileSize = map.TileSize;
            int cols = currentTilesetBitmap.Width / tileSize;
            int rows = currentTilesetBitmap.Height / tileSize;

            int clickedCol = e.X / tileSize;
            int clickedRow = e.Y / tileSize;

            if(clickedCol >= 0 && clickedCol < cols && clickedRow >= 0 && clickedRow < rows)
            {
                selectedTileIndex = clickedRow * cols + clickedCol;
                EditorContextManager.SelectedTileIndex = selectedTileIndex;
                selectedTileLabel.Text = $"Selected Tile Index: {selectedTileIndex}";

                tileValueNumeric.Enabled = true;

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

            map.TileProperties[selectedTileIndex] = val;

            NeedsToBeSaved = true;
            tilesetPictureBox.Invalidate();
        }

        private void InitializePropertiesToolstripEvents()
        {
            AddComponentButton.DropDown = new ContextMenuStrip();

            AddComponentButton.DropDownOpening += (s, e) =>
            {
                AddComponentButton.DropDownItems.Clear();
                GameObject? selectedGo = GetSelectedGameObjectFromHierarchy();

                var componentTypes = new System.Collections.Generic.List<Type>();

                var coreAssembly = typeof(GameComponent).Assembly;
                componentTypes.AddRange(coreAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(GameComponent)) && !t.IsAbstract));

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
                    if(type == typeof(Engine.Core.ECS.Components.TransformComponent))
                        continue;

                    ToolStripMenuItem item = new ToolStripMenuItem(type.Name.Replace("Component", ""));
                    Type targetType = type;

                    if(selectedGo != null && selectedGo.Components.ContainsKey(targetType))
                    {
                        item.Enabled = false;
                        item.Text += " (Already Attached)";
                    }

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

                            RebuildInspectorPanel(selectedGo, forceRebuild: true);
                        }
                    };

                    AddComponentButton.DropDownItems.Add(item);
                }
            };

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
                    RebuildInspectorPanel(selectedGo, forceRebuild: true);
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
            SaveScene();
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
                        GameScene revertedScene = SceneSerializer.LoadScene(targetScenePath);

                        EditorContextManager.ActiveLoadedScene = revertedScene;
                        EditorContextManager.ActiveLoadedScene.resetContextSceneInManagers();

                        AttachSceneEvents(revertedScene);

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

        public void ImportPyxelJsonToMap(string filePath)
        {
            string jsonContent = File.ReadAllText(filePath);
            var pyxelDoc = JsonSerializer.Deserialize<PyxelJsonDocument>(jsonContent);

            if(pyxelDoc == null)
                return;

            // Create your native engine Map instance
            var newMap = new Map(pyxelDoc.tileswide, pyxelDoc.tileshigh)
            {
                MapName = Path.GetFileNameWithoutExtension(filePath),
                TileSize = pyxelDoc.tilewidth,
                IsEnabled = true,
                GridFlattened = new List<int>()
            };

            // Initialize the flat grid array with default zeros (empty tiles)
            int totalCells = pyxelDoc.tileswide * pyxelDoc.tileshigh;
            for(int i = 0; i < totalCells; i++)
            {
                newMap.GridFlattened.Add(0);
            }

            // Pull tile data from the first layer (Layer 0)
            if(pyxelDoc.layers != null && pyxelDoc.layers.Count > 0)
            {
                var layer = pyxelDoc.layers[0];
                if(layer.tiles != null)
                {
                    foreach(var t in layer.tiles)
                    {
                        // Convert 2D coordinates into your 1D flattened index formula
                        int index = (t.y * pyxelDoc.tileswide) + t.x;
                        if(index >= 0 && index < newMap.GridFlattened.Count)
                        {
                            newMap.GridFlattened[index] = t.tile;
                        }
                    }
                }
            }

            EditorContextManager.ActiveLoadedScene.SceneMaps.Add(newMap);
            RefreshMapsTab();
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
            string filterText = e;

            ConsoleTextBox.BeginUpdate();
            ConsoleTextBox.Clear();

            foreach(var log in _masterLogHistory)
            {
                if(string.IsNullOrEmpty(filterText) || log.Message.Contains(filterText, StringComparison.CurrentCultureIgnoreCase))
                {
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
            ConsoleTextBox.SelectionColor = ConsoleTextBox.ForeColor;

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
