namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void menuToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void spriteEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void SceneHierarchySearchBar_SearchTextChanged(object sender, string filterText)
        {
            if(string.IsNullOrEmpty(filterText))
            {
                // Reset node font colors and expand configurations back to normal
                ResetTreeNodes(SceneHierarchyTreeView.Nodes);

                return;
            }

            SceneHierarchyTreeView.BeginUpdate();

            // Call the recursive filter method we discussed previously, 
            // passing the clean string directly from the control payload
            FilterTreeNodes(SceneHierarchyTreeView.Nodes, filterText);

            SceneHierarchyTreeView.EndUpdate();
        }


        private void ResetTreeNodes(TreeNodeCollection nodes)
        {
            foreach(TreeNode node in nodes)
            {
                node.ForeColor = SystemColors.WindowText; // Restore default system color
                node.Collapse(); // Optional: Collapse everything back to a clean state

                // Keep digging down into nested nodes
                ResetTreeNodes(node.Nodes);
            }
        }

        private bool FilterTreeNodes(TreeNodeCollection nodes, string filter)
        {
            bool anyChildVisible = false;

            foreach(TreeNode node in nodes)
            {
                // 1. Deep Dive: Always check children first (e.g., check Components inside a GameObject)
                bool isChildVisible = FilterTreeNodes(node.Nodes, filter);

                // 2. Evaluation: Does this specific entity or property name match our search string?
                bool isCurrentMatch = node.Text.Contains(filter, StringComparison.CurrentCultureIgnoreCase);

                // 3. Execution Action: If this item matches, or any of its children match, keep it visible
                if(isCurrentMatch || isChildVisible)
                {
                    node.ForeColor = SystemColors.WindowText; // Standard readable text color

                    // If a child matched, we MUST expand the parent node so the user can see it nested!
                    if(isChildVisible)
                    {
                        node.Expand();
                    }

                    anyChildVisible = true;
                }
                else
                {
                    // If nothing matches, collapse it out of view or gray it out. 
                    // In basic WinForms handling, graying it out is highly readable:
                    node.ForeColor = SystemColors.GrayText;
                    node.Collapse();
                }
            }
            return anyChildVisible;

        }

        private void ProjectFolderSearchBar_SearchTextChanged(object sender, string filterText)
        {
            if(string.IsNullOrEmpty(filterText))
            {
                // Reset node font colors and expand configurations back to normal
                ResetTreeNodes(ProjectFolderTreeView.Nodes);

                return;
            }

            ProjectFolderTreeView.BeginUpdate();

            // Call the recursive filter method we discussed previously, 
            // passing the clean string directly from the control payload
            FilterTreeNodes(ProjectFolderTreeView.Nodes, filterText);

            ProjectFolderTreeView.EndUpdate();
        }

        private void componentToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}