using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using Engine.Core.ECS;
using Engine.Core.Serialization;
using Engine.Core.Utilities;
using Engine.Editor.WinFormsApp1;
using Engine.Core.ECS.Components;
using SharpDX.WIC;
using System.Reflection;

namespace WinFormsApp1
{
    public partial class Form1
    {
        private ContextMenuStrip _hierarchyContextMenu = new ContextMenuStrip();

        private void InitializeSceneHierarchyMenus()
        {
            SceneHierarchyTreeView.LabelEdit = true;
            SceneHierarchyTreeView.AllowDrop = true;

            ToolStripMenuItem createEmpty = new ToolStripMenuItem("Create Empty");
            createEmpty.Click += (s, e) => CreateEmptyGameObject_Click();

            ToolStripMenuItem renameItem = new ToolStripMenuItem("Rename Entity");
            renameItem.Click += (s, e) => RenameGameObject_Click();

            ToolStripMenuItem duplicateItem = new ToolStripMenuItem("Duplicate Entity");
            duplicateItem.Click += (s, e) => DuplicateGameObject();

            ToolStripMenuItem destroyItem = new ToolStripMenuItem("Destroy Entity");
            destroyItem.Click += (s, e) => DestroyGameObject_Click();

            ToolStripMenuItem saveAsPrefab = new ToolStripMenuItem("Save as Prefab");

            ToolStripMenuItem addPrefab = new ToolStripMenuItem("Add Prefab Object");

            _hierarchyContextMenu.Items.Add(createEmpty);
            _hierarchyContextMenu.Items.Add(renameItem);
            _hierarchyContextMenu.Items.Add(duplicateItem);
            _hierarchyContextMenu.Items.Add(destroyItem);
            _hierarchyContextMenu.Items.Add(new ToolStripSeparator());
            _hierarchyContextMenu.Items.Add(saveAsPrefab);
            _hierarchyContextMenu.Items.Add(addPrefab);

            // Drag & Drop Wireups
            SceneHierarchyTreeView.ItemDrag += SceneHierarchyTreeView_ItemDrag;
            SceneHierarchyTreeView.DragEnter += SceneHierarchyTreeView_DragEnter;
            SceneHierarchyTreeView.DragDrop += SceneHierarchyTreeView_DragDrop;
            SceneHierarchyTreeView.MouseUp += SceneHierarchyTreeView_MouseUp;
            SceneHierarchyTreeView.AfterLabelEdit += SceneHierarchyTreeView_AfterLabelEdit;
            SceneHierarchyTreeView.AfterSelect += SceneHierarchyTreeView_AfterSelect;
            SceneHierarchyTreeView.AfterLabelEdit += SceneHierarchyTreeView_AfterLabelEdit;
            SceneHierarchyTreeView.AfterSelect += SceneHierarchyTreeView_AfterSelect;
            sceneHierarchySearchBar.SearchTextChanged += (s, searchText) =>
            {
                PerformHierarchySearch(searchText);
            };
        }

        public void PopulateSceneHierarchyTree(TreeView hierarchyTreeView, GameScene activeScene)
        {
            if(hierarchyTreeView == null)
                return;

            HashSet<Guid> expandedObjectIds = new HashSet<Guid>();
            CaptureExpandedStates(hierarchyTreeView.Nodes, expandedObjectIds);
            Guid? selectedObjectId = (hierarchyTreeView.SelectedNode?.Tag is GameObject selectedGo) ? selectedGo.Id : null;

            hierarchyTreeView.BeginUpdate();
            hierarchyTreeView.Nodes.Clear();

            if(activeScene == null)
            {
                hierarchyTreeView.EndUpdate();
                return;
            }

            // Grab all entities currently tracked in the active scene
            var allEntities = activeScene.Entities.GetSerializableEntities().ToList();

            // Pass 1: Instantly generate a visual TreeNode wrapper for every single object
            Dictionary<Guid, TreeNode> nodeMap = new Dictionary<Guid, TreeNode>();
            foreach(GameObject go in allEntities)
            {
                TreeNode visualNode = new TreeNode(go.Name)
                {
                    Tag = go,
                    ImageKey = "GameObjectIcon",
                    SelectedImageKey = "GameObjectIcon"
                };
                nodeMap[go.Id] = visualNode;
            }

            // Pass 2: High-efficiency layout linkage via your Parent graph references
            foreach(GameObject go in allEntities)
            {
                TreeNode currentVisualNode = nodeMap[go.Id];

                if(go.Parent != null && nodeMap.TryGetValue(go.Parent.Id, out TreeNode parentVisualNode))
                {
                    // If it has a valid structural parent, mount it directly to the parent node's collection
                    parentVisualNode.Nodes.Add(currentVisualNode);
                }
                else
                {
                    // Otherwise, it belongs on the root shelf of your scene view
                    hierarchyTreeView.Nodes.Add(currentVisualNode);
                }
            }

            // Restore user UI context selections seamlessly
            RestoreExpandedStates(hierarchyTreeView.Nodes, expandedObjectIds);

            if(selectedObjectId.HasValue && nodeMap.TryGetValue(selectedObjectId.Value, out TreeNode nodeToSelect))
            {
                hierarchyTreeView.SelectedNode = nodeToSelect;
            }

            hierarchyTreeView.EndUpdate();
        }
        private void CrawlSceneHierarchy(GameObject currentGo, TreeNode parentVisualNode)
        {
            foreach(GameObject childGo in currentGo.Children)
            {
                TreeNode childVisualNode = new TreeNode(childGo.Name)
                {
                    Tag = childGo,
                    ImageKey = "GameObjectIcon",
                    SelectedImageKey = "GameObjectIcon"
                };
                parentVisualNode.Nodes.Add(childVisualNode);
                CrawlSceneHierarchy(childGo, childVisualNode);
            }
        }

        private void DuplicateGameObject()
        {
            GameScene activeScene = EditorContextManager.ActiveLoadedScene;
            if(activeScene == null)
                return;

            // 1. Target the ACTUAL engine object reference from the selected node's Tag
            TreeNode selectedNode = SceneHierarchyTreeView.SelectedNode;
            if(selectedNode == null || selectedNode.Tag is not GameObject originalEntity)
                return;

            // 2. Clone the GameObject using a memory serialization pass (Deep Copy)
            GameObject duplicatedEntity = DeepCloneGameObject(originalEntity);
            if(duplicatedEntity == null)
                return;

            // 3. Assign structural identity differentiations 
            duplicatedEntity.Id = Guid.NewGuid();
            duplicatedEntity.Name = $"{originalEntity.Name} (Copy)";

            // 4. Handle hierarchy routing safely
            if(originalEntity.Parent != null)
            {
                duplicatedEntity.SetParent(originalEntity.Parent);
                activeScene.AddGameObject(duplicatedEntity);
            }
            else
            {
                // If it was root-level, register it straight to the active scene array context
                activeScene.AddGameObject(duplicatedEntity);
            }

            // 5. Completely rebuild the layout tree
            PopulateSceneHierarchyTree(SceneHierarchyTreeView, activeScene);

            // 6. Automatically find and highlight the newly created copy node in the UI
            TreeNode matchNode = FindNodeByGameObjectId(SceneHierarchyTreeView.Nodes, duplicatedEntity.Id);
            if(matchNode != null)
            {
                SceneHierarchyTreeView.SelectedNode = matchNode;
            }
        }

        /// <summary>
        /// Helper to handle structural deep copying via your engine serialization namespace logic
        /// </summary>
        private GameObject DeepCloneGameObject(GameObject source)
        {
            try
            {
                // 1. Create the base entity copy
                GameObject clone = new GameObject() { Name = source.Name };

                // 2. Loop through every component on the original object
                foreach(var originalComp in source.Components.Values)
                {
                    Type compType = originalComp.GetType();

                    // Instantiate a fresh instance of the specific component type
                    var clonedComp = Activator.CreateInstance(compType);

                    // 👉 FIX 1: Copy all public PROPERTIES
                    foreach(var prop in compType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    {
                        if(!prop.CanWrite || !prop.CanRead)
                            continue;

                        // Skip structural engine references so we don't link back to the old object
                        if(prop.Name == "Id" || prop.Name == "GameObject" || prop.Name == "Parent")
                            continue;

                        prop.SetValue(clonedComp, prop.GetValue(originalComp));
                    }

                    // 👉 FIX 2: Copy all public FIELDS (Vectors, ints, strings, etc.)
                    foreach(var field in compType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    {
                        if(field.Name == "Id" || field.Name == "GameObject" || field.Name == "Parent")
                            continue;

                        field.SetValue(clonedComp, field.GetValue(originalComp));
                    }

                    // 👉 FIX 3: Register this newly populated component to your cloned GameObject!
                    // Adjust this method call to match your actual engine API (e.g., clone.AddComponent() or directly adding to a dictionary)
                    if(clonedComp is GameComponent gameComp)
                    {
                        // If your system uses a dict directly:
                        clone.Components[compType] = gameComp;

                        // Optional: If your component has a back-reference property to its owner, wire it up
                        var goProp = compType.GetProperty("GameObject") ?? compType.GetProperty("Parent");
                        goProp?.SetValue(clonedComp, clone);
                    }
                }

                return clone;
            }
            catch(Exception ex)
            {
                Log.Error($"[Duplicate Failed] Reflection copy crashed: {ex.Message}");
                return null;
            }
        }
        // --- Creation & Modification Core Events ---
        private void CreateEmptyGameObject_Click()
        {
            GameScene activeScene = EditorContextManager.ActiveLoadedScene;
            if(activeScene == null)
                return;

            TreeNode selectedVisualNode = SceneHierarchyTreeView.SelectedNode;
            GameObject newEntity; // Declared cleanly without a dummy allocation

            // Safely check if we should wire up a parent hierarchy link
            if(selectedVisualNode != null && selectedVisualNode.Tag is GameObject parentEntity)
            {
                // Spawns using your custom automatic parent context linking method
                newEntity = activeScene.Spawn("New GameObject", parentEntity);
            }
            else
            {
                // Spawns a clean root-level object directly registered to the scene manager!
                newEntity = activeScene.Spawn("New GameObject");
            }

            // Completely rebuild the tree with the newly registered entity safely included
            PopulateSceneHierarchyTree(SceneHierarchyTreeView, activeScene);

            TreeNode brandNewNode = FindNodeByGameObjectId(SceneHierarchyTreeView.Nodes, newEntity.Id);
            if(brandNewNode != null)
            {
                SceneHierarchyTreeView.SelectedNode = brandNewNode;
                if(selectedVisualNode != null)
                    selectedVisualNode.Expand();

                brandNewNode.BeginEdit();
            }
        }

        private void SceneHierarchyTreeView_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if(e.Label == null)
                return;
            if(string.IsNullOrWhiteSpace(e.Label))
            {
                MessageBox.Show("GameObject names cannot be blank.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.CancelEdit = true;
                return;
            }

            if(e.Node.Tag is GameObject targetEntity)
            {
                targetEntity.Name = e.Label.Trim();
                Log.Info($"[Hierarchy] Inline rename committed for GUID '{targetEntity.Id}': {targetEntity.Name}");
            }
        }

        private void RenameGameObject_Click()
        {
            SceneHierarchyTreeView.SelectedNode?.BeginEdit();
        }

        private void DestroyGameObject_Click()
        {
            TreeNode selectedVisualNode = SceneHierarchyTreeView.SelectedNode;
            if(selectedVisualNode == null || selectedVisualNode.Tag is not GameObject targetEntity)
                return;

            GameScene activeScene = EditorContextManager.ActiveLoadedScene;
            if(activeScene == null)
                return;

            // Clean up live hierarchy links first
            if(targetEntity.Parent != null)
                targetEntity.SetParent(null);

            // Cascade destruction across all underlying children recursive nodes
            DestroyHierarchyRecursively(targetEntity, activeScene);

            // FIX: Use the native operational manager to safely de-allocate instance arrays
            activeScene.Entities.RemoveEntity(targetEntity);

            PopulateSceneHierarchyTree(SceneHierarchyTreeView, activeScene);
        }

        private void DestroyHierarchyRecursively(GameObject target, GameScene scene)
        {

            for(int i = target.Children.Count - 1; i >= 0; i--)
            {
                GameObject child = target.Children[i] as GameObject;
                DestroyHierarchyRecursively(child, scene);

                // FIX: Map database cleanup to EntityManager directly
                scene.Entities.RemoveEntity(child);
            }
        }

        // --- Drag and Drop Interceptors ---
        private void SceneHierarchyTreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if(e.Item != null)
                DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void SceneHierarchyTreeView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(typeof(TreeNode)) ? DragDropEffects.Move : DragDropEffects.None;
        }

        private void SceneHierarchyTreeView_DragDrop(object sender, DragEventArgs e)
        {
            if(!e.Data.GetDataPresent(typeof(TreeNode)))
                return;
            TreeNode draggedNode = (TreeNode) e.Data.GetData(typeof(TreeNode));

            System.Drawing.Point pt = SceneHierarchyTreeView.PointToClient(new System.Drawing.Point(e.X, e.Y));
            TreeNode targetNode = SceneHierarchyTreeView.GetNodeAt(pt);

            if(draggedNode.Tag is not GameObject draggedGo)
                return;
            GameScene activeScene = EditorContextManager.ActiveLoadedScene;
            if(activeScene == null)
                return;

            if(targetNode == null) // Dragged to empty space -> Make Root
            {
                if(draggedGo.Parent == null)
                    return;

                // FIX: Detach hierarchy safely using centralized graph modifier 
                draggedGo.SetParent(null);
                PopulateSceneHierarchyTree(SceneHierarchyTreeView, activeScene);
                return;
            }

            if(targetNode.Tag is not GameObject targetGo)
                return;
            if(draggedNode == targetNode || IsNodeDescendant(draggedNode, targetNode))
                return;

            // FIX: Remap via symmetric graph method
            draggedGo.SetParent(targetGo);
            PopulateSceneHierarchyTree(SceneHierarchyTreeView, activeScene);

            TreeNode updatedTargetNode = FindNodeByGameObjectId(SceneHierarchyTreeView.Nodes, targetGo.Id);
            updatedTargetNode?.Expand();
        }

        private void SceneHierarchyTreeView_MouseUp(object sender, MouseEventArgs e)
        {
            if(e.Button != MouseButtons.Right)
                return;
            System.Drawing.Point mousePosition = new System.Drawing.Point(e.X, e.Y);
            SceneHierarchyTreeView.SelectedNode = SceneHierarchyTreeView.GetNodeAt(mousePosition);
            _hierarchyContextMenu.Show(SceneHierarchyTreeView, mousePosition);
        }

        private void LoadDefaultSandboxScene()
        {
            // FIX: Rebuilt initialization steps to use the exact structural factory API layout
            GameScene sandbox = new GameScene() { SceneName = "Default Sandbox" };
            sandbox.InitializeManagers();

            sandbox.Spawn("Main Camera");
            var playerNode = sandbox.Spawn("Player Entity");

            var childWeapon = sandbox.Spawn("Equipped Weapon Staff");
            childWeapon.SetParent(playerNode);

            sandbox.Spawn("Static Level Floor");

            EditorContextManager.ActiveLoadedScene = sandbox;
            PopulateSceneHierarchyTree(SceneHierarchyTreeView, sandbox);
        }

        // --- ID-State Resolution Helpers ---
        private void CaptureExpandedStates(TreeNodeCollection nodes, HashSet<Guid> expandedIds)
        {
            foreach(TreeNode node in nodes)
            {
                if(node.IsExpanded && node.Tag is GameObject go)
                    expandedIds.Add(go.Id);
                CaptureExpandedStates(node.Nodes, expandedIds);
            }
        }

        private void RestoreExpandedStates(TreeNodeCollection nodes, HashSet<Guid> expandedIds)
        {
            foreach(TreeNode node in nodes)
            {
                if(node.Tag is GameObject go && expandedIds.Contains(go.Id))
                    node.Expand();
                RestoreExpandedStates(node.Nodes, expandedIds);
            }
        }

        private TreeNode FindNodeByGameObjectId(TreeNodeCollection nodes, Guid targetId)
        {
            foreach(TreeNode node in nodes)
            {
                if(node.Tag is GameObject go && go.Id == targetId)
                    return node;
                TreeNode childResult = FindNodeByGameObjectId(node.Nodes, targetId);
                if(childResult != null)
                    return childResult;
            }
            return null;
        }

        private bool IsNodeDescendant(TreeNode parent, TreeNode child)
        {
            if(child.Parent == null)
                return false;
            if(child.Parent == parent)
                return true;
            return IsNodeDescendant(parent, child.Parent);
        }

        private void PerformHierarchySearch(string searchText)
        {
            SceneHierarchyTreeView.BeginUpdate();

            // 1. Clear out previous styling artifacts
            ClearNodeHighlights(SceneHierarchyTreeView.Nodes);

            // 2. If the user cleared the bar, we are done
            if(string.IsNullOrWhiteSpace(searchText))
            {
                SceneHierarchyTreeView.EndUpdate();
                return;
            }

            string cleanSearch = searchText.Trim().ToLower();

            // 3. Scan and highlight all matched partial instances recursively
            HighlightMatchingNodes(SceneHierarchyTreeView.Nodes, cleanSearch);

            SceneHierarchyTreeView.EndUpdate();
        }

        private void ClearNodeHighlights(TreeNodeCollection nodes)
        {
            foreach(TreeNode node in nodes)
            {
                node.BackColor = System.Drawing.Color.Empty; // Resets cleanly to systemic layout colors
                node.ForeColor = System.Drawing.Color.Empty;
                ClearNodeHighlights(node.Nodes);
            }
        }

        private bool HighlightMatchingNodes(TreeNodeCollection nodes, string searchText)
        {
            bool anyChildMatched = false;

            foreach(TreeNode node in nodes)
            {
                // Case-insensitive evaluation of the partial text segment
                bool isMatch = node.Text.ToLower().Contains(searchText);

                // Recursively walk downstream child branches first
                bool childMatched = HighlightMatchingNodes(node.Nodes, searchText);

                if(isMatch)
                {
                    node.BackColor = System.Drawing.Color.Yellow;
                    node.ForeColor = System.Drawing.Color.Black;
                }

                // Auto-expand parents if this node or any downstream entity matches
                if(isMatch || childMatched)
                {
                    node.Expand();
                    anyChildMatched = true;
                }
            }

            return anyChildMatched;
        }

        private void SceneHierarchyTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            InspectorFlowPanel.Controls.Clear();
            if(e.Node == null || e.Node.Tag is not GameObject targetGo)
                return;

            InspectorFlowPanel.SuspendLayout();
            int targetWidth = InspectorFlowPanel.Width;

            // Card 1: Core GameObject Properties (Id, Name, IsActive)
            Panel baseCard = ComponentCardFactory.CreateCard(targetGo.Name, targetGo, targetWidth);
            InspectorFlowPanel.Controls.Add(baseCard);

            // Card 2+: Extract the direct, live memory instances stored inside the entity
            // FIX: Add '.Values' if Components is a Dictionary, or ensure it pulls the raw GameComponent references
            foreach(var component in targetGo.Components.Values)
            {
                // 1. Get the clean runtime name of the active instance (e.g., "TransformComponent")
                string componentName = component.GetType().Name;

                // 2. Pass the direct live reference straight into your wrapped factory card panel
                Panel componentCard = ComponentCardFactory.CreateCard(componentName, component, targetWidth);
                InspectorFlowPanel.Controls.Add(componentCard);
            }

            InspectorFlowPanel.ResumeLayout();
        }
    }
}