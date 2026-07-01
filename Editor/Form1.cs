using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using Engine.Core.ECS;
using Engine.Core.Serialization;
using Engine.Core.Utilities;

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

        public static TreeView ActiveHierarchyTreeView {
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
                // 1. Build the path where your default scene's json file should live
                string targetScenePath = Path.Combine(EditorContextManager.CurrentProjectRoot, "Content", "Scenes", "Default Sandbox.json");

                // 2. CHECK: If the file exists, load it! Otherwise, fall back to generating a clean slate template.
                if(File.Exists(targetScenePath))
                {
                    try
                    {
                        Log.Info($"[Editor UI] Found existing workspace state file. Deserializing active layout tree...");

                        // Read the file structure straight back into memory
                        GameScene loadedScene = SceneSerializer.LoadScene(targetScenePath);

                        // Set the context and populate your UI nodes with the genuine saved data
                        EditorContextManager.ActiveLoadedScene = loadedScene;
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
                        MessageBox.Show("The selected folder is not a valid engine project.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    EditorContextManager.OpenProjectContext(targetFolder);
                    OnProjectLoaded();
                }
            }
        }

        private void onSaveProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(!EditorContextManager.IsProjectLoaded)
            {
                MessageBox.Show("No active project workspace is currently open.", "Save Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                string sceneFileName = $"{EditorContextManager.ActiveLoadedScene.SceneName}.json";
                string targetScenePath = Path.Combine(EditorContextManager.CurrentProjectRoot, "Content", "Scenes", sceneFileName);

                // 2. Ensure directories exist safely on disk
                string directoryCheck = Path.GetDirectoryName(targetScenePath);
                if(!string.IsNullOrEmpty(directoryCheck) && !Directory.Exists(directoryCheck))
                {
                    Directory.CreateDirectory(directoryCheck);
                }

                // 3.  EXECUTE YOUR EXACT NATIVE ENGINE SERIALIZER 
                // We pass the live scene layout and target destination directly
                SceneSerializer.SaveScene(EditorContextManager.ActiveLoadedScene, targetScenePath);

                MessageBox.Show($"Project workspace and active scene layout saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            ConsoleTextBox.AppendText(formattedText + Environment.NewLine);
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
    }
}