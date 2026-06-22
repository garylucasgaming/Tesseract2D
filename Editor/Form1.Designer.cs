namespace WinFormsApp1
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            menuStrip1 = new MenuStrip();
            menuToolStripMenuItem = new ToolStripMenuItem();
            newProjectToolStripMenuItem = new ToolStripMenuItem();
            loadProjectToolStripMenuItem = new ToolStripMenuItem();
            saveProjectToolStripMenuItem = new ToolStripMenuItem();
            saveProjectAsToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            fileToolStripMenuItem = new ToolStripMenuItem();
            editToolStripMenuItem = new ToolStripMenuItem();
            projectSettingsToolStripMenuItem = new ToolStripMenuItem();
            prjoectPreferencesToolStripMenuItem = new ToolStripMenuItem();
            editShortcutsToolStripMenuItem = new ToolStripMenuItem();
            undoToolStripMenuItem = new ToolStripMenuItem();
            redoToolStripMenuItem = new ToolStripMenuItem();
            cutToolStripMenuItem = new ToolStripMenuItem();
            copyToolStripMenuItem = new ToolStripMenuItem();
            pasteToolStripMenuItem = new ToolStripMenuItem();
            duplicateToolStripMenuItem = new ToolStripMenuItem();
            deleteToolStripMenuItem = new ToolStripMenuItem();
            codeTemplatesToolStripMenuItem = new ToolStripMenuItem();
            assetsToolStripMenuItem = new ToolStripMenuItem();
            addNewAssetToolStripMenuItem = new ToolStripMenuItem();
            gameObjectToolStripMenuItem = new ToolStripMenuItem();
            componentToolStripMenuItem = new ToolStripMenuItem();
            gameSystemToolStripMenuItem = new ToolStripMenuItem();
            gameManagerToolStripMenuItem = new ToolStripMenuItem();
            gameEventToolStripMenuItem = new ToolStripMenuItem();
            databaseToolStripMenuItem = new ToolStripMenuItem();
            resourceToolStripMenuItem = new ToolStripMenuItem();
            importAssetsToolStripMenuItem = new ToolStripMenuItem();
            openMGCBToolStripMenuItem = new ToolStripMenuItem();
            reimportAllAssetsToolStripMenuItem = new ToolStripMenuItem();
            windowToolStripMenuItem = new ToolStripMenuItem();
            spriteEditorToolStripMenuItem = new ToolStripMenuItem();
            sceneHierarchyToolStripMenuItem = new ToolStripMenuItem();
            propertiesToolStripMenuItem = new ToolStripMenuItem();
            consoleToolStripMenuItem = new ToolStripMenuItem();
            toolsToolStripMenuItem = new ToolStripMenuItem();
            spriteEditorToolStripMenuItem1 = new ToolStripMenuItem();
            tileMapEditorToolStripMenuItem = new ToolStripMenuItem();
            animatorToolStripMenuItem = new ToolStripMenuItem();
            uICanvasToolStripMenuItem = new ToolStripMenuItem();
            audioMixerToolStripMenuItem = new ToolStripMenuItem();
            projectFolderToolStripMenuItem = new ToolStripMenuItem();
            gameToolStripMenuItem = new ToolStripMenuItem();
            runToolStripMenuItem = new ToolStripMenuItem();
            stopToolStripMenuItem = new ToolStripMenuItem();
            pauseToolStripMenuItem = new ToolStripMenuItem();
            stepForwardToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            documentationToolStripMenuItem = new ToolStripMenuItem();
            SceneHierarchy = new GroupBox();
            SceneHierarchyTreeView = new TreeView();
            PropertiesWindow = new GroupBox();
            tabControl1 = new TabControl();
            SceneView = new TabPage();
            mgWindowControl = new Editor.MGWindowControl();
            tabControl2 = new TabControl();
            ProjectFolderTabPage = new TabPage();
            ProjectFolderTreeView = new TreeView();
            ConsoleTabPage = new TabPage();
            ConsoleTextBox = new TextBox();
            InspectorFlowPanel = new FlowLayoutPanel();
            menuStrip1.SuspendLayout();
            SceneHierarchy.SuspendLayout();
            PropertiesWindow.SuspendLayout();
            tabControl1.SuspendLayout();
            SceneView.SuspendLayout();
            tabControl2.SuspendLayout();
            ProjectFolderTabPage.SuspendLayout();
            ConsoleTabPage.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { menuToolStripMenuItem, editToolStripMenuItem, assetsToolStripMenuItem, windowToolStripMenuItem, gameToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1264, 24);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // menuToolStripMenuItem
            // 
            menuToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newProjectToolStripMenuItem, loadProjectToolStripMenuItem, saveProjectToolStripMenuItem, saveProjectAsToolStripMenuItem, exitToolStripMenuItem, fileToolStripMenuItem });
            menuToolStripMenuItem.Name = "menuToolStripMenuItem";
            menuToolStripMenuItem.Size = new Size(37, 20);
            menuToolStripMenuItem.Text = "File";
            // 
            // newProjectToolStripMenuItem
            // 
            newProjectToolStripMenuItem.Name = "newProjectToolStripMenuItem";
            newProjectToolStripMenuItem.Size = new Size(154, 22);
            newProjectToolStripMenuItem.Text = "New Project";
            newProjectToolStripMenuItem.Click += onCreateProjectToolStripMenuItem_Click;
            // 
            // loadProjectToolStripMenuItem
            // 
            loadProjectToolStripMenuItem.Name = "loadProjectToolStripMenuItem";
            loadProjectToolStripMenuItem.Size = new Size(154, 22);
            loadProjectToolStripMenuItem.Text = "Load Project";
            loadProjectToolStripMenuItem.Click += onLoadProjectToolStripMenuItem_Click;
            // 
            // saveProjectToolStripMenuItem
            // 
            saveProjectToolStripMenuItem.Name = "saveProjectToolStripMenuItem";
            saveProjectToolStripMenuItem.Size = new Size(154, 22);
            saveProjectToolStripMenuItem.Text = "Save Project";
            saveProjectToolStripMenuItem.Click += onSaveProjectToolStripMenuItem_Click;
            // 
            // saveProjectAsToolStripMenuItem
            // 
            saveProjectAsToolStripMenuItem.Name = "saveProjectAsToolStripMenuItem";
            saveProjectAsToolStripMenuItem.Size = new Size(154, 22);
            saveProjectAsToolStripMenuItem.Text = "Save Project As";
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(154, 22);
            exitToolStripMenuItem.Text = "Exit";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(154, 22);
            fileToolStripMenuItem.Text = "File";
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { projectSettingsToolStripMenuItem, prjoectPreferencesToolStripMenuItem, editShortcutsToolStripMenuItem, undoToolStripMenuItem, redoToolStripMenuItem, cutToolStripMenuItem, copyToolStripMenuItem, pasteToolStripMenuItem, duplicateToolStripMenuItem, deleteToolStripMenuItem, codeTemplatesToolStripMenuItem });
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(39, 20);
            editToolStripMenuItem.Text = "Edit";
            // 
            // projectSettingsToolStripMenuItem
            // 
            projectSettingsToolStripMenuItem.Name = "projectSettingsToolStripMenuItem";
            projectSettingsToolStripMenuItem.Size = new Size(175, 22);
            projectSettingsToolStripMenuItem.Text = "Project Settings";
            // 
            // prjoectPreferencesToolStripMenuItem
            // 
            prjoectPreferencesToolStripMenuItem.Name = "prjoectPreferencesToolStripMenuItem";
            prjoectPreferencesToolStripMenuItem.Size = new Size(175, 22);
            prjoectPreferencesToolStripMenuItem.Text = "Project Preferences";
            // 
            // editShortcutsToolStripMenuItem
            // 
            editShortcutsToolStripMenuItem.Name = "editShortcutsToolStripMenuItem";
            editShortcutsToolStripMenuItem.Size = new Size(175, 22);
            editShortcutsToolStripMenuItem.Text = "Edit Shortcuts";
            // 
            // undoToolStripMenuItem
            // 
            undoToolStripMenuItem.Name = "undoToolStripMenuItem";
            undoToolStripMenuItem.Size = new Size(175, 22);
            undoToolStripMenuItem.Text = "Undo";
            // 
            // redoToolStripMenuItem
            // 
            redoToolStripMenuItem.Name = "redoToolStripMenuItem";
            redoToolStripMenuItem.Size = new Size(175, 22);
            redoToolStripMenuItem.Text = "Redo";
            // 
            // cutToolStripMenuItem
            // 
            cutToolStripMenuItem.Name = "cutToolStripMenuItem";
            cutToolStripMenuItem.Size = new Size(175, 22);
            cutToolStripMenuItem.Text = "Cut";
            // 
            // copyToolStripMenuItem
            // 
            copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            copyToolStripMenuItem.Size = new Size(175, 22);
            copyToolStripMenuItem.Text = "Copy";
            // 
            // pasteToolStripMenuItem
            // 
            pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
            pasteToolStripMenuItem.Size = new Size(175, 22);
            pasteToolStripMenuItem.Text = "Paste";
            // 
            // duplicateToolStripMenuItem
            // 
            duplicateToolStripMenuItem.Name = "duplicateToolStripMenuItem";
            duplicateToolStripMenuItem.Size = new Size(175, 22);
            duplicateToolStripMenuItem.Text = "Duplicate";
            // 
            // deleteToolStripMenuItem
            // 
            deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            deleteToolStripMenuItem.Size = new Size(175, 22);
            deleteToolStripMenuItem.Text = "Delete";
            // 
            // codeTemplatesToolStripMenuItem
            // 
            codeTemplatesToolStripMenuItem.Name = "codeTemplatesToolStripMenuItem";
            codeTemplatesToolStripMenuItem.Size = new Size(175, 22);
            codeTemplatesToolStripMenuItem.Text = "Code Templates";
            // 
            // assetsToolStripMenuItem
            // 
            assetsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { addNewAssetToolStripMenuItem, importAssetsToolStripMenuItem, openMGCBToolStripMenuItem, reimportAllAssetsToolStripMenuItem });
            assetsToolStripMenuItem.Name = "assetsToolStripMenuItem";
            assetsToolStripMenuItem.Size = new Size(52, 20);
            assetsToolStripMenuItem.Text = "Assets";
            // 
            // addNewAssetToolStripMenuItem
            // 
            addNewAssetToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { gameObjectToolStripMenuItem, componentToolStripMenuItem, gameSystemToolStripMenuItem, gameManagerToolStripMenuItem, gameEventToolStripMenuItem, databaseToolStripMenuItem, resourceToolStripMenuItem });
            addNewAssetToolStripMenuItem.Name = "addNewAssetToolStripMenuItem";
            addNewAssetToolStripMenuItem.Size = new Size(176, 22);
            addNewAssetToolStripMenuItem.Text = "Add New Asset";
            // 
            // gameObjectToolStripMenuItem
            // 
            gameObjectToolStripMenuItem.Name = "gameObjectToolStripMenuItem";
            gameObjectToolStripMenuItem.Size = new Size(169, 22);
            gameObjectToolStripMenuItem.Text = "GameObject";
            // 
            // componentToolStripMenuItem
            // 
            componentToolStripMenuItem.Name = "componentToolStripMenuItem";
            componentToolStripMenuItem.Size = new Size(169, 22);
            componentToolStripMenuItem.Text = "GameComponent";
            // 
            // gameSystemToolStripMenuItem
            // 
            gameSystemToolStripMenuItem.Name = "gameSystemToolStripMenuItem";
            gameSystemToolStripMenuItem.Size = new Size(169, 22);
            gameSystemToolStripMenuItem.Text = "GameSystem";
            // 
            // gameManagerToolStripMenuItem
            // 
            gameManagerToolStripMenuItem.Name = "gameManagerToolStripMenuItem";
            gameManagerToolStripMenuItem.Size = new Size(169, 22);
            gameManagerToolStripMenuItem.Text = "GameManager";
            // 
            // gameEventToolStripMenuItem
            // 
            gameEventToolStripMenuItem.Name = "gameEventToolStripMenuItem";
            gameEventToolStripMenuItem.Size = new Size(169, 22);
            gameEventToolStripMenuItem.Text = "GameEvent";
            // 
            // databaseToolStripMenuItem
            // 
            databaseToolStripMenuItem.Name = "databaseToolStripMenuItem";
            databaseToolStripMenuItem.Size = new Size(169, 22);
            databaseToolStripMenuItem.Text = "Database";
            // 
            // resourceToolStripMenuItem
            // 
            resourceToolStripMenuItem.Name = "resourceToolStripMenuItem";
            resourceToolStripMenuItem.Size = new Size(169, 22);
            resourceToolStripMenuItem.Text = "Resource";
            // 
            // importAssetsToolStripMenuItem
            // 
            importAssetsToolStripMenuItem.Name = "importAssetsToolStripMenuItem";
            importAssetsToolStripMenuItem.Size = new Size(176, 22);
            importAssetsToolStripMenuItem.Text = "Import Assets";
            // 
            // openMGCBToolStripMenuItem
            // 
            openMGCBToolStripMenuItem.Name = "openMGCBToolStripMenuItem";
            openMGCBToolStripMenuItem.Size = new Size(176, 22);
            openMGCBToolStripMenuItem.Text = "Open MGCB";
            // 
            // reimportAllAssetsToolStripMenuItem
            // 
            reimportAllAssetsToolStripMenuItem.Name = "reimportAllAssetsToolStripMenuItem";
            reimportAllAssetsToolStripMenuItem.Size = new Size(176, 22);
            reimportAllAssetsToolStripMenuItem.Text = "Reimport All Assets";
            // 
            // windowToolStripMenuItem
            // 
            windowToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { spriteEditorToolStripMenuItem, sceneHierarchyToolStripMenuItem, propertiesToolStripMenuItem, consoleToolStripMenuItem, toolsToolStripMenuItem, projectFolderToolStripMenuItem });
            windowToolStripMenuItem.Name = "windowToolStripMenuItem";
            windowToolStripMenuItem.Size = new Size(63, 20);
            windowToolStripMenuItem.Text = "Window";
            // 
            // spriteEditorToolStripMenuItem
            // 
            spriteEditorToolStripMenuItem.Name = "spriteEditorToolStripMenuItem";
            spriteEditorToolStripMenuItem.Size = new Size(159, 22);
            spriteEditorToolStripMenuItem.Text = "Scene View";
            // 
            // sceneHierarchyToolStripMenuItem
            // 
            sceneHierarchyToolStripMenuItem.Name = "sceneHierarchyToolStripMenuItem";
            sceneHierarchyToolStripMenuItem.Size = new Size(159, 22);
            sceneHierarchyToolStripMenuItem.Text = "Scene Hierarchy";
            // 
            // propertiesToolStripMenuItem
            // 
            propertiesToolStripMenuItem.Name = "propertiesToolStripMenuItem";
            propertiesToolStripMenuItem.Size = new Size(159, 22);
            propertiesToolStripMenuItem.Text = "Properties";
            // 
            // consoleToolStripMenuItem
            // 
            consoleToolStripMenuItem.Name = "consoleToolStripMenuItem";
            consoleToolStripMenuItem.Size = new Size(159, 22);
            consoleToolStripMenuItem.Text = "Console";
            // 
            // toolsToolStripMenuItem
            // 
            toolsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { spriteEditorToolStripMenuItem1, tileMapEditorToolStripMenuItem, animatorToolStripMenuItem, uICanvasToolStripMenuItem, audioMixerToolStripMenuItem });
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new Size(159, 22);
            toolsToolStripMenuItem.Text = "Tools";
            // 
            // spriteEditorToolStripMenuItem1
            // 
            spriteEditorToolStripMenuItem1.Name = "spriteEditorToolStripMenuItem1";
            spriteEditorToolStripMenuItem1.Size = new Size(150, 22);
            spriteEditorToolStripMenuItem1.Text = "Sprite Editor";
            // 
            // tileMapEditorToolStripMenuItem
            // 
            tileMapEditorToolStripMenuItem.Name = "tileMapEditorToolStripMenuItem";
            tileMapEditorToolStripMenuItem.Size = new Size(150, 22);
            tileMapEditorToolStripMenuItem.Text = "TileMap Editor";
            // 
            // animatorToolStripMenuItem
            // 
            animatorToolStripMenuItem.Name = "animatorToolStripMenuItem";
            animatorToolStripMenuItem.Size = new Size(150, 22);
            animatorToolStripMenuItem.Text = "Animator";
            // 
            // uICanvasToolStripMenuItem
            // 
            uICanvasToolStripMenuItem.Name = "uICanvasToolStripMenuItem";
            uICanvasToolStripMenuItem.Size = new Size(150, 22);
            uICanvasToolStripMenuItem.Text = "UI Canvas";
            // 
            // audioMixerToolStripMenuItem
            // 
            audioMixerToolStripMenuItem.Name = "audioMixerToolStripMenuItem";
            audioMixerToolStripMenuItem.Size = new Size(150, 22);
            audioMixerToolStripMenuItem.Text = "Audio Mixer";
            // 
            // projectFolderToolStripMenuItem
            // 
            projectFolderToolStripMenuItem.Name = "projectFolderToolStripMenuItem";
            projectFolderToolStripMenuItem.Size = new Size(159, 22);
            projectFolderToolStripMenuItem.Text = "Project Folder";
            // 
            // gameToolStripMenuItem
            // 
            gameToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { runToolStripMenuItem, stopToolStripMenuItem, pauseToolStripMenuItem, stepForwardToolStripMenuItem });
            gameToolStripMenuItem.Name = "gameToolStripMenuItem";
            gameToolStripMenuItem.Size = new Size(50, 20);
            gameToolStripMenuItem.Text = "Game";
            // 
            // runToolStripMenuItem
            // 
            runToolStripMenuItem.Name = "runToolStripMenuItem";
            runToolStripMenuItem.Size = new Size(140, 22);
            runToolStripMenuItem.Text = "Run ";
            // 
            // stopToolStripMenuItem
            // 
            stopToolStripMenuItem.Name = "stopToolStripMenuItem";
            stopToolStripMenuItem.Size = new Size(140, 22);
            stopToolStripMenuItem.Text = "Stop";
            // 
            // pauseToolStripMenuItem
            // 
            pauseToolStripMenuItem.Name = "pauseToolStripMenuItem";
            pauseToolStripMenuItem.Size = new Size(140, 22);
            pauseToolStripMenuItem.Text = "Pause";
            // 
            // stepForwardToolStripMenuItem
            // 
            stepForwardToolStripMenuItem.Name = "stepForwardToolStripMenuItem";
            stepForwardToolStripMenuItem.Size = new Size(140, 22);
            stepForwardToolStripMenuItem.Text = "StepForward";
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { documentationToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(44, 20);
            helpToolStripMenuItem.Text = "Help";
            // 
            // documentationToolStripMenuItem
            // 
            documentationToolStripMenuItem.Name = "documentationToolStripMenuItem";
            documentationToolStripMenuItem.Size = new Size(157, 22);
            documentationToolStripMenuItem.Text = "Documentation";
            // 
            // SceneHierarchy
            // 
            SceneHierarchy.Controls.Add(SceneHierarchyTreeView);
            SceneHierarchy.Dock = DockStyle.Left;
            SceneHierarchy.Location = new Point(0, 24);
            SceneHierarchy.Name = "SceneHierarchy";
            SceneHierarchy.Size = new Size(171, 657);
            SceneHierarchy.TabIndex = 1;
            SceneHierarchy.TabStop = false;
            SceneHierarchy.Text = "Scene Hierarchy";
            // 
            // SceneHierarchyTreeView
            // 
            SceneHierarchyTreeView.AllowDrop = true;
            SceneHierarchyTreeView.Dock = DockStyle.Fill;
            SceneHierarchyTreeView.LabelEdit = true;
            SceneHierarchyTreeView.Location = new Point(3, 19);
            SceneHierarchyTreeView.Name = "SceneHierarchyTreeView";
            SceneHierarchyTreeView.Size = new Size(165, 635);
            SceneHierarchyTreeView.TabIndex = 1;
            SceneHierarchyTreeView.ItemDrag += SceneHierarchyTreeView_ItemDrag;
            SceneHierarchyTreeView.DragDrop += SceneHierarchyTreeView_DragDrop;
            SceneHierarchyTreeView.DragEnter += SceneHierarchyTreeView_DragEnter;
            // 
            // PropertiesWindow
            // 
            PropertiesWindow.Controls.Add(InspectorFlowPanel);
            PropertiesWindow.Dock = DockStyle.Right;
            PropertiesWindow.FlatStyle = FlatStyle.Flat;
            PropertiesWindow.Location = new Point(1064, 24);
            PropertiesWindow.Name = "PropertiesWindow";
            PropertiesWindow.Size = new Size(200, 657);
            PropertiesWindow.TabIndex = 2;
            PropertiesWindow.TabStop = false;
            PropertiesWindow.Text = "Properties";
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(SceneView);
            tabControl1.Dock = DockStyle.Top;
            tabControl1.Location = new Point(171, 24);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(893, 449);
            tabControl1.TabIndex = 3;
            // 
            // SceneView
            // 
            SceneView.Controls.Add(mgWindowControl);
            SceneView.Location = new Point(4, 24);
            SceneView.Name = "SceneView";
            SceneView.Padding = new Padding(3);
            SceneView.Size = new Size(885, 421);
            SceneView.TabIndex = 0;
            SceneView.Text = "Scene View";
            SceneView.UseVisualStyleBackColor = true;
            // 
            // mgWindowControl
            // 
            mgWindowControl.Dock = DockStyle.Fill;
            mgWindowControl.Location = new Point(3, 3);
            mgWindowControl.MouseHoverUpdatesOnly = false;
            mgWindowControl.Name = "mgWindowControl";
            mgWindowControl.Size = new Size(879, 415);
            mgWindowControl.TabIndex = 0;
            mgWindowControl.Text = "mgWindowControl2";
            // 
            // tabControl2
            // 
            tabControl2.Controls.Add(ProjectFolderTabPage);
            tabControl2.Controls.Add(ConsoleTabPage);
            tabControl2.Dock = DockStyle.Bottom;
            tabControl2.Location = new Point(171, 475);
            tabControl2.Name = "tabControl2";
            tabControl2.SelectedIndex = 0;
            tabControl2.Size = new Size(893, 206);
            tabControl2.TabIndex = 1;
            // 
            // ProjectFolderTabPage
            // 
            ProjectFolderTabPage.Controls.Add(ProjectFolderTreeView);
            ProjectFolderTabPage.Location = new Point(4, 24);
            ProjectFolderTabPage.Name = "ProjectFolderTabPage";
            ProjectFolderTabPage.Padding = new Padding(3);
            ProjectFolderTabPage.Size = new Size(885, 178);
            ProjectFolderTabPage.TabIndex = 0;
            ProjectFolderTabPage.Text = "Project Folder";
            ProjectFolderTabPage.UseVisualStyleBackColor = true;
            // 
            // ProjectFolderTreeView
            // 
            ProjectFolderTreeView.Dock = DockStyle.Fill;
            ProjectFolderTreeView.HotTracking = true;
            ProjectFolderTreeView.Location = new Point(3, 3);
            ProjectFolderTreeView.Name = "ProjectFolderTreeView";
            ProjectFolderTreeView.Size = new Size(879, 172);
            ProjectFolderTreeView.TabIndex = 1;
            // 
            // ConsoleTabPage
            // 
            ConsoleTabPage.Controls.Add(ConsoleTextBox);
            ConsoleTabPage.Location = new Point(4, 24);
            ConsoleTabPage.Name = "ConsoleTabPage";
            ConsoleTabPage.Padding = new Padding(3);
            ConsoleTabPage.Size = new Size(885, 178);
            ConsoleTabPage.TabIndex = 1;
            ConsoleTabPage.Text = "Editor Console";
            ConsoleTabPage.UseVisualStyleBackColor = true;
            // 
            // ConsoleTextBox
            // 
            ConsoleTextBox.Dock = DockStyle.Fill;
            ConsoleTextBox.Location = new Point(3, 3);
            ConsoleTextBox.Multiline = true;
            ConsoleTextBox.Name = "ConsoleTextBox";
            ConsoleTextBox.ReadOnly = true;
            ConsoleTextBox.ScrollBars = ScrollBars.Vertical;
            ConsoleTextBox.Size = new Size(879, 172);
            ConsoleTextBox.TabIndex = 0;
            // 
            // InspectorFlowPanel
            // 
            InspectorFlowPanel.AutoScroll = true;
            InspectorFlowPanel.Dock = DockStyle.Fill;
            InspectorFlowPanel.FlowDirection = FlowDirection.TopDown;
            InspectorFlowPanel.Location = new Point(3, 19);
            InspectorFlowPanel.Name = "InspectorFlowPanel";
            InspectorFlowPanel.Size = new Size(194, 635);
            InspectorFlowPanel.TabIndex = 0;
            InspectorFlowPanel.WrapContents = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1264, 681);
            Controls.Add(tabControl2);
            Controls.Add(tabControl1);
            Controls.Add(PropertiesWindow);
            Controls.Add(SceneHierarchy);
            Controls.Add(menuStrip1);
            Icon = (Icon) resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Margin = new Padding(3, 2, 3, 2);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form1";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            SceneHierarchy.ResumeLayout(false);
            PropertiesWindow.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            SceneView.ResumeLayout(false);
            tabControl2.ResumeLayout(false);
            ProjectFolderTabPage.ResumeLayout(false);
            ConsoleTabPage.ResumeLayout(false);
            ConsoleTabPage.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem menuToolStripMenuItem;
        private ToolStripMenuItem newProjectToolStripMenuItem;
        private ToolStripMenuItem loadProjectToolStripMenuItem;
        private ToolStripMenuItem saveProjectToolStripMenuItem;
        private ToolStripMenuItem saveProjectAsToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem prjoectPreferencesToolStripMenuItem;
        private ToolStripMenuItem editShortcutsToolStripMenuItem;
        private ToolStripMenuItem assetsToolStripMenuItem;
        private ToolStripMenuItem addNewAssetToolStripMenuItem;
        private ToolStripMenuItem importAssetsToolStripMenuItem;
        private ToolStripMenuItem openMGCBToolStripMenuItem;
        private ToolStripMenuItem windowToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem spriteEditorToolStripMenuItem;
        private ToolStripMenuItem propertiesToolStripMenuItem;
        private ToolStripMenuItem documentationToolStripMenuItem;
        private ToolStripMenuItem gameObjectToolStripMenuItem;
        private ToolStripMenuItem componentToolStripMenuItem;
        private ToolStripMenuItem gameSystemToolStripMenuItem;
        private ToolStripMenuItem gameManagerToolStripMenuItem;
        private ToolStripMenuItem gameEventToolStripMenuItem;
        private ToolStripMenuItem reimportAllAssetsToolStripMenuItem;
        private ToolStripMenuItem sceneHierarchyToolStripMenuItem;
        private ToolStripMenuItem consoleToolStripMenuItem;
        private ToolStripMenuItem toolsToolStripMenuItem;
        private ToolStripMenuItem spriteEditorToolStripMenuItem1;
        private ToolStripMenuItem tileMapEditorToolStripMenuItem;
        private ToolStripMenuItem animatorToolStripMenuItem;
        private ToolStripMenuItem uICanvasToolStripMenuItem;
        private ToolStripMenuItem projectSettingsToolStripMenuItem;
        private ToolStripMenuItem undoToolStripMenuItem;
        private ToolStripMenuItem redoToolStripMenuItem;
        private ToolStripMenuItem cutToolStripMenuItem;
        private ToolStripMenuItem copyToolStripMenuItem;
        private ToolStripMenuItem pasteToolStripMenuItem;
        private ToolStripMenuItem duplicateToolStripMenuItem;
        private ToolStripMenuItem deleteToolStripMenuItem;
        private ToolStripMenuItem audioMixerToolStripMenuItem;
        private ToolStripMenuItem databaseToolStripMenuItem;
        private ToolStripMenuItem resourceToolStripMenuItem;
        private GroupBox SceneHierarchy;
        private GroupBox PropertiesWindow;
        private TabControl tabControl1;
        private TabPage SceneView;
        private Editor.MGWindowControl mgWindowControl1;
        private ToolStripMenuItem gameToolStripMenuItem;
        private ToolStripMenuItem runToolStripMenuItem;
        private ToolStripMenuItem stopToolStripMenuItem;
        private TabControl tabControl2;
        private TabPage ProjectFolderTabPage;
        private TabPage ConsoleTabPage;
        private TextBox ConsoleTextBox;
        private ToolStripMenuItem codeTemplatesToolStripMenuItem;
        private ToolStripMenuItem projectFolderToolStripMenuItem;
        private ToolStripMenuItem pauseToolStripMenuItem;
        private ToolStripMenuItem stepForwardToolStripMenuItem;
        private TreeView SceneHierarchyTreeView;
        private Editor.TextSearchBarControl SceneHierarchySearchBar;
        private TreeView ProjectFolderTreeView;
        private Editor.TextSearchBarControl ProjectFolderSearchBar;
        private ToolStripMenuItem fileToolStripMenuItem;
        private Editor.MGWindowControl mgWindowControl;
        private FlowLayoutPanel InspectorFlowPanel;
    }
}