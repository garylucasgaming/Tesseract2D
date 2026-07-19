namespace Engine.Editor
{
    partial class DatabaseViewer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if(disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DatabaseViewer));
            DatabaseToolStrip = new ToolStrip();
            DatabaseToolstripLabel = new ToolStripLabel();
            DatabaseToolStripComboBox = new ToolStripComboBox();
            SaveDatabaseButton = new ToolStripButton();
            NewDatabaseButton = new ToolStripButton();
            panel1 = new Panel();
            splitContainer1 = new SplitContainer();
            DatabaseGridView = new DataGridView();
            toolStrip1 = new ToolStrip();
            AddRowButton = new ToolStripButton();
            DeleteRowButton = new ToolStripButton();
            DatabasePropertyGrid = new PropertyGrid();
            DatabaseToolStrip.SuspendLayout();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) DatabaseGridView).BeginInit();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // DatabaseToolStrip
            // 
            DatabaseToolStrip.Items.AddRange(new ToolStripItem[] { DatabaseToolstripLabel, DatabaseToolStripComboBox, SaveDatabaseButton, NewDatabaseButton });
            DatabaseToolStrip.Location = new Point(0, 0);
            DatabaseToolStrip.Name = "DatabaseToolStrip";
            DatabaseToolStrip.Size = new Size(800, 25);
            DatabaseToolStrip.TabIndex = 0;
            DatabaseToolStrip.Text = "DatabaseToolstrip";
            // 
            // DatabaseToolstripLabel
            // 
            DatabaseToolstripLabel.Name = "DatabaseToolstripLabel";
            DatabaseToolstripLabel.Size = new Size(87, 22);
            DatabaseToolstripLabel.Text = "DatabaseName";
            // 
            // DatabaseToolStripComboBox
            // 
            DatabaseToolStripComboBox.Name = "DatabaseToolStripComboBox";
            DatabaseToolStripComboBox.Size = new Size(121, 25);
            // 
            // SaveDatabaseButton
            // 
            SaveDatabaseButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            SaveDatabaseButton.Image = (Image) resources.GetObject("SaveDatabaseButton.Image");
            SaveDatabaseButton.ImageTransparentColor = Color.Magenta;
            SaveDatabaseButton.Name = "SaveDatabaseButton";
            SaveDatabaseButton.Size = new Size(23, 22);
            SaveDatabaseButton.Text = "SaveDatabase";
            SaveDatabaseButton.Click += btnSaveDatabase_Click;
            // 
            // NewDatabaseButton
            // 
            NewDatabaseButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            NewDatabaseButton.Image = (Image) resources.GetObject("NewDatabaseButton.Image");
            NewDatabaseButton.ImageTransparentColor = Color.Magenta;
            NewDatabaseButton.Name = "NewDatabaseButton";
            NewDatabaseButton.Size = new Size(23, 22);
            NewDatabaseButton.Text = "NewDatabase";
            NewDatabaseButton.Click += btnNewDatabase_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(splitContainer1);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 25);
            panel1.Name = "panel1";
            panel1.Size = new Size(800, 425);
            panel1.TabIndex = 1;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(DatabaseGridView);
            splitContainer1.Panel1.Controls.Add(toolStrip1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(DatabasePropertyGrid);
            splitContainer1.Size = new Size(800, 425);
            splitContainer1.SplitterDistance = 592;
            splitContainer1.TabIndex = 0;
            // 
            // DatabaseGridView
            // 
            DatabaseGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            DatabaseGridView.Dock = DockStyle.Fill;
            DatabaseGridView.Location = new Point(32, 0);
            DatabaseGridView.Name = "DatabaseGridView";
            DatabaseGridView.Size = new Size(560, 425);
            DatabaseGridView.TabIndex = 0;
            // 
            // toolStrip1
            // 
            toolStrip1.Dock = DockStyle.Left;
            toolStrip1.Items.AddRange(new ToolStripItem[] { AddRowButton, DeleteRowButton });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(32, 425);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // AddRowButton
            // 
            AddRowButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            AddRowButton.Image = (Image) resources.GetObject("AddRowButton.Image");
            AddRowButton.ImageTransparentColor = Color.Magenta;
            AddRowButton.Name = "AddRowButton";
            AddRowButton.Size = new Size(29, 20);
            AddRowButton.Text = "AddRow";
            AddRowButton.Click += btnAddRow_Click;
            // 
            // DeleteRowButton
            // 
            DeleteRowButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            DeleteRowButton.Image = (Image) resources.GetObject("DeleteRowButton.Image");
            DeleteRowButton.ImageTransparentColor = Color.Magenta;
            DeleteRowButton.Name = "DeleteRowButton";
            DeleteRowButton.Size = new Size(29, 20);
            DeleteRowButton.Text = "DeleteRow";
            DeleteRowButton.Click += btnRemoveRow_Click;
            // 
            // DatabasePropertyGrid
            // 
            DatabasePropertyGrid.Dock = DockStyle.Fill;
            DatabasePropertyGrid.Location = new Point(0, 0);
            DatabasePropertyGrid.Name = "DatabasePropertyGrid";
            DatabasePropertyGrid.Size = new Size(204, 425);
            DatabasePropertyGrid.TabIndex = 0;
            // 
            // DatabaseViewer
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(panel1);
            Controls.Add(DatabaseToolStrip);
            Name = "DatabaseViewer";
            Text = "DatabaseViewer";
            DatabaseToolStrip.ResumeLayout(false);
            DatabaseToolStrip.PerformLayout();
            panel1.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) DatabaseGridView).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStrip DatabaseToolStrip;
        private Panel panel1;
        private SplitContainer splitContainer1;
        private DataGridView DatabaseGridView;
        private ToolStripLabel DatabaseToolstripLabel;
        private ToolStripComboBox DatabaseToolStripComboBox;
        private ToolStripButton SaveDatabaseButton;
        private ToolStripButton NewDatabaseButton;
        private PropertyGrid DatabasePropertyGrid;
        private ToolStrip toolStrip1;
        private ToolStripButton AddRowButton;
        private ToolStripButton DeleteRowButton;
    }
}