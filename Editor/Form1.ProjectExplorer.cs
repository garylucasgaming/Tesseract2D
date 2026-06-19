using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using Engine.Core.Serialization;
using Engine.Core.Utilities;
using System.Runtime.InteropServices;

namespace WinFormsApp1
{
    public partial class Form1
    {
        private ContextMenuStrip _folderContextMenu = new ContextMenuStrip();

        private void InitializeProjectExplorerMenus()
        {
            ToolStripMenuItem newFolderItem = new ToolStripMenuItem("New Folder");
            newFolderItem.Click += NewFolderItem_Click;

            ToolStripMenuItem renameItem = new ToolStripMenuItem("Rename");
            renameItem.Click += RenameItem_Click;

            ToolStripMenuItem deleteItem = new ToolStripMenuItem("Delete");
            deleteItem.Click += DeleteItem_Click;

            _folderContextMenu.Items.Add(newFolderItem);
            _folderContextMenu.Items.Add(new ToolStripSeparator());
            _folderContextMenu.Items.Add(renameItem);
            _folderContextMenu.Items.Add(deleteItem);

            ProjectFolderTreeView.MouseUp += ProjectFolderTreeView_MouseUp;
            ProjectFolderTreeView.LabelEdit = false; // Using prompt fallback for disk operations
        }

        private void InitializeExplorerIcons()
        {
            _systemImageList.ColorDepth = ColorDepth.Depth32Bit;
            _systemImageList.ImageSize = new Size(16, 16);
            ProjectFolderTreeView.ImageList = _systemImageList;
        }

        public void PopulateProjectExplorerTree(TreeView treeView)
        {
            if(treeView == null)
                return;
            treeView.BeginUpdate();
            treeView.Nodes.Clear();
            _systemImageList.Images.Clear();

            if(!EditorContextManager.IsProjectLoaded)
            {
                treeView.EndUpdate();
                return;
            }

            try
            {
                string rootPath = EditorContextManager.CurrentProjectRoot!;
                string projectName = Path.GetFileName(rootPath);

                TreeNode rootNode = new TreeNode(projectName) { Tag = rootPath };
                AssignSystemIconToNode(rootPath, rootNode, isFolder: true);
                treeView.Nodes.Add(rootNode);

                CrawlDirectoryTree(rootPath, rootNode);
                rootNode.Expand();
            }
            catch(Exception ex)
            {
                Log.Error($"[Project Explorer Error] Failed to populate folder view loops: {ex.Message}");
            }
            finally
            {
                treeView.EndUpdate();
            }
        }

        private void CrawlDirectoryTree(string currentDirectory, TreeNode parentNode)
        {
            try
            {
                string[] subDirectories = Directory.GetDirectories(currentDirectory);
                foreach(string subDir in subDirectories)
                {
                    string dirName = Path.GetFileName(subDir);
                    if(dirName.Equals("Library", StringComparison.OrdinalIgnoreCase) ||
                        dirName.Equals("Temp", StringComparison.OrdinalIgnoreCase))
                        continue;

                    TreeNode dirNode = new TreeNode(dirName) { Tag = subDir };
                    AssignSystemIconToNode(subDir, dirNode, isFolder: true, folderName: dirName);
                    parentNode.Nodes.Add(dirNode);
                    CrawlDirectoryTree(subDir, dirNode);
                }

                string[] files = Directory.GetFiles(currentDirectory);
                foreach(string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    TreeNode fileNode = new TreeNode(fileName) { Tag = file };
                    AssignSystemIconToNode(file, fileNode, isFolder: false);
                    parentNode.Nodes.Add(fileNode);
                }
            }
            catch(Exception ex)
            {
                Log.Error($"[Directory Crawler Error] Failed to read structural paths: {ex.Message}");
            }
        }

        private void AssignSystemIconToNode(string path, TreeNode node, bool isFolder, string folderName = "")
        {
            string cacheKey = isFolder
                ? (folderName.Equals("Assets", StringComparison.OrdinalIgnoreCase) ? "ENGINE_ASSETS_FOLDER"
                   : folderName.Equals("Content", StringComparison.OrdinalIgnoreCase) ? "ENGINE_CONTENT_FOLDER" : "INTERNAL_FOLDER_KEY")
                : Path.GetExtension(path).ToLower();

            if(!_systemImageList.Images.ContainsKey(cacheKey))
            {
                Icon nativeIcon = GetExtensionIcon(path, isFolder);
                _systemImageList.Images.Add(cacheKey, nativeIcon);
            }

            node.ImageKey = cacheKey;
            node.SelectedImageKey = cacheKey;
        }

        private Icon GetExtensionIcon(string pathOrExtension, bool isFolder)
        {
            SHFILEINFO shfi = new SHFILEINFO();
            uint flags = SHGFI_ICON | SHGFI_SMALLICON;

            if(!isFolder && pathOrExtension.StartsWith("."))
                flags |= SHGFI_USEFILEATTRIBUTES;

            uint attributes = isFolder ? FILE_ATTRIBUTE_DIRECTORY : 0;
            IntPtr res = SHGetFileInfo(pathOrExtension, attributes, ref shfi, (uint) Marshal.SizeOf(shfi), flags);

            if(res != IntPtr.Zero && shfi.hIcon != IntPtr.Zero)
            {
                Icon icon = (Icon) Icon.FromHandle(shfi.hIcon).Clone();
                DestroyIcon(shfi.hIcon);
                return icon;
            }
            return SystemIcons.WinLogo;
        }

        private void ProjectFolderTreeView_MouseUp(object sender, MouseEventArgs e)
        {
            if(e.Button != MouseButtons.Right)
                return;
            System.Drawing.Point mousePosition = new System.Drawing.Point(e.X, e.Y);
            TreeNode clickedNode = ProjectFolderTreeView.GetNodeAt(mousePosition);

            if(clickedNode != null)
            {
                ProjectFolderTreeView.SelectedNode = clickedNode;
                string targetPath = clickedNode.Tag as string;

                if(!string.IsNullOrEmpty(targetPath) && (Directory.Exists(targetPath) || File.Exists(targetPath)))
                {
                    _folderContextMenu.Show(ProjectFolderTreeView, mousePosition);
                }
            }
        }

        private void NewFolderItem_Click(object sender, EventArgs e)
        {
            TreeNode selectedNode = ProjectFolderTreeView.SelectedNode;
            if(selectedNode == null)
                return;
            string parentDir = selectedNode.Tag as string;
            if(string.IsNullOrEmpty(parentDir) || !Directory.Exists(parentDir))
                return;

            string newFolderName = PromptUserForProjectName();
            if(string.IsNullOrWhiteSpace(newFolderName))
                return;

            string fullNewFolderPath = Path.Combine(parentDir, newFolderName);
            try
            {
                if(!Directory.Exists(fullNewFolderPath))
                {
                    Directory.CreateDirectory(fullNewFolderPath);
                    PopulateProjectExplorerTree(ProjectFolderTreeView);
                }
            }
            catch(Exception ex) { Log.Error($"[IO Error] {ex.Message}"); }
        }

        private void RenameItem_Click(object sender, EventArgs e)
        {
            TreeNode selectedNode = ProjectFolderTreeView.SelectedNode;
            if(selectedNode == null)
                return;
            string currentPath = selectedNode.Tag as string;
            if(string.IsNullOrEmpty(currentPath) || currentPath.Equals(EditorContextManager.CurrentProjectRoot, StringComparison.OrdinalIgnoreCase))
                return;

            string newName = PromptUserForProjectName();
            if(string.IsNullOrWhiteSpace(newName))
                return;

            string parentDirectory = Path.GetDirectoryName(currentPath)!;
            string newPath = Path.Combine(parentDirectory, newName);

            if(File.Exists(currentPath))
            {
                string extension = Path.GetExtension(currentPath);
                if(!newName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    newPath += extension;
                File.Move(currentPath, newPath);
            }
            else if(Directory.Exists(currentPath))
            {
                Directory.Move(currentPath, newPath);
            }

            PopulateProjectExplorerTree(ProjectFolderTreeView);
        }

        private void DeleteItem_Click(object sender, EventArgs e)
        {
            TreeNode selectedNode = ProjectFolderTreeView.SelectedNode;
            if(selectedNode == null)
                return;
            string targetPath = selectedNode.Tag as string;
            if(string.IsNullOrEmpty(targetPath) || targetPath.Equals(EditorContextManager.CurrentProjectRoot, StringComparison.OrdinalIgnoreCase))
                return;

            if(MessageBox.Show($"Permanently delete '{Path.GetFileName(targetPath)}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if(File.Exists(targetPath))
                    File.Delete(targetPath);
                else if(Directory.Exists(targetPath))
                    Directory.Delete(targetPath, true);
                PopulateProjectExplorerTree(ProjectFolderTreeView);
            }
        }

        private void ProjectFolderTreeViewSearchBar_SearchTextChanged(object sender, string filterText)
        {
            if(string.IsNullOrEmpty(filterText))
            {
                ResetTreeNodes(ProjectFolderTreeView.Nodes);
                return;
            }
            ProjectFolderTreeView.BeginUpdate();
            FilterTreeNodes(ProjectFolderTreeView.Nodes, filterText);
            ProjectFolderTreeView.EndUpdate();
        }
    }
}