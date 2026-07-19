using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Engine.Editor
{
    public class DatabaseDialog : Form
    {
        private TextBox txtName;
        private ComboBox cmbTypes;
        private Button btnOk;
        private Button btnCancel;

        public string DatabaseFileName => txtName.Text.Trim();
        public Type SelectedComponentType => (Type) cmbTypes.SelectedItem;

        public DatabaseDialog(List<Type> availableTypes)
        {
            InitializeComponent(availableTypes);
        }

        private void InitializeComponent(List<Type> availableTypes)
        {
            this.Text = "Create New Asset Database";
            this.Size = new System.Drawing.Size(320, 240); // Made slightly taller to accommodate safe padding
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Row 1: Name Section (Expanded vertical gap to 25px to survive High-DPI text scaling)
            Label lblName = new Label { Text = "Database File Name:", Location = new System.Drawing.Point(20, 15), AutoSize = true };
            txtName = new TextBox { Location = new System.Drawing.Point(20, 40), Width = 260 };

            // Row 2: Type Section (Spaced comfortably below the text box bounds)
            Label lblType = new Label { Text = "Target Data Component Type:", Location = new System.Drawing.Point(20, 85), AutoSize = true };
            cmbTypes = new ComboBox { Location = new System.Drawing.Point(20, 110), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };

            cmbTypes.DisplayMember = "Name";
            foreach(var type in availableTypes)
            {
                cmbTypes.Items.Add(type);
            }
            if(cmbTypes.Items.Count > 0)
                cmbTypes.SelectedIndex = 0;

            // Row 3: Action Buttons (Grounded neatly near the bottom of the canvas)
            btnOk = new Button { Text = "Create", Location = new System.Drawing.Point(115, 160), DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Cancel", Location = new System.Drawing.Point(200, 160), DialogResult = DialogResult.Cancel };

            this.Controls.AddRange(new Control[] { lblName, txtName, lblType, cmbTypes, btnOk, btnCancel });
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}