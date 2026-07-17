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

            ToolStripMenuItem addItem = new ToolStripMenuItem("Add");
            addItem.Click += AddItem_Click;

            ToolStripMenuItem createPrefab = new ToolStripMenuItem("New Prefab");

            //c# block
            ToolStripMenuItem cSharpBlock = new ToolStripMenuItem("C#");
            ToolStripMenuItem createNewComponent = new ToolStripMenuItem("New Component");
            ToolStripMenuItem createNewSystem = new ToolStripMenuItem("New System");
            ToolStripMenuItem createNewManager = new ToolStripMenuItem("New Manager");
            ToolStripMenuItem createNewCSharpScript = new ToolStripMenuItem("New C# Script");
            ToolStripMenuItem baseComponent = new ToolStripMenuItem("Component");
            ToolStripMenuItem dataComponent = new ToolStripMenuItem("Data Component");
            cSharpBlock.DropDownItems.Add(createNewComponent);
            cSharpBlock.DropDownItems.Add(createNewSystem);
            cSharpBlock.DropDownItems.Add(createNewManager);
            cSharpBlock.DropDownItems.Add(createNewCSharpScript);
            createNewComponent.DropDownItems.Add(baseComponent);
            createNewComponent.DropDownItems.Add(dataComponent);

            // lua block
            ToolStripMenuItem luaBlock = new ToolStripMenuItem("Lua");
            ToolStripMenuItem createNewLuaComponent = new ToolStripMenuItem("New Lua Component");
            ToolStripMenuItem createNewLuaSystem = new ToolStripMenuItem("New Lua System");
            ToolStripMenuItem createNewLuaManager = new ToolStripMenuItem("New Lua Manager");
            ToolStripMenuItem createNewLuaScript = new ToolStripMenuItem("New Lua Script");
            ToolStripMenuItem baseLuaComponent = new ToolStripMenuItem("Lua Component");
            ToolStripMenuItem dataLuaComponent = new ToolStripMenuItem("Lua Data Component");
            luaBlock.DropDownItems.Add(createNewLuaComponent);
            luaBlock.DropDownItems.Add(createNewLuaSystem);
            luaBlock.DropDownItems.Add(createNewLuaManager);
            luaBlock.DropDownItems.Add(createNewLuaScript);
            createNewLuaComponent.DropDownItems.Add(baseLuaComponent);
            createNewLuaComponent.DropDownItems.Add(dataLuaComponent);

            ToolStripLabel cSharpStripLabel = new ToolStripLabel("C#");
            ToolStripLabel luaStripLabel = new ToolStripLabel("Lua");

            _folderContextMenu.Items.Add(newFolderItem);
            _folderContextMenu.Items.Add(new ToolStripSeparator());
            _folderContextMenu.Items.Add(addItem);
            _folderContextMenu.Items.Add(renameItem);
            _folderContextMenu.Items.Add(deleteItem);
            _folderContextMenu.Items.Add(new ToolStripSeparator());
            _folderContextMenu.Items.Add(createPrefab);
            _folderContextMenu.Items.Add(new ToolStripSeparator());
            _folderContextMenu.Items.Add(cSharpBlock);
            _folderContextMenu.Items.Add(new ToolStripSeparator());
            _folderContextMenu.Items.Add(luaBlock);


            ProjectFolderTreeView.MouseUp += ProjectFolderTreeView_MouseUp;
            ProjectFolderTreeView.LabelEdit = false; // Using prompt fallback for disk operations

            // 💡 NEW: Initialize Drag & Drop Settings and Hooks
            ProjectFolderTreeView.AllowDrop = true;
            ProjectFolderTreeView.DragEnter += ProjectFolderTreeView_DragEnter;
            ProjectFolderTreeView.DragDrop += ProjectFolderTreeView_DragDrop;
            ProjectFolderTreeView.ItemDrag += ProjectFolderTreeView_ItemDrag;
            ProjectFolderTreeView.DragOver += ProjectFolderTreeView_DragOver;
        }

        private void ProjectFolderTreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if(e.Item is TreeNode dragNode && dragNode.Tag is string dragPath)
            {
                // Don't allow dragging the root directory folder node
                if(dragPath.Equals(EditorContextManager.CurrentProjectRoot, StringComparison.OrdinalIgnoreCase))
                    return;

                // Begin the WinForms DragDrop loop passing the TreeNode as the payload
                DoDragDrop(dragNode, DragDropEffects.Move);
            }
        }

        private void ProjectFolderTreeView_DragOver(object sender, DragEventArgs e)
        {
            // Check if the payload is an internal TreeNode structure
            if(e.Data.GetDataPresent(typeof(TreeNode)))
            {
                System.Drawing.Point clientPoint = ProjectFolderTreeView.PointToClient(new System.Drawing.Point(e.X, e.Y));
                TreeNode targetNode = ProjectFolderTreeView.GetNodeAt(clientPoint);
                TreeNode dragNode = (TreeNode) e.Data.GetData(typeof(TreeNode));

                if(dragNode != null)
                {
                    string sourcePath = dragNode.Tag as string;
                    if(!string.IsNullOrEmpty(sourcePath))
                    {
                        if(targetNode != null)
                        {
                            string targetPath = targetNode.Tag as string;
                            if(!string.IsNullOrEmpty(targetPath))
                            {
                                // 1. Guard: Cannot drop an asset onto itself
                                if(dragNode == targetNode)
                                {
                                    e.Effect = DragDropEffects.None;
                                    return;
                                }

                                // 2. Guard: Cannot drop a folder into itself or its own nested directories
                                if(Directory.Exists(sourcePath))
                                {
                                    string sourceDirWithSlash = sourcePath.EndsWith(Path.DirectorySeparatorChar.ToString())
                                        ? sourcePath
                                        : sourcePath + Path.DirectorySeparatorChar;

                                    string targetDirWithSlash = targetPath.EndsWith(Path.DirectorySeparatorChar.ToString())
                                        ? targetPath
                                        : targetPath + Path.DirectorySeparatorChar;

                                    if(targetDirWithSlash.StartsWith(sourceDirWithSlash, StringComparison.OrdinalIgnoreCase))
                                    {
                                        e.Effect = DragDropEffects.None;
                                        return;
                                    }
                                }

                                // Provide active feedback by highlighting the folder target candidate
                                ProjectFolderTreeView.SelectedNode = targetNode;
                                e.Effect = DragDropEffects.Move;
                                return;
                            }
                        }
                        else
                        {
                            // If hovering over empty tree space, fallback to dropping into the Project Root directory
                            if(EditorContextManager.IsProjectLoaded)
                            {
                                e.Effect = DragDropEffects.Move;
                                return;
                            }
                        }
                    }
                }
                e.Effect = DragDropEffects.None;
            }
        }

        private void ProjectFolderTreeView_DragEnter(object sender, DragEventArgs e)
        {
            // External OS file drop -> Copy Action
            if(e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            // Internal TreeView Node drop -> Move Action
            else if(e.Data.GetDataPresent(typeof(TreeNode)))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void ProjectFolderTreeView_DragDrop(object sender, DragEventArgs e)
        {
            // Find target coordinates relative to the TreeView bounds
            System.Drawing.Point clientPoint = ProjectFolderTreeView.PointToClient(new System.Drawing.Point(e.X, e.Y));
            TreeNode targetNode = ProjectFolderTreeView.GetNodeAt(clientPoint);
            string destinationDirectory = null;

            // Determine target path directory structure
            if(targetNode != null)
            {
                string targetPath = targetNode.Tag as string;
                if(!string.IsNullOrEmpty(targetPath))
                {
                    destinationDirectory = Directory.Exists(targetPath)
                        ? targetPath
                        : Path.GetDirectoryName(targetPath);
                }
            }
            else
            {
                if(EditorContextManager.IsProjectLoaded)
                {
                    destinationDirectory = EditorContextManager.CurrentProjectRoot;
                }
            }

            if(string.IsNullOrEmpty(destinationDirectory) || !Directory.Exists(destinationDirectory))
            {
                Log.Warning("[Content Pipeline] Drag & Drop canceled: No valid destination directory found.");
                return;
            }

            // --- 1. HANDLE INTERNAL MOVES (TreeNode Drag) ---
            if(e.Data.GetDataPresent(typeof(TreeNode)))
            {
                TreeNode dragNode = (TreeNode) e.Data.GetData(typeof(TreeNode));
                if(dragNode == null || !(dragNode.Tag is string sourcePath))
                    return;

                string currentDirectory = Path.GetDirectoryName(sourcePath);
                if(destinationDirectory.Equals(currentDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    return; // Already in this directory, do nothing
                }

                string itemName = Path.GetFileName(sourcePath);
                string destPath = Path.Combine(destinationDirectory, itemName);

                try
                {
                    if(File.Exists(sourcePath))
                    {
                        if(File.Exists(destPath))
                        {
                            var result = MessageBox.Show(
                                $"An asset named '{itemName}' already exists in this folder. Overwrite it?",
                                "Asset Conflict",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question
                            );

                            if(result == DialogResult.No)
                                return;

                            File.Delete(destPath);
                        }

                        File.Move(sourcePath, destPath);
                        Log.Info($"[Content Pipeline] Moved asset file: '{itemName}' to '{destinationDirectory}'");
                    }
                    else if(Directory.Exists(sourcePath))
                    {
                        if(Directory.Exists(destPath))
                        {
                            MessageBox.Show($"A folder named '{itemName}' already exists in this location.", "Folder Conflict", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        Directory.Move(sourcePath, destPath);
                        Log.Info($"[Content Pipeline] Moved folder: '{itemName}' to '{destinationDirectory}'");
                    }

                    // Refresh the explorer hierarchy and expand the targeted destination node
                    PopulateProjectExplorerTree(ProjectFolderTreeView, destinationDirectory);
                }
                catch(Exception ex)
                {
                    Log.Error($"[Project Explorer Error] Failed moving asset: {ex.Message}");
                    MessageBox.Show($"Failed to move item: {ex.Message}", "Move Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return; // Break execution early, internal drop succeeded
            }

            // --- 2. HANDLE EXTERNAL IMPORTS (OS File Drop) ---
            if(e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] droppedFiles = (string[]) e.Data.GetData(DataFormats.FileDrop);
                if(droppedFiles == null || droppedFiles.Length == 0)
                    return;

                try
                {
                    bool treeNeedsRefresh = false;

                    foreach(string sourceFilePath in droppedFiles)
                    {
                        if(File.Exists(sourceFilePath))
                        {
                            string fileName = Path.GetFileName(sourceFilePath);
                            string destFilePath = Path.Combine(destinationDirectory, fileName);

                            if(File.Exists(destFilePath))
                            {
                                var result = MessageBox.Show(
                                    $"An asset named '{fileName}' already exists in this folder. Overwrite it?",
                                    "Asset Conflict",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question
                                );

                                if(result == DialogResult.No)
                                    continue;
                            }

                            File.Copy(sourceFilePath, destFilePath, overwrite: true);
                            Log.Info($"[Content Pipeline] Drag-Imported file: {fileName}");
                            treeNeedsRefresh = true;
                        }
                        else if(Directory.Exists(sourceFilePath))
                        {
                            string dirName = Path.GetFileName(sourceFilePath);
                            string destDirPath = Path.Combine(destinationDirectory, dirName);

                            CopyDirectoryRecursively(sourceFilePath, destDirPath);
                            Log.Info($"[Content Pipeline] Drag-Imported directory: {dirName}");
                            treeNeedsRefresh = true;
                        }
                    }

                    if(treeNeedsRefresh)
                    {
                        PopulateProjectExplorerTree(ProjectFolderTreeView, destinationDirectory);
                    }
                }
                catch(Exception ex)
                {
                    Log.Error($"[Drag & Drop Import Error] Failed processing external drag data: {ex.Message}");
                    MessageBox.Show($"Failed to drag-import items: {ex.Message}", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        // Helper method to support dropping whole directory folders from Windows File Explorer
        private void CopyDirectoryRecursively(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach(string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }

            foreach(string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectoryRecursively(subDir, destSubDir);
            }
        }
        private void InitializeExplorerIcons()
        {
            _systemImageList.ColorDepth = ColorDepth.Depth32Bit;
            _systemImageList.ImageSize = new Size(16, 16);
            ProjectFolderTreeView.ImageList = _systemImageList;
        }

        public void PopulateProjectExplorerTree(TreeView treeView, string selectedPath = null)
        {
            if(treeView == null)
                return;

            treeView.BeginUpdate();

            // 💡 STEP 1: Collect the paths of all currently expanded folder nodes
            HashSet<string> expandedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            KeepTrackOfExpandedNodes(treeView.Nodes, expandedPaths);

            // If a specific path was targeted (e.g. where we just added an item), ensure it is expanded
            if(!string.IsNullOrEmpty(selectedPath))
            {
                expandedPaths.Add(selectedPath);
            }

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

                // 💡 STEP 2: Restore the expansion states recursively across the new nodes
                RestoreExpandedNodesState(treeView.Nodes, expandedPaths);

                // 💡 NEW STEP 3: Find and select the targeted node if provided
                if(!string.IsNullOrEmpty(selectedPath))
                {
                    TreeNode targetNode = FindNodeByPath(treeView.Nodes, selectedPath);
                    if(targetNode != null)
                    {
                        treeView.SelectedNode = targetNode;
                        targetNode.EnsureVisible(); // Scrolls the tree view to focus on it if necessary
                    }
                }
                else if(expandedPaths.Count == 0)
                {
                    // Always make sure the root node itself is expanded if nothing else was tracked yet
                    rootNode.Expand();
                }
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

        // Helper method to recursively search for a node matching the disk path
        private TreeNode FindNodeByPath(TreeNodeCollection nodes, string path)
        {
            foreach(TreeNode node in nodes)
            {
                if(node.Tag is string nodePath && nodePath.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    return node;
                }

                if(node.Nodes.Count > 0)
                {
                    TreeNode found = FindNodeByPath(node.Nodes, path);
                    if(found != null)
                        return found;
                }
            }
            return null;
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

        private void KeepTrackOfExpandedNodes(TreeNodeCollection nodes, HashSet<string> expandedPaths)
        {
            foreach(TreeNode node in nodes)
            {
                if(node.IsExpanded && node.Tag is string path)
                {
                    expandedPaths.Add(path);
                }

                if(node.Nodes.Count > 0)
                {
                    KeepTrackOfExpandedNodes(node.Nodes, expandedPaths);
                }
            }
        }

        // 💡 HELPER 2: Matches the new tree layout nodes against our open path registry
        private void RestoreExpandedNodesState(TreeNodeCollection nodes, HashSet<string> expandedPaths)
        {
            foreach(TreeNode node in nodes)
            {
                if(node.Tag is string path && expandedPaths.Contains(path))
                {
                    node.Expand();
                }

                if(node.Nodes.Count > 0)
                {
                    RestoreExpandedNodesState(node.Nodes, expandedPaths);
                }
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

        private void AddItem_Click(object sender, EventArgs e)
        {
            TreeNode selectedNode = ProjectFolderTreeView.SelectedNode;
            if(selectedNode == null)
                return;

            string targetPath = selectedNode.Tag as string;
            if(string.IsNullOrEmpty(targetPath))
                return;

            string destinationDirectory = Directory.Exists(targetPath)
                ? targetPath
                : Path.GetDirectoryName(targetPath);

            if(string.IsNullOrEmpty(destinationDirectory) || !Directory.Exists(destinationDirectory))
                return;

            using(OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Multiselect = true;
                openFileDialog.Title = "Import Assets into Project";
                openFileDialog.Filter = "All Files (*.*)|*.*|" +
                                        "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|" +
                                        "Audio (*.wav;*.mp3;*.ogg)|*.wav;*.mp3;*.ogg|" +
                                        "Data (*.toml;*.yaml;*.json;*.txt)|*.toml;*.yaml;*.json;*.txt";

                if(openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        bool itemsImported = false;

                        foreach(string sourceFilePath in openFileDialog.FileNames)
                        {
                            string fileName = Path.GetFileName(sourceFilePath);
                            string destFilePath = Path.Combine(destinationDirectory, fileName);

                            if(File.Exists(destFilePath))
                            {
                                var result = MessageBox.Show(
                                    $"An asset named '{fileName}' already exists in this folder. Overwrite it?",
                                    "Asset Conflict",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question
                                );

                                if(result == DialogResult.No)
                                    continue;
                            }

                            File.Copy(sourceFilePath, destFilePath, overwrite: true);
                            Log.Info($"[Content Pipeline] Imported asset source: {fileName}");
                            itemsImported = true;
                        }

                        // Refresh tree layout and explicitly pass the target folder to expand/select it
                        if(itemsImported)
                        {
                            PopulateProjectExplorerTree(ProjectFolderTreeView, destinationDirectory);
                        }
                    }
                    catch(Exception ex)
                    {
                        Log.Error($"[Asset Import Error] Failed to copy external asset dependencies: {ex.Message}");
                        MessageBox.Show($"Failed to import some files: {ex.Message}", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
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