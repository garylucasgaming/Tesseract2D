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
            helpToolStripMenuItem = new ToolStripMenuItem();
            documentationToolStripMenuItem = new ToolStripMenuItem();
            SceneHierarchyPanel = new GroupBox();
            tabControl3 = new TabControl();
            SceneHierarchyTab = new TabPage();
            HierarchyToolStrip = new ToolStrip();
            SaveSceneButton = new ToolStripButton();
            LoadSceneButton = new ToolStripButton();
            CreateNewSceneButton = new ToolStripButton();
            sceneHierarchySearchBar = new Editor.TextSearchBarControl();
            SceneHierarchyTreeView = new TreeView();
            ManagersTab = new TabPage();
            SystemsTab = new TabPage();
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
            progressBar = new ToolStripProgressBar();
            progressBarTextBox = new ToolStripTextBox();
            panel1 = new Panel();
            splitContainer1 = new SplitContainer();
            tabControl2 = new TabControl();
            ConsoleTabPage = new TabPage();
            ConsoleTextBox = new RichTextBox();
            consoleSearchBar = new Editor.TextSearchBarControl();
            tabControl4 = new TabControl();
            tabPage1 = new TabPage();
            ProjectFolderTreeView = new TreeView();
            panel2 = new Panel();
            panel3 = new Panel();
            splitContainer2 = new SplitContainer();
            splitContainer3 = new SplitContainer();
            splitContainer4 = new SplitContainer();
            menuStrip1.SuspendLayout();
            SceneHierarchyPanel.SuspendLayout();
            tabControl3.SuspendLayout();
            SceneHierarchyTab.SuspendLayout();
            HierarchyToolStrip.SuspendLayout();
            PropertiesWindow.SuspendLayout();
            propertiesToolStrip.SuspendLayout();
            tabControl1.SuspendLayout();
            SceneView.SuspendLayout();
            SceneToolStrip.SuspendLayout();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            tabControl2.SuspendLayout();
            ConsoleTabPage.SuspendLayout();
            tabControl4.SuspendLayout();
            tabPage1.SuspendLayout();
            panel2.SuspendLayout();
            panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) splitContainer3).BeginInit();
            splitContainer3.Panel1.SuspendLayout();
            splitContainer3.Panel2.SuspendLayout();
            splitContainer3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) splitContainer4).BeginInit();
            splitContainer4.Panel1.SuspendLayout();
            splitContainer4.Panel2.SuspendLayout();
            splitContainer4.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.BackColor = SystemColors.Control;
            menuStrip1.Items.AddRange(new ToolStripItem[] { menuToolStripMenuItem, editToolStripMenuItem, assetsToolStripMenuItem, windowToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(805, 24);
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
            toolsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { spriteEditorToolStripMenuItem1, tileMapEditorToolStripMenuItem, animatorToolStripMenuItem, uICanvasToolStripMenuItem, audioMixerToolStripMenuItem });
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new Size(101, 22);
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
            SceneHierarchyPanel.Controls.Add(tabControl3);
            SceneHierarchyPanel.Dock = DockStyle.Fill;
            SceneHierarchyPanel.Location = new Point(0, 0);
            SceneHierarchyPanel.Name = "SceneHierarchyPanel";
            SceneHierarchyPanel.Size = new Size(193, 681);
            SceneHierarchyPanel.TabIndex = 1;
            SceneHierarchyPanel.TabStop = false;
            SceneHierarchyPanel.Text = "Scene Hierarchy";
            // 
            // tabControl3
            // 
            tabControl3.Controls.Add(SceneHierarchyTab);
            tabControl3.Controls.Add(ManagersTab);
            tabControl3.Controls.Add(SystemsTab);
            tabControl3.Dock = DockStyle.Fill;
            tabControl3.Location = new Point(3, 19);
            tabControl3.Name = "tabControl3";
            tabControl3.SelectedIndex = 0;
            tabControl3.Size = new Size(187, 659);
            tabControl3.TabIndex = 4;
            // 
            // SceneHierarchyTab
            // 
            SceneHierarchyTab.Controls.Add(HierarchyToolStrip);
            SceneHierarchyTab.Controls.Add(sceneHierarchySearchBar);
            SceneHierarchyTab.Controls.Add(SceneHierarchyTreeView);
            SceneHierarchyTab.Location = new Point(4, 24);
            SceneHierarchyTab.Name = "SceneHierarchyTab";
            SceneHierarchyTab.Padding = new Padding(3);
            SceneHierarchyTab.Size = new Size(179, 631);
            SceneHierarchyTab.TabIndex = 0;
            SceneHierarchyTab.Text = "Scene";
            SceneHierarchyTab.UseVisualStyleBackColor = true;
            // 
            // HierarchyToolStrip
            // 
            HierarchyToolStrip.Items.AddRange(new ToolStripItem[] { SaveSceneButton, LoadSceneButton, CreateNewSceneButton });
            HierarchyToolStrip.Location = new Point(3, 28);
            HierarchyToolStrip.Name = "HierarchyToolStrip";
            HierarchyToolStrip.Size = new Size(173, 25);
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
            LoadSceneButton.Click += onLoadProjectToolStripMenuItem_Click;
            // 
            // CreateNewSceneButton
            // 
            CreateNewSceneButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            CreateNewSceneButton.Image = (Image) resources.GetObject("CreateNewSceneButton.Image");
            CreateNewSceneButton.ImageTransparentColor = Color.Magenta;
            CreateNewSceneButton.Name = "CreateNewSceneButton";
            CreateNewSceneButton.Size = new Size(23, 22);
            CreateNewSceneButton.Text = "CreateNewScene";
            // 
            // sceneHierarchySearchBar
            // 
            sceneHierarchySearchBar.Dock = DockStyle.Top;
            sceneHierarchySearchBar.Location = new Point(3, 3);
            sceneHierarchySearchBar.Name = "sceneHierarchySearchBar";
            sceneHierarchySearchBar.Size = new Size(173, 25);
            sceneHierarchySearchBar.TabIndex = 3;
            sceneHierarchySearchBar.Load += textSearchBarControl1_Load;
            // 
            // SceneHierarchyTreeView
            // 
            SceneHierarchyTreeView.AllowDrop = true;
            SceneHierarchyTreeView.Dock = DockStyle.Bottom;
            SceneHierarchyTreeView.LabelEdit = true;
            SceneHierarchyTreeView.Location = new Point(3, 80);
            SceneHierarchyTreeView.Name = "SceneHierarchyTreeView";
            SceneHierarchyTreeView.Size = new Size(173, 548);
            SceneHierarchyTreeView.TabIndex = 1;
            SceneHierarchyTreeView.ItemDrag += SceneHierarchyTreeView_ItemDrag;
            SceneHierarchyTreeView.DragDrop += SceneHierarchyTreeView_DragDrop;
            SceneHierarchyTreeView.DragEnter += SceneHierarchyTreeView_DragEnter;
            // 
            // ManagersTab
            // 
            ManagersTab.Location = new Point(4, 24);
            ManagersTab.Name = "ManagersTab";
            ManagersTab.Padding = new Padding(3);
            ManagersTab.Size = new Size(179, 631);
            ManagersTab.TabIndex = 1;
            ManagersTab.Text = "Managers";
            ManagersTab.UseVisualStyleBackColor = true;
            // 
            // SystemsTab
            // 
            SystemsTab.Location = new Point(4, 24);
            SystemsTab.Name = "SystemsTab";
            SystemsTab.Size = new Size(179, 631);
            SystemsTab.TabIndex = 2;
            SystemsTab.Text = "Systems";
            SystemsTab.UseVisualStyleBackColor = true;
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
            PropertiesWindow.Size = new Size(258, 681);
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
            InspectorFlowPanel.Size = new Size(252, 634);
            InspectorFlowPanel.TabIndex = 0;
            InspectorFlowPanel.WrapContents = false;
            // 
            // propertiesToolStrip
            // 
            propertiesToolStrip.Items.AddRange(new ToolStripItem[] { AddComponentButton, RemoveComponentButton });
            propertiesToolStrip.Location = new Point(3, 19);
            propertiesToolStrip.Name = "propertiesToolStrip";
            propertiesToolStrip.Size = new Size(252, 25);
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
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(805, 409);
            tabControl1.TabIndex = 3;
            // 
            // SceneView
            // 
            SceneView.Controls.Add(mgWindowControl);
            SceneView.Controls.Add(SceneToolStrip);
            SceneView.Location = new Point(4, 24);
            SceneView.Name = "SceneView";
            SceneView.Padding = new Padding(3);
            SceneView.Size = new Size(797, 381);
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
            mgWindowControl.Size = new Size(791, 350);
            mgWindowControl.TabIndex = 0;
            mgWindowControl.Text = "mgWindowControl2";
            // 
            // SceneToolStrip
            // 
            SceneToolStrip.AutoSize = false;
            SceneToolStrip.BackColor = SystemColors.Control;
            SceneToolStrip.Dock = DockStyle.Bottom;
            SceneToolStrip.Items.AddRange(new ToolStripItem[] { StartSimulationButton, PauseSimulationButton, StopSimulationButton, progressBar, progressBarTextBox });
            SceneToolStrip.Location = new Point(3, 353);
            SceneToolStrip.Name = "SceneToolStrip";
            SceneToolStrip.Size = new Size(791, 25);
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
            // progressBar
            // 
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(100, 22);
            // 
            // progressBarTextBox
            // 
            progressBarTextBox.Name = "progressBarTextBox";
            progressBarTextBox.ReadOnly = true;
            progressBarTextBox.Size = new Size(300, 23);
            // 
            // panel1
            // 
            panel1.Controls.Add(splitContainer4);
            panel1.Controls.Add(menuStrip1);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(805, 681);
            panel1.TabIndex = 4;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(tabControl2);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(tabControl4);
            splitContainer1.Size = new Size(805, 244);
            splitContainer1.SplitterDistance = 373;
            splitContainer1.TabIndex = 5;
            // 
            // tabControl2
            // 
            tabControl2.Controls.Add(ConsoleTabPage);
            tabControl2.Dock = DockStyle.Fill;
            tabControl2.Location = new Point(0, 0);
            tabControl2.Name = "tabControl2";
            tabControl2.SelectedIndex = 0;
            tabControl2.Size = new Size(373, 244);
            tabControl2.TabIndex = 1;
            // 
            // ConsoleTabPage
            // 
            ConsoleTabPage.Controls.Add(ConsoleTextBox);
            ConsoleTabPage.Controls.Add(consoleSearchBar);
            ConsoleTabPage.Location = new Point(4, 24);
            ConsoleTabPage.Name = "ConsoleTabPage";
            ConsoleTabPage.Padding = new Padding(3);
            ConsoleTabPage.Size = new Size(365, 216);
            ConsoleTabPage.TabIndex = 1;
            ConsoleTabPage.Text = "Editor Console";
            ConsoleTabPage.UseVisualStyleBackColor = true;
            // 
            // ConsoleTextBox
            // 
            ConsoleTextBox.Dock = DockStyle.Bottom;
            ConsoleTextBox.Location = new Point(3, 65);
            ConsoleTextBox.Name = "ConsoleTextBox";
            ConsoleTextBox.ReadOnly = true;
            ConsoleTextBox.Size = new Size(359, 148);
            ConsoleTextBox.TabIndex = 2;
            ConsoleTextBox.Text = "";
            // 
            // consoleSearchBar
            // 
            consoleSearchBar.Dock = DockStyle.Top;
            consoleSearchBar.Location = new Point(3, 3);
            consoleSearchBar.Name = "consoleSearchBar";
            consoleSearchBar.Size = new Size(359, 25);
            consoleSearchBar.TabIndex = 1;
            consoleSearchBar.SearchTextChanged += consoleSearchBar_SearchTextChanged;
            consoleSearchBar.Load += consoleSearchBar_Load;
            // 
            // tabControl4
            // 
            tabControl4.Controls.Add(tabPage1);
            tabControl4.Dock = DockStyle.Fill;
            tabControl4.Location = new Point(0, 0);
            tabControl4.Name = "tabControl4";
            tabControl4.SelectedIndex = 0;
            tabControl4.Size = new Size(428, 244);
            tabControl4.TabIndex = 2;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(ProjectFolderTreeView);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(420, 216);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Project Folder";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // ProjectFolderTreeView
            // 
            ProjectFolderTreeView.AllowDrop = true;
            ProjectFolderTreeView.Dock = DockStyle.Fill;
            ProjectFolderTreeView.HotTracking = true;
            ProjectFolderTreeView.LabelEdit = true;
            ProjectFolderTreeView.Location = new Point(3, 3);
            ProjectFolderTreeView.Name = "ProjectFolderTreeView";
            ProjectFolderTreeView.Size = new Size(414, 210);
            ProjectFolderTreeView.TabIndex = 1;
            // 
            // panel2
            // 
            panel2.Controls.Add(SceneHierarchyPanel);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(0, 0);
            panel2.Name = "panel2";
            panel2.Size = new Size(193, 681);
            panel2.TabIndex = 5;
            // 
            // panel3
            // 
            panel3.Controls.Add(PropertiesWindow);
            panel3.Dock = DockStyle.Fill;
            panel3.Location = new Point(0, 0);
            panel3.Name = "panel3";
            panel3.Size = new Size(258, 681);
            panel3.TabIndex = 6;
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.Location = new Point(0, 0);
            splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(panel2);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(splitContainer3);
            splitContainer2.Size = new Size(1264, 681);
            splitContainer2.SplitterDistance = 193;
            splitContainer2.TabIndex = 5;
            // 
            // splitContainer3
            // 
            splitContainer3.Dock = DockStyle.Fill;
            splitContainer3.Location = new Point(0, 0);
            splitContainer3.Name = "splitContainer3";
            // 
            // splitContainer3.Panel1
            // 
            splitContainer3.Panel1.Controls.Add(panel1);
            // 
            // splitContainer3.Panel2
            // 
            splitContainer3.Panel2.Controls.Add(panel3);
            splitContainer3.Size = new Size(1067, 681);
            splitContainer3.SplitterDistance = 805;
            splitContainer3.TabIndex = 0;
            // 
            // splitContainer4
            // 
            splitContainer4.Dock = DockStyle.Fill;
            splitContainer4.Location = new Point(0, 24);
            splitContainer4.Name = "splitContainer4";
            splitContainer4.Orientation = Orientation.Horizontal;
            // 
            // splitContainer4.Panel1
            // 
            splitContainer4.Panel1.Controls.Add(tabControl1);
            // 
            // splitContainer4.Panel2
            // 
            splitContainer4.Panel2.Controls.Add(splitContainer1);
            splitContainer4.Size = new Size(805, 657);
            splitContainer4.SplitterDistance = 409;
            splitContainer4.TabIndex = 6;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            ClientSize = new Size(1264, 681);
            Controls.Add(splitContainer2);
            Icon = (Icon) resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Margin = new Padding(3, 2, 3, 2);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form1";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            SceneHierarchyPanel.ResumeLayout(false);
            tabControl3.ResumeLayout(false);
            SceneHierarchyTab.ResumeLayout(false);
            SceneHierarchyTab.PerformLayout();
            HierarchyToolStrip.ResumeLayout(false);
            HierarchyToolStrip.PerformLayout();
            PropertiesWindow.ResumeLayout(false);
            PropertiesWindow.PerformLayout();
            propertiesToolStrip.ResumeLayout(false);
            propertiesToolStrip.PerformLayout();
            tabControl1.ResumeLayout(false);
            SceneView.ResumeLayout(false);
            SceneToolStrip.ResumeLayout(false);
            SceneToolStrip.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            tabControl2.ResumeLayout(false);
            ConsoleTabPage.ResumeLayout(false);
            tabControl4.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            splitContainer3.Panel1.ResumeLayout(false);
            splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) splitContainer3).EndInit();
            splitContainer3.ResumeLayout(false);
            splitContainer4.Panel1.ResumeLayout(false);
            splitContainer4.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) splitContainer4).EndInit();
            splitContainer4.ResumeLayout(false);
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
        public TreeView SceneHierarchyTreeView;
        private Editor.TextSearchBarControl SceneHierarchySearchBar;
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
        private TabControl tabControl3;
        private TabPage SceneHierarchyTab;
        private TabPage ManagersTab;
        private TabPage SystemsTab;
        private Panel panel2;
        private Panel panel3;
        private ToolStripProgressBar progressBar;
        private ToolStripTextBox progressBarTextBox;
        private SplitContainer splitContainer1;
        private TabControl tabControl4;
        private TabPage tabPage1;
        private TreeView ProjectFolderTreeView;
        private TabControl tabControl2;
        private TabPage ConsoleTabPage;
        private RichTextBox ConsoleTextBox;
        private Editor.TextSearchBarControl consoleSearchBar;
        private SplitContainer splitContainer2;
        private SplitContainer splitContainer3;
        private SplitContainer splitContainer4;
    }
}