using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using Engine.Core.ECS;
using Engine.Core.Serialization;
using Engine.Core.Utilities;
using Engine.Editor.WinFormsApp1;

namespace WinFormsApp1
{
    public partial class Form1
    {
        private ContextMenuStrip _hierarchyContextMenu = new ContextMenuStrip();


        private void InitializeSceneHierarchyMenus()
        {
            SceneHierarchyTreeView.LabelEdit = true;
            SceneHierarchyTreeView.AllowDrop = true;

            ToolStripMenuItem createEmptyItem = new ToolStripMenuItem("Create Empty");
            createEmptyItem.Click += (s, e) => CreateEmptyGameObject_Click();

            ToolStripMenuItem renameItem = new ToolStripMenuItem("Rename Entity");
            renameItem.Click += (s, e) => RenameGameObject_Click();

            ToolStripMenuItem destroyItem = new ToolStripMenuItem("Destroy Entity");
            destroyItem.Click += (s, e) => DestroyGameObject_Click();

            _hierarchyContextMenu.Items.Add(createEmptyItem);
            _hierarchyContextMenu.Items.Add(renameItem);
            _hierarchyContextMenu.Items.Add(new ToolStripSeparator());
            _hierarchyContextMenu.Items.Add(destroyItem);

            // Drag & Drop Wireups
            SceneHierarchyTreeView.ItemDrag += SceneHierarchyTreeView_ItemDrag;
            SceneHierarchyTreeView.DragEnter += SceneHierarchyTreeView_DragEnter;
            SceneHierarchyTreeView.DragDrop += SceneHierarchyTreeView_DragDrop;
            SceneHierarchyTreeView.MouseUp += SceneHierarchyTreeView_MouseUp;
            SceneHierarchyTreeView.AfterLabelEdit += SceneHierarchyTreeView_AfterLabelEdit;

            // 👇 NEW: Hook up the selection interceptor for the Inspector Properties View
            SceneHierarchyTreeView.AfterSelect += SceneHierarchyTreeView_AfterSelect;
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

            var rootEntities = activeScene.GameObjects.Where(go => go.Parent == null);
            foreach(GameObject rootGo in rootEntities)
            {
                TreeNode visualNode = new TreeNode(rootGo.Name)
                {
                    Tag = rootGo,
                    ImageKey = "GameObjectIcon",
                    SelectedImageKey = "GameObjectIcon"
                };
                hierarchyTreeView.Nodes.Add(visualNode);
                CrawlSceneHierarchy(rootGo, visualNode);
            }

            RestoreExpandedStates(hierarchyTreeView.Nodes, expandedObjectIds);

            if(selectedObjectId.HasValue)
            {
                TreeNode nodeToSelect = FindNodeByGameObjectId(hierarchyTreeView.Nodes, selectedObjectId.Value);
                if(nodeToSelect != null)
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

        // --- Creation & Modification Core Events ---
        private void CreateEmptyGameObject_Click()
        {
            GameScene activeScene = EditorContextManager.ActiveLoadedScene;
            if(activeScene == null)
                return;

            TreeNode selectedVisualNode = SceneHierarchyTreeView.SelectedNode;
            GameObject newEntity = new GameObject() { Name = "New GameObject", ContextScene = activeScene };

            if(selectedVisualNode != null && selectedVisualNode.Tag is GameObject parentEntity)
            {
                parentEntity.AddChild(newEntity);
                activeScene.RegisterGameObject(newEntity);
            }
            else
            {
                activeScene.RegisterGameObject(newEntity);
            }

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

            if(targetEntity.Parent != null)
                targetEntity.Parent.RemoveChild(targetEntity);
            else
                DestroyHierarchyRecursively(targetEntity, activeScene);

            activeScene.DestroyGameObject(targetEntity);
            PopulateSceneHierarchyTree(SceneHierarchyTreeView, activeScene);
        }

        private void DestroyHierarchyRecursively(GameObject target, GameScene scene)
        {
            for(int i = target.Children.Count - 1; i >= 0; i--)
            {
                GameObject child = target.Children[i];
                DestroyHierarchyRecursively(child, scene);
                scene.DestroyGameObject(child);
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
                draggedGo.Parent.RemoveChild(draggedGo);
                PopulateSceneHierarchyTree(SceneHierarchyTreeView, activeScene);
                return;
            }

            if(targetNode.Tag is not GameObject targetGo)
                return;
            if(draggedNode == targetNode || IsNodeDescendant(draggedNode, targetNode))
                return;

            targetGo.AddChild(draggedGo);
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
            GameScene sandbox = new GameScene() { SceneName = "Default Sandbox" };
            var mainCam = sandbox.CreateGameObject("Main Camera");
            mainCam.ContextScene = sandbox;
            var playerNode = sandbox.CreateGameObject("Player Entity");
            playerNode.ContextScene = sandbox;

            var childWeapon = new GameObject() { Name = "Equipped Weapon Staff", ContextScene = sandbox };
            playerNode.AddChild(childWeapon);
            sandbox.RegisterGameObject(childWeapon);

            var staticFloor = sandbox.CreateGameObject("Static Level Floor");
            staticFloor.ContextScene = sandbox;

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

        private void SceneHierarchySearchBar_SearchTextChanged(object sender, string filterText)
        {
            if(string.IsNullOrEmpty(filterText))
            {
                ResetTreeNodes(SceneHierarchyTreeView.Nodes);
                return;
            }
            SceneHierarchyTreeView.BeginUpdate();
            FilterTreeNodes(SceneHierarchyTreeView.Nodes, filterText);
            SceneHierarchyTreeView.EndUpdate();
        }

        // 👇 NEW: The dynamic selection handler method
        private void SceneHierarchyTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // Clear the inspector sheet immediately on focus shift
            InspectorFlowPanel.Controls.Clear();

            if(e.Node == null || e.Node.Tag is not GameObject targetGo)
                return;

            // Lock layout math updates to prevent scrolling/flicker artifacts during generation
            InspectorFlowPanel.SuspendLayout();

            int targetWidth = InspectorFlowPanel.Width;

            // Card 1: Core metadata profile panel (Editable Name field)
            Panel baseCard = ComponentCardFactory.CreateCard("GameObject Settings", targetGo, targetWidth);
            InspectorFlowPanel.Controls.Add(baseCard);

            // Card 2: Transform manipulation module
            if(targetGo.Transform != null)
            {
                Panel transformCard = ComponentCardFactory.CreateCard("Transform Component", targetGo.Transform, targetWidth);
                InspectorFlowPanel.Controls.Add(transformCard);
            }

            // Card 3+: Generic ECS array processor
            /*
            foreach (var component in targetGo.Components)
            {
                string name = component.GetType().Name;
                Panel componentCard = ComponentCardFactory.CreateCard(name, component, targetWidth);
                InspectorFlowPanel.Controls.Add(componentCard);
            }
            */

            // Release the rendering thread engine smoothly
            InspectorFlowPanel.ResumeLayout();
        }
    }
}