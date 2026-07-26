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
            assetsToolStripMenuItem = new ToolStripMenuItem();
            importAssetsToolStripMenuItem = new ToolStripMenuItem();
            openMGCBToolStripMenuItem = new ToolStripMenuItem();
            reimportAllAssetsToolStripMenuItem = new ToolStripMenuItem();
            windowToolStripMenuItem = new ToolStripMenuItem();
            toolsToolStripMenuItem = new ToolStripMenuItem();
            spriteEditorToolStripMenuItem1 = new ToolStripMenuItem();
            tileMapEditorToolStripMenuItem = new ToolStripMenuItem();
            animatorToolStripMenuItem = new ToolStripMenuItem();
            uICanvasToolStripMenuItem = new ToolStripMenuItem();
            audioMixerToolStripMenuItem = new ToolStripMenuItem();
            databaseViewerToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            documentationToolStripMenuItem = new ToolStripMenuItem();
            SceneHierarchyPanel = new GroupBox();
            TabPage = new TabControl();
            SceneHierarchyTab = new TabPage();
            SceneHierarchyTreeView = new TreeView();
            HierarchyToolStrip = new ToolStrip();
            SaveSceneButton = new ToolStripButton();
            LoadSceneButton = new ToolStripButton();
            CreateNewSceneButton = new ToolStripButton();
            SceneNameBox = new ToolStripTextBox();
            sceneHierarchySearchBar = new Editor.TextSearchBarControl();
            ManagersTab = new TabPage();
            ManagerListView = new ListView();
            toolStrip1 = new ToolStrip();
            AddManagerDropdownButton = new ToolStripDropDownButton();
            RemoveManagerButton = new ToolStripButton();
            SystemsTab = new TabPage();
            SystemListView = new ListView();
            toolStrip2 = new ToolStrip();
            AddSystemDropdownButton = new ToolStripDropDownButton();
            RemoveSystemButton = new ToolStripButton();
            UITab = new TabPage();
            UIListView = new ListView();
            UIToolStrip = new ToolStrip();
            AddUIScreenButton = new ToolStripDropDownButton();
            RemoveUIScreenButton = new ToolStripButton();
            LaunchGumEditorButton = new ToolStripButton();
            PropertiesWindow = new GroupBox();
            InspectorFlowPanel = new FlowLayoutPanel();
            propertiesToolStrip = new ToolStrip();
            AddComponentButton = new ToolStripDropDownButton();
            RemoveComponentButton = new ToolStripButton();
            tabControl1 = new TabControl();
            SceneView = new TabPage();
            mgWindowControl = new Editor.MGWindowControl();
            SceneToolStrip = new ToolStrip();
            StartSimulationButton = new ToolStripButton();
            PauseSimulationButton = new ToolStripButton();
            StopSimulationButton = new ToolStripButton();
            progressBarTextBox = new ToolStripTextBox();
            tabControl2 = new TabControl();
            ProjectFolderTabPage = new TabPage();
            ProjectFolderTreeView = new TreeView();
            ConsoleTabPage = new TabPage();
            ConsoleTextBox = new RichTextBox();
            consoleSearchBar = new Editor.TextSearchBarControl();
            panel1 = new Panel();
            panel2 = new Panel();
            panel3 = new Panel();
            splitContainer1 = new SplitContainer();
            splitContainer2 = new SplitContainer();
            splitContainer3 = new SplitContainer();
            menuStrip1.SuspendLayout();
            SceneHierarchyPanel.SuspendLayout();
            TabPage.SuspendLayout();
            SceneHierarchyTab.SuspendLayout();
            HierarchyToolStrip.SuspendLayout();
            ManagersTab.SuspendLayout();
            toolStrip1.SuspendLayout();
            SystemsTab.SuspendLayout();
            toolStrip2.SuspendLayout();
            UITab.SuspendLayout();
            UIToolStrip.SuspendLayout();
            PropertiesWindow.SuspendLayout();
            propertiesToolStrip.SuspendLayout();
            tabControl1.SuspendLayout();
            SceneView.SuspendLayout();
            SceneToolStrip.SuspendLayout();
            tabControl2.SuspendLayout();
            ProjectFolderTabPage.SuspendLayout();
            ConsoleTabPage.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) splitContainer3).BeginInit();
            splitContainer3.Panel1.SuspendLayout();
            splitContainer3.Panel2.SuspendLayout();
            splitContainer3.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.BackColor = SystemColors.Control;
            menuStrip1.Items.AddRange(new ToolStripItem[] { menuToolStripMenuItem, editToolStripMenuItem, assetsToolStripMenuItem, windowToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(738, 24);
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
            fileToolStripMenuItem.Text = "Build Game";
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { projectSettingsToolStripMenuItem, prjoectPreferencesToolStripMenuItem, editShortcutsToolStripMenuItem });
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(39, 20);
            editToolStripMenuItem.Text = "Edit";
            // 
            // projectSettingsToolStripMenuItem
            // 
            projectSettingsToolStripMenuItem.Name = "projectSettingsToolStripMenuItem";
            projectSettingsToolStripMenuItem.Size = new Size(175, 22);
            projectSettingsToolStripMenuItem.Text = "Project Settings";
            projectSettingsToolStripMenuItem.Click += OpenProjectSettings;
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
            // assetsToolStripMenuItem
            // 
            assetsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { importAssetsToolStripMenuItem, openMGCBToolStripMenuItem, reimportAllAssetsToolStripMenuItem });
            assetsToolStripMenuItem.Name = "assetsToolStripMenuItem";
            assetsToolStripMenuItem.Size = new Size(52, 20);
            assetsToolStripMenuItem.Text = "Assets";
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
            windowToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { toolsToolStripMenuItem });
            windowToolStripMenuItem.Name = "windowToolStripMenuItem";
            windowToolStripMenuItem.Overflow = ToolStripItemOverflow.AsNeeded;
            windowToolStripMenuItem.Size = new Size(63, 20);
            windowToolStripMenuItem.Text = "Window";
            // 
            // toolsToolStripMenuItem
            // 
            toolsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { spriteEditorToolStripMenuItem1, tileMapEditorToolStripMenuItem, animatorToolStripMenuItem, uICanvasToolStripMenuItem, audioMixerToolStripMenuItem, databaseViewerToolStripMenuItem });
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new Size(101, 22);
            toolsToolStripMenuItem.Text = "Tools";
            // 
            // spriteEditorToolStripMenuItem1
            // 
            spriteEditorToolStripMenuItem1.Name = "spriteEditorToolStripMenuItem1";
            spriteEditorToolStripMenuItem1.Size = new Size(160, 22);
            spriteEditorToolStripMenuItem1.Text = "Sprite Editor";
            // 
            // tileMapEditorToolStripMenuItem
            // 
            tileMapEditorToolStripMenuItem.Name = "tileMapEditorToolStripMenuItem";
            tileMapEditorToolStripMenuItem.Size = new Size(160, 22);
            tileMapEditorToolStripMenuItem.Text = "TileMap Editor";
            // 
            // animatorToolStripMenuItem
            // 
            animatorToolStripMenuItem.Name = "animatorToolStripMenuItem";
            animatorToolStripMenuItem.Size = new Size(160, 22);
            animatorToolStripMenuItem.Text = "Animator";
            // 
            // uICanvasToolStripMenuItem
            // 
            uICanvasToolStripMenuItem.Name = "uICanvasToolStripMenuItem";
            uICanvasToolStripMenuItem.Size = new Size(160, 22);
            uICanvasToolStripMenuItem.Text = "UI Canvas";
            // 
            // audioMixerToolStripMenuItem
            // 
            audioMixerToolStripMenuItem.Name = "audioMixerToolStripMenuItem";
            audioMixerToolStripMenuItem.Size = new Size(160, 22);
            audioMixerToolStripMenuItem.Text = "Audio Mixer";
            // 
            // databaseViewerToolStripMenuItem
            // 
            databaseViewerToolStripMenuItem.Name = "databaseViewerToolStripMenuItem";
            databaseViewerToolStripMenuItem.Size = new Size(160, 22);
            databaseViewerToolStripMenuItem.Text = "Database Viewer";
            databaseViewerToolStripMenuItem.Click += databaseViewerToolStripMenuItem_Click;
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
            // SceneHierarchyPanel
            // 
            SceneHierarchyPanel.Controls.Add(TabPage);
            SceneHierarchyPanel.Dock = DockStyle.Fill;
            SceneHierarchyPanel.Location = new Point(0, 0);
            SceneHierarchyPanel.Name = "SceneHierarchyPanel";
            SceneHierarchyPanel.Size = new Size(238, 681);
            SceneHierarchyPanel.TabIndex = 1;
            SceneHierarchyPanel.TabStop = false;
            SceneHierarchyPanel.Text = "Scene Hierarchy";
            // 
            // TabPage
            // 
            TabPage.Controls.Add(SceneHierarchyTab);
            TabPage.Controls.Add(ManagersTab);
            TabPage.Controls.Add(SystemsTab);
            TabPage.Controls.Add(UITab);
            TabPage.Dock = DockStyle.Fill;
            TabPage.Location = new Point(3, 19);
            TabPage.Name = "TabPage";
            TabPage.SelectedIndex = 0;
            TabPage.Size = new Size(232, 659);
            TabPage.TabIndex = 4;
            // 
            // SceneHierarchyTab
            // 
            SceneHierarchyTab.Controls.Add(SceneHierarchyTreeView);
            SceneHierarchyTab.Controls.Add(HierarchyToolStrip);
            SceneHierarchyTab.Controls.Add(sceneHierarchySearchBar);
            SceneHierarchyTab.Location = new Point(4, 24);
            SceneHierarchyTab.Name = "SceneHierarchyTab";
            SceneHierarchyTab.Padding = new Padding(3);
            SceneHierarchyTab.Size = new Size(224, 631);
            SceneHierarchyTab.TabIndex = 0;
            SceneHierarchyTab.Text = "Scene";
            SceneHierarchyTab.UseVisualStyleBackColor = true;
            // 
            // SceneHierarchyTreeView
            // 
            SceneHierarchyTreeView.AllowDrop = true;
            SceneHierarchyTreeView.Dock = DockStyle.Fill;
            SceneHierarchyTreeView.LabelEdit = true;
            SceneHierarchyTreeView.Location = new Point(3, 53);
            SceneHierarchyTreeView.Name = "SceneHierarchyTreeView";
            SceneHierarchyTreeView.Size = new Size(218, 575);
            SceneHierarchyTreeView.TabIndex = 1;
            SceneHierarchyTreeView.ItemDrag += SceneHierarchyTreeView_ItemDrag;
            SceneHierarchyTreeView.DragDrop += SceneHierarchyTreeView_DragDrop;
            SceneHierarchyTreeView.DragEnter += SceneHierarchyTreeView_DragEnter;
            // 
            // HierarchyToolStrip
            // 
            HierarchyToolStrip.Items.AddRange(new ToolStripItem[] { SaveSceneButton, LoadSceneButton, CreateNewSceneButton, SceneNameBox });
            HierarchyToolStrip.Location = new Point(3, 28);
            HierarchyToolStrip.Name = "HierarchyToolStrip";
            HierarchyToolStrip.Size = new Size(218, 25);
            HierarchyToolStrip.TabIndex = 2;
            HierarchyToolStrip.Text = "toolStrip2";
            // 
            // SaveSceneButton
            // 
            SaveSceneButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            SaveSceneButton.Image = (Image) resources.GetObject("SaveSceneButton.Image");
            SaveSceneButton.ImageTransparentColor = Color.Magenta;
            SaveSceneButton.Name = "SaveSceneButton";
            SaveSceneButton.Size = new Size(23, 22);
            SaveSceneButton.Text = "SaveScene";
            SaveSceneButton.Click += onSaveProjectToolStripMenuItem_Click;
            // 
            // LoadSceneButton
            // 
            LoadSceneButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            LoadSceneButton.Image = (Image) resources.GetObject("LoadSceneButton.Image");
            LoadSceneButton.ImageTransparentColor = Color.Magenta;
            LoadSceneButton.Name = "LoadSceneButton";
            LoadSceneButton.Size = new Size(23, 22);
            LoadSceneButton.Text = "LoadScene";
            LoadSceneButton.Click += LoadSceneButton_Click;
            // 
            // CreateNewSceneButton
            // 
            CreateNewSceneButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            CreateNewSceneButton.Image = (Image) resources.GetObject("CreateNewSceneButton.Image");
            CreateNewSceneButton.ImageTransparentColor = Color.Magenta;
            CreateNewSceneButton.Name = "CreateNewSceneButton";
            CreateNewSceneButton.Size = new Size(23, 22);
            CreateNewSceneButton.Text = "CreateNewScene";
            CreateNewSceneButton.Click += CreateNewSceneButton_Click;
            // 
            // SceneNameBox
            // 
            SceneNameBox.Name = "SceneNameBox";
            SceneNameBox.Size = new Size(100, 25);
            SceneNameBox.TextChanged += SceneNameBox_TextChanged;
            // 
            // sceneHierarchySearchBar
            // 
            sceneHierarchySearchBar.Dock = DockStyle.Top;
            sceneHierarchySearchBar.Location = new Point(3, 3);
            sceneHierarchySearchBar.Name = "sceneHierarchySearchBar";
            sceneHierarchySearchBar.Size = new Size(218, 25);
            sceneHierarchySearchBar.TabIndex = 3;
            sceneHierarchySearchBar.Load += textSearchBarControl1_Load;
            // 
            // ManagersTab
            // 
            ManagersTab.Controls.Add(ManagerListView);
            ManagersTab.Controls.Add(toolStrip1);
            ManagersTab.Location = new Point(4, 24);
            ManagersTab.Name = "ManagersTab";
            ManagersTab.Padding = new Padding(3);
            ManagersTab.Size = new Size(224, 631);
            ManagersTab.TabIndex = 1;
            ManagersTab.Text = "Managers";
            ManagersTab.UseVisualStyleBackColor = true;
            // 
            // ManagerListView
            // 
            ManagerListView.Dock = DockStyle.Fill;
            ManagerListView.Location = new Point(3, 28);
            ManagerListView.Name = "ManagerListView";
            ManagerListView.Size = new Size(218, 600);
            ManagerListView.TabIndex = 0;
            ManagerListView.UseCompatibleStateImageBehavior = false;
            // 
            // toolStrip1
            // 
            toolStrip1.Items.AddRange(new ToolStripItem[] { AddManagerDropdownButton, RemoveManagerButton });
            toolStrip1.Location = new Point(3, 3);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(218, 25);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // AddManagerDropdownButton
            // 
            AddManagerDropdownButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            AddManagerDropdownButton.Image = (Image) resources.GetObject("AddManagerDropdownButton.Image");
            AddManagerDropdownButton.ImageTransparentColor = Color.Magenta;
            AddManagerDropdownButton.Name = "AddManagerDropdownButton";
            AddManagerDropdownButton.Size = new Size(29, 22);
            AddManagerDropdownButton.Text = "AddManagerDropdownButton";
            // 
            // RemoveManagerButton
            // 
            RemoveManagerButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            RemoveManagerButton.Image = (Image) resources.GetObject("RemoveManagerButton.Image");
            RemoveManagerButton.ImageTransparentColor = Color.Magenta;
            RemoveManagerButton.Name = "RemoveManagerButton";
            RemoveManagerButton.Size = new Size(23, 22);
            RemoveManagerButton.Text = "RemoveManagerButton";
            // 
            // SystemsTab
            // 
            SystemsTab.Controls.Add(SystemListView);
            SystemsTab.Controls.Add(toolStrip2);
            SystemsTab.Location = new Point(4, 24);
            SystemsTab.Name = "SystemsTab";
            SystemsTab.Size = new Size(224, 631);
            SystemsTab.TabIndex = 2;
            SystemsTab.Text = "Systems";
            SystemsTab.UseVisualStyleBackColor = true;
            // 
            // SystemListView
            // 
            SystemListView.Dock = DockStyle.Fill;
            SystemListView.Location = new Point(0, 25);
            SystemListView.Name = "SystemListView";
            SystemListView.Size = new Size(224, 606);
            SystemListView.TabIndex = 1;
            SystemListView.UseCompatibleStateImageBehavior = false;
            // 
            // toolStrip2
            // 
            toolStrip2.Items.AddRange(new ToolStripItem[] { AddSystemDropdownButton, RemoveSystemButton });
            toolStrip2.Location = new Point(0, 0);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new Size(224, 25);
            toolStrip2.TabIndex = 0;
            toolStrip2.Text = "toolStrip2";
            // 
            // AddSystemDropdownButton
            // 
            AddSystemDropdownButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            AddSystemDropdownButton.Image = (Image) resources.GetObject("AddSystemDropdownButton.Image");
            AddSystemDropdownButton.ImageTransparentColor = Color.Magenta;
            AddSystemDropdownButton.Name = "AddSystemDropdownButton";
            AddSystemDropdownButton.Size = new Size(29, 22);
            AddSystemDropdownButton.Text = "AddSystemDropdownButton";
            // 
            // RemoveSystemButton
            // 
            RemoveSystemButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            RemoveSystemButton.Image = (Image) resources.GetObject("RemoveSystemButton.Image");
            RemoveSystemButton.ImageTransparentColor = Color.Magenta;
            RemoveSystemButton.Name = "RemoveSystemButton";
            RemoveSystemButton.Size = new Size(23, 22);
            RemoveSystemButton.Text = "RemoveSystemButton";
            // 
            // UITab
            // 
            UITab.Controls.Add(UIListView);
            UITab.Controls.Add(UIToolStrip);
            UITab.Location = new Point(4, 24);
            UITab.Name = "UITab";
            UITab.Padding = new Padding(3);
            UITab.Size = new Size(224, 631);
            UITab.TabIndex = 3;
            UITab.Text = "UI";
            UITab.UseVisualStyleBackColor = true;
            // 
            // UIListView
            // 
            UIListView.Dock = DockStyle.Fill;
            UIListView.Location = new Point(3, 28);
            UIListView.Name = "UIListView";
            UIListView.Size = new Size(218, 600);
            UIListView.TabIndex = 1;
            UIListView.UseCompatibleStateImageBehavior = false;
            // 
            // UIToolStrip
            // 
            UIToolStrip.Items.AddRange(new ToolStripItem[] { AddUIScreenButton, RemoveUIScreenButton, LaunchGumEditorButton });
            UIToolStrip.Location = new Point(3, 3);
            UIToolStrip.Name = "UIToolStrip";
            UIToolStrip.Size = new Size(218, 25);
            UIToolStrip.TabIndex = 0;
            UIToolStrip.Text = "toolStrip3";
            // 
            // AddUIScreenButton
            // 
            AddUIScreenButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            AddUIScreenButton.Image = (Image) resources.GetObject("AddUIScreenButton.Image");
            AddUIScreenButton.ImageTransparentColor = Color.Magenta;
            AddUIScreenButton.Name = "AddUIScreenButton";
            AddUIScreenButton.Size = new Size(29, 22);
            AddUIScreenButton.Text = "AddUIScreen";
            // 
            // RemoveUIScreenButton
            // 
            RemoveUIScreenButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            RemoveUIScreenButton.Image = (Image) resources.GetObject("RemoveUIScreenButton.Image");
            RemoveUIScreenButton.ImageTransparentColor = Color.Magenta;
            RemoveUIScreenButton.Name = "RemoveUIScreenButton";
            RemoveUIScreenButton.Size = new Size(23, 22);
            RemoveUIScreenButton.Text = "RemoveUIScreen";
            // 
            // LaunchGumEditorButton
            // 
            LaunchGumEditorButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            LaunchGumEditorButton.Image = (Image) resources.GetObject("LaunchGumEditorButton.Image");
            LaunchGumEditorButton.ImageTransparentColor = Color.Magenta;
            LaunchGumEditorButton.Name = "LaunchGumEditorButton";
            LaunchGumEditorButton.Size = new Size(87, 22);
            LaunchGumEditorButton.Text = "LaunchGumUI";
            LaunchGumEditorButton.Click += LaunchGumEditorButton_Click;
            // 
            // PropertiesWindow
            // 
            PropertiesWindow.AutoSize = true;
            PropertiesWindow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            PropertiesWindow.Controls.Add(InspectorFlowPanel);
            PropertiesWindow.Controls.Add(propertiesToolStrip);
            PropertiesWindow.Dock = DockStyle.Fill;
            PropertiesWindow.FlatStyle = FlatStyle.Flat;
            PropertiesWindow.Location = new Point(0, 0);
            PropertiesWindow.Name = "PropertiesWindow";
            PropertiesWindow.Size = new Size(280, 681);
            PropertiesWindow.TabIndex = 2;
            PropertiesWindow.TabStop = false;
            PropertiesWindow.Text = "Properties";
            // 
            // InspectorFlowPanel
            // 
            InspectorFlowPanel.AutoScroll = true;
            InspectorFlowPanel.Dock = DockStyle.Fill;
            InspectorFlowPanel.FlowDirection = FlowDirection.TopDown;
            InspectorFlowPanel.Location = new Point(3, 44);
            InspectorFlowPanel.Name = "InspectorFlowPanel";
            InspectorFlowPanel.Size = new Size(274, 634);
            InspectorFlowPanel.TabIndex = 0;
            InspectorFlowPanel.WrapContents = false;
            // 
            // propertiesToolStrip
            // 
            propertiesToolStrip.Items.AddRange(new ToolStripItem[] { AddComponentButton, RemoveComponentButton });
            propertiesToolStrip.Location = new Point(3, 19);
            propertiesToolStrip.Name = "propertiesToolStrip";
            propertiesToolStrip.Size = new Size(274, 25);
            propertiesToolStrip.TabIndex = 1;
            propertiesToolStrip.Text = "toolStrip3";
            // 
            // AddComponentButton
            // 
            AddComponentButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            AddComponentButton.Image = (Image) resources.GetObject("AddComponentButton.Image");
            AddComponentButton.ImageTransparentColor = Color.Magenta;
            AddComponentButton.Name = "AddComponentButton";
            AddComponentButton.Size = new Size(29, 22);
            AddComponentButton.Text = "AddComponent";
            // 
            // RemoveComponentButton
            // 
            RemoveComponentButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            RemoveComponentButton.Image = (Image) resources.GetObject("RemoveComponentButton.Image");
            RemoveComponentButton.ImageTransparentColor = Color.Magenta;
            RemoveComponentButton.Name = "RemoveComponentButton";
            RemoveComponentButton.Size = new Size(23, 22);
            RemoveComponentButton.Text = "RemoveComponent";
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(SceneView);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 24);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(738, 461);
            tabControl1.TabIndex = 3;
            // 
            // SceneView
            // 
            SceneView.Controls.Add(mgWindowControl);
            SceneView.Controls.Add(SceneToolStrip);
            SceneView.Location = new Point(4, 24);
            SceneView.Name = "SceneView";
            SceneView.Padding = new Padding(3);
            SceneView.Size = new Size(730, 433);
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
            mgWindowControl.SimulationPaused = false;
            mgWindowControl.SimulationRunning = false;
            mgWindowControl.Size = new Size(724, 402);
            mgWindowControl.TabIndex = 0;
            mgWindowControl.Text = "mgWindowControl2";
            // 
            // SceneToolStrip
            // 
            SceneToolStrip.AutoSize = false;
            SceneToolStrip.BackColor = SystemColors.Control;
            SceneToolStrip.Dock = DockStyle.Bottom;
            SceneToolStrip.Items.AddRange(new ToolStripItem[] { StartSimulationButton, PauseSimulationButton, StopSimulationButton, progressBarTextBox });
            SceneToolStrip.Location = new Point(3, 405);
            SceneToolStrip.Name = "SceneToolStrip";
            SceneToolStrip.Size = new Size(724, 25);
            SceneToolStrip.TabIndex = 4;
            SceneToolStrip.Text = "toolStrip1";
            // 
            // StartSimulationButton
            // 
            StartSimulationButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            StartSimulationButton.Image = (Image) resources.GetObject("StartSimulationButton.Image");
            StartSimulationButton.ImageTransparentColor = Color.Magenta;
            StartSimulationButton.Name = "StartSimulationButton";
            StartSimulationButton.Size = new Size(23, 22);
            StartSimulationButton.Text = "StartSimulation";
            StartSimulationButton.Click += StartSimulationButton_Click;
            // 
            // PauseSimulationButton
            // 
            PauseSimulationButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            PauseSimulationButton.Image = (Image) resources.GetObject("PauseSimulationButton.Image");
            PauseSimulationButton.ImageTransparentColor = Color.Magenta;
            PauseSimulationButton.Name = "PauseSimulationButton";
            PauseSimulationButton.Size = new Size(23, 22);
            PauseSimulationButton.Text = "PauseSimulation";
            PauseSimulationButton.Click += PauseSimulationButton_Click;
            // 
            // StopSimulationButton
            // 
            StopSimulationButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            StopSimulationButton.Image = (Image) resources.GetObject("StopSimulationButton.Image");
            StopSimulationButton.ImageTransparentColor = Color.Magenta;
            StopSimulationButton.Name = "StopSimulationButton";
            StopSimulationButton.Size = new Size(23, 22);
            StopSimulationButton.Text = "StopSimulation";
            StopSimulationButton.Click += StopSimulationButton_Click;
            // 
            // progressBarTextBox
            // 
            progressBarTextBox.Name = "progressBarTextBox";
            progressBarTextBox.ReadOnly = true;
            progressBarTextBox.Size = new Size(300, 25);
            // 
            // tabControl2
            // 
            tabControl2.Controls.Add(ProjectFolderTabPage);
            tabControl2.Controls.Add(ConsoleTabPage);
            tabControl2.Dock = DockStyle.Fill;
            tabControl2.Location = new Point(0, 0);
            tabControl2.Name = "tabControl2";
            tabControl2.SelectedIndex = 0;
            tabControl2.Size = new Size(738, 192);
            tabControl2.TabIndex = 1;
            // 
            // ProjectFolderTabPage
            // 
            ProjectFolderTabPage.Controls.Add(ProjectFolderTreeView);
            ProjectFolderTabPage.Location = new Point(4, 24);
            ProjectFolderTabPage.Name = "ProjectFolderTabPage";
            ProjectFolderTabPage.Padding = new Padding(3);
            ProjectFolderTabPage.Size = new Size(730, 164);
            ProjectFolderTabPage.TabIndex = 0;
            ProjectFolderTabPage.Text = "Project Folder";
            ProjectFolderTabPage.UseVisualStyleBackColor = true;
            // 
            // ProjectFolderTreeView
            // 
            ProjectFolderTreeView.AllowDrop = true;
            ProjectFolderTreeView.Dock = DockStyle.Fill;
            ProjectFolderTreeView.HotTracking = true;
            ProjectFolderTreeView.Location = new Point(3, 3);
            ProjectFolderTreeView.Name = "ProjectFolderTreeView";
            ProjectFolderTreeView.Size = new Size(724, 158);
            ProjectFolderTreeView.TabIndex = 1;
            // 
            // ConsoleTabPage
            // 
            ConsoleTabPage.Controls.Add(ConsoleTextBox);
            ConsoleTabPage.Controls.Add(consoleSearchBar);
            ConsoleTabPage.Location = new Point(4, 24);
            ConsoleTabPage.Name = "ConsoleTabPage";
            ConsoleTabPage.Padding = new Padding(3);
            ConsoleTabPage.Size = new Size(730, 164);
            ConsoleTabPage.TabIndex = 1;
            ConsoleTabPage.Text = "Editor Console";
            ConsoleTabPage.UseVisualStyleBackColor = true;
            // 
            // ConsoleTextBox
            // 
            ConsoleTextBox.Dock = DockStyle.Bottom;
            ConsoleTextBox.Location = new Point(3, 13);
            ConsoleTextBox.Name = "ConsoleTextBox";
            ConsoleTextBox.ReadOnly = true;
            ConsoleTextBox.Size = new Size(724, 148);
            ConsoleTextBox.TabIndex = 2;
            ConsoleTextBox.Text = "";
            // 
            // consoleSearchBar
            // 
            consoleSearchBar.Dock = DockStyle.Top;
            consoleSearchBar.Location = new Point(3, 3);
            consoleSearchBar.Name = "consoleSearchBar";
            consoleSearchBar.Size = new Size(724, 25);
            consoleSearchBar.TabIndex = 1;
            consoleSearchBar.SearchTextChanged += consoleSearchBar_SearchTextChanged;
            consoleSearchBar.Load += consoleSearchBar_Load;
            // 
            // panel1
            // 
            panel1.Controls.Add(tabControl1);
            panel1.Controls.Add(menuStrip1);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(738, 485);
            panel1.TabIndex = 4;
            // 
            // panel2
            // 
            panel2.Controls.Add(SceneHierarchyPanel);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(0, 0);
            panel2.Name = "panel2";
            panel2.Size = new Size(238, 681);
            panel2.TabIndex = 5;
            // 
            // panel3
            // 
            panel3.Controls.Add(PropertiesWindow);
            panel3.Dock = DockStyle.Fill;
            panel3.Location = new Point(0, 0);
            panel3.Name = "panel3";
            panel3.Size = new Size(280, 681);
            panel3.TabIndex = 6;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(panel2);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(splitContainer2);
            splitContainer1.Size = new Size(1264, 681);
            splitContainer1.SplitterDistance = 238;
            splitContainer1.TabIndex = 5;
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.Location = new Point(0, 0);
            splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(splitContainer3);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(panel3);
            splitContainer2.Size = new Size(1022, 681);
            splitContainer2.SplitterDistance = 738;
            splitContainer2.TabIndex = 0;
            // 
            // splitContainer3
            // 
            splitContainer3.Dock = DockStyle.Fill;
            splitContainer3.Location = new Point(0, 0);
            splitContainer3.Name = "splitContainer3";
            splitContainer3.Orientation = Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            splitContainer3.Panel1.Controls.Add(panel1);
            // 
            // splitContainer3.Panel2
            // 
            splitContainer3.Panel2.Controls.Add(tabControl2);
            splitContainer3.Size = new Size(738, 681);
            splitContainer3.SplitterDistance = 485;
            splitContainer3.TabIndex = 0;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            ClientSize = new Size(1264, 681);
            Controls.Add(splitContainer1);
            Icon = (Icon) resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Margin = new Padding(3, 2, 3, 2);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form1";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            SceneHierarchyPanel.ResumeLayout(false);
            TabPage.ResumeLayout(false);
            SceneHierarchyTab.ResumeLayout(false);
            SceneHierarchyTab.PerformLayout();
            HierarchyToolStrip.ResumeLayout(false);
            HierarchyToolStrip.PerformLayout();
            ManagersTab.ResumeLayout(false);
            ManagersTab.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            SystemsTab.ResumeLayout(false);
            SystemsTab.PerformLayout();
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            UITab.ResumeLayout(false);
            UITab.PerformLayout();
            UIToolStrip.ResumeLayout(false);
            UIToolStrip.PerformLayout();
            PropertiesWindow.ResumeLayout(false);
            PropertiesWindow.PerformLayout();
            propertiesToolStrip.ResumeLayout(false);
            propertiesToolStrip.PerformLayout();
            tabControl1.ResumeLayout(false);
            SceneView.ResumeLayout(false);
            SceneToolStrip.ResumeLayout(false);
            SceneToolStrip.PerformLayout();
            tabControl2.ResumeLayout(false);
            ProjectFolderTabPage.ResumeLayout(false);
            ConsoleTabPage.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            splitContainer3.Panel1.ResumeLayout(false);
            splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) splitContainer3).EndInit();
            splitContainer3.ResumeLayout(false);
            ResumeLayout(false);
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
        private ToolStripMenuItem importAssetsToolStripMenuItem;
        private ToolStripMenuItem openMGCBToolStripMenuItem;
        private ToolStripMenuItem windowToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem documentationToolStripMenuItem;
        private ToolStripMenuItem reimportAllAssetsToolStripMenuItem;
        private ToolStripMenuItem toolsToolStripMenuItem;
        private ToolStripMenuItem spriteEditorToolStripMenuItem1;
        private ToolStripMenuItem tileMapEditorToolStripMenuItem;
        private ToolStripMenuItem animatorToolStripMenuItem;
        private ToolStripMenuItem uICanvasToolStripMenuItem;
        private ToolStripMenuItem projectSettingsToolStripMenuItem;
        private ToolStripMenuItem audioMixerToolStripMenuItem;
        private GroupBox SceneHierarchyPanel;
        private GroupBox PropertiesWindow;
        private TabControl tabControl1;
        private TabPage SceneView;
        private Editor.MGWindowControl mgWindowControl1;
        private TabControl tabControl2;
        private TabPage ProjectFolderTabPage;
        private TabPage ConsoleTabPage;
        public TreeView SceneHierarchyTreeView;
        private Editor.TextSearchBarControl SceneHierarchySearchBar;
        private TreeView ProjectFolderTreeView;
        private Editor.TextSearchBarControl ProjectFolderSearchBar;
        private ToolStripMenuItem fileToolStripMenuItem;
        private Editor.MGWindowControl mgWindowControl;
        private FlowLayoutPanel InspectorFlowPanel;
        private Panel panel1;
        private ToolStrip SceneToolStrip;
        private ToolStripButton StartSimulationButton;
        private ToolStripButton PauseSimulationButton;
        private ToolStripButton StopSimulationButton;
        private ToolStrip HierarchyToolStrip;
        private ToolStripButton SaveSceneButton;
        private ToolStrip propertiesToolStrip;
        private ToolStripButton RemoveComponentButton;
        private ToolStripButton LoadSceneButton;
        private ToolStripDropDownButton AddComponentButton;
        private ToolStripButton CreateNewSceneButton;
        private Editor.TextSearchBarControl sceneHierarchySearchBar;
        private Editor.TextSearchBarControl consoleSearchBar;
        private RichTextBox ConsoleTextBox;
        private TabControl TabPage;
        private TabPage SceneHierarchyTab;
        private TabPage ManagersTab;
        private TabPage SystemsTab;
        private Panel panel2;
        private Panel panel3;
        private ToolStripTextBox progressBarTextBox;
        private ToolStripTextBox SceneNameBox;
        private ToolStripMenuItem databaseViewerToolStripMenuItem;
        private SplitContainer splitContainer1;
        private SplitContainer splitContainer2;
        private SplitContainer splitContainer3;
        private ListView ManagerListView;
        private ToolStrip toolStrip1;
        private ListView SystemListView;
        private ToolStrip toolStrip2;
        private ToolStripDropDownButton AddManagerDropdownButton;
        private ToolStripButton RemoveManagerButton;
        private ToolStripDropDownButton AddSystemDropdownButton;
        private ToolStripButton RemoveSystemButton;
        private TabPage UITab;
        private ListView UIListView;
        private ToolStrip UIToolStrip;
        private ToolStripDropDownButton AddUIScreenButton;
        private ToolStripButton RemoveUIScreenButton;
        private ToolStripButton LaunchGumEditorButton;
    }
}