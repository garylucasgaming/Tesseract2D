using System;
using System.Drawing;
using System.Windows.Forms;

namespace Engine.Editor.WinFormsApp1
{
    using global::WinFormsApp1;

    public static class ComponentCardFactory
    {
        public static object? SelectedComponentInstance
        {
            get; private set;
        }
        private static Panel? _lastSelectedCardPanel = null;
        private static Label? _lastSelectedHeaderLabel = null;

        // Theme Definitions (Dark Theme Baseline with an Electric Blue Selection Pop)
        private static readonly Color DarkHeaderColor = Color.FromArgb(60, 60, 60);
        private static readonly Color DarkBodyColor = SystemColors.Control;

        // 💡 Visual Selection Tints
        private static readonly Color SelectedHeaderColor = Color.FromArgb(40, 100, 180); // Distinct Electric Blue
        private static readonly Color SelectedBodyColor = Color.FromArgb(220, 230, 245);   // Soft Ice Blue Accent

        public static void ClearSelection()
        {
            SelectedComponentInstance = null;

            // Reset visual states cleanly back to default
            if(_lastSelectedCardPanel != null)
                _lastSelectedCardPanel.BackColor = DarkBodyColor;
            if(_lastSelectedHeaderLabel != null)
                _lastSelectedHeaderLabel.BackColor = DarkHeaderColor;

            _lastSelectedCardPanel = null;
            _lastSelectedHeaderLabel = null;
        }

        public static Panel CreateCard(string componentName, object componentInstance, int width)
        {
            int headerHeight = 26;

            var filteredWrapper = new FilteredPropertyWrapper(componentInstance);
            int gridHeight = CalculateGridHeight(filteredWrapper);

            Panel cardPanel = new Panel()
            {
                Width = width - 25,
                Height = headerHeight + gridHeight,
                Margin = new Padding(5, 5, 5, 10),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = DarkBodyColor
            };

            string cleanName = componentName.Replace("Component", "");
            Label headerLabel = new Label()
            {
                Text = $"  ▼  {cleanName}",
                Location = new Point(0, 0),
                Width = cardPanel.Width,
                Height = headerHeight,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = DarkHeaderColor,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };

            PropertyGrid propGrid = new PropertyGrid()
            {
                Location = new Point(0, headerHeight),
                Width = cardPanel.Width,
                Height = gridHeight,
                SelectedObject = filteredWrapper,
                ToolbarVisible = false,
                HelpVisible = false,
                PropertySort = PropertySort.Categorized,
                Tag = componentInstance
            };

            // 💡 REPAINT SELECTION ENGINE
            Action markAsSelected = () =>
            {
                // 1. Revert the last active card back to its boring dark theme
                if(_lastSelectedCardPanel != null && _lastSelectedCardPanel != cardPanel)
                {
                    _lastSelectedCardPanel.BackColor = DarkBodyColor;
                }
                if(_lastSelectedHeaderLabel != null && _lastSelectedHeaderLabel != headerLabel)
                {
                    _lastSelectedHeaderLabel.BackColor = DarkHeaderColor;
                }

                // 2. Assign current active tracking fields
                SelectedComponentInstance = componentInstance;
                _lastSelectedCardPanel = cardPanel;
                _lastSelectedHeaderLabel = headerLabel;

                // 3. Inject vibrant selection colors to let the user know what's targeted
                cardPanel.BackColor = SelectedBodyColor;
                headerLabel.BackColor = SelectedHeaderColor;
            };

            // Hook header label click to select AND toggle expand state
            headerLabel.Click += (s, e) =>
            {
                markAsSelected();

                propGrid.Visible = !propGrid.Visible;
                if(propGrid.Visible)
                {
                    cardPanel.Height = headerHeight + gridHeight;
                    headerLabel.Text = $"  ▼  {cleanName}";
                }
                else
                {
                    cardPanel.Height = headerHeight;
                    headerLabel.Text = $"  ►  {cleanName}";
                }
            };

            // 💡 Crucial: If they tweak an attribute or select a row cell inside the PropertyGrid, 
            // trigger the exact same visual highlight state!
            propGrid.GotFocus += (s, e) => markAsSelected();
            propGrid.SelectedGridItemChanged += (s, e) => markAsSelected();

            cardPanel.Controls.Add(headerLabel);
            cardPanel.Controls.Add(propGrid);

            return cardPanel;
        }

        private static int CalculateGridHeight(FilteredPropertyWrapper wrappedTarget)
        {
            if(wrappedTarget == null)
                return 45;

            var properties = wrappedTarget.GetProperties();
            int rowCount = properties.Count;

            if(rowCount == 0)
                return 45;

            return (rowCount * 22) + 25;
        }
    }
}