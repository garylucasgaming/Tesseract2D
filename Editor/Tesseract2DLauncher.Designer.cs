namespace Engine.Editor
{
    partial class Tesseract2DLauncher
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
            panel2 = new Panel();
            projectsFlowPanel = new FlowLayoutPanel();
            panel1 = new Panel();
            AddExistingProjectButton = new Button();
            NewProjectButton = new Button();
            label1 = new Label();
            panel2.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // panel2
            // 
            panel2.BackColor = Color.Transparent;
            panel2.Controls.Add(projectsFlowPanel);
            panel2.Controls.Add(panel1);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(0, 0);
            panel2.Name = "panel2";
            panel2.Size = new Size(800, 450);
            panel2.TabIndex = 1;
            // 
            // projectsFlowPanel
            // 
            projectsFlowPanel.Dock = DockStyle.Fill;
            projectsFlowPanel.Location = new Point(0, 100);
            projectsFlowPanel.Name = "projectsFlowPanel";
            projectsFlowPanel.Size = new Size(800, 350);
            projectsFlowPanel.TabIndex = 1;
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(  14,   27,   26);
            panel1.Controls.Add(AddExistingProjectButton);
            panel1.Controls.Add(NewProjectButton);
            panel1.Controls.Add(label1);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(800, 100);
            panel1.TabIndex = 0;
            // 
            // AddExistingProjectButton
            // 
            AddExistingProjectButton.Anchor =  AnchorStyles.Top | AnchorStyles.Right;
            AddExistingProjectButton.BackColor = Color.FromArgb(  194,   43,   238);
            AddExistingProjectButton.FlatAppearance.BorderSize = 0;
            AddExistingProjectButton.FlatStyle = FlatStyle.Popup;
            AddExistingProjectButton.Font = new Font("Arial", 9.75F, FontStyle.Bold, GraphicsUnit.Point,  0);
            AddExistingProjectButton.ForeColor = SystemColors.Control;
            AddExistingProjectButton.Location = new Point(533, 30);
            AddExistingProjectButton.Name = "AddExistingProjectButton";
            AddExistingProjectButton.Size = new Size(99, 35);
            AddExistingProjectButton.TabIndex = 2;
            AddExistingProjectButton.Text = "Add Existing";
            AddExistingProjectButton.UseVisualStyleBackColor = false;
            AddExistingProjectButton.Click += AddExistingProjectButton_Click;
            // 
            // NewProjectButton
            // 
            NewProjectButton.Anchor =  AnchorStyles.Top | AnchorStyles.Right;
            NewProjectButton.BackColor = Color.FromArgb(  23,   181,   149);
            NewProjectButton.FlatAppearance.BorderSize = 0;
            NewProjectButton.FlatStyle = FlatStyle.Popup;
            NewProjectButton.Font = new Font("Arial", 12F, FontStyle.Bold, GraphicsUnit.Point,  0);
            NewProjectButton.ForeColor = SystemColors.Control;
            NewProjectButton.Location = new Point(638, 30);
            NewProjectButton.Name = "NewProjectButton";
            NewProjectButton.Size = new Size(150, 35);
            NewProjectButton.TabIndex = 1;
            NewProjectButton.Text = "+ New Project";
            NewProjectButton.UseVisualStyleBackColor = false;
            NewProjectButton.Click += btnNewProject_Click;
            // 
            // label1
            // 
            label1.BackColor = Color.Transparent;
            label1.Dock = DockStyle.Left;
            label1.Font = new Font("Arial", 36F, FontStyle.Bold, GraphicsUnit.Point,  0);
            label1.ForeColor = SystemColors.Control;
            label1.Location = new Point(0, 0);
            label1.Name = "label1";
            label1.Size = new Size(217, 100);
            label1.TabIndex = 0;
            label1.Text = "Projects";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // Tesseract2DLauncher
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(  17,   34,   32);
            ClientSize = new Size(800, 450);
            Controls.Add(panel2);
            Name = "Tesseract2DLauncher";
            Text = "Tesseract2DLauncher";
            panel2.ResumeLayout(false);
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private Panel panel1;
        private Label label1;
        private Panel panel2;
        private Button NewProjectButton;
        private FlowLayoutPanel projectsFlowPanel;
        private Button AddExistingProjectButton;
    }
}