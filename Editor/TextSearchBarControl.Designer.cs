namespace Editor
{
    partial class TextSearchBarControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            textSearchInput = new TextBox();
            SuspendLayout();
            // 
            // textSearchInput
            // 
            textSearchInput.BackColor = SystemColors.Control;
            textSearchInput.Dock = DockStyle.Fill;
            textSearchInput.ForeColor = SystemColors.ControlText;
            textSearchInput.Location = new Point(0, 0);
            textSearchInput.Name = "textSearchInput";
            textSearchInput.Size = new Size(150, 23);
            textSearchInput.TabIndex = 0;
            // 
            // TextSearchBarControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            Controls.Add(textSearchInput);
            Name = "TextSearchBarControl";
            Size = new Size(150, 25);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textSearchInput;
    }
}
