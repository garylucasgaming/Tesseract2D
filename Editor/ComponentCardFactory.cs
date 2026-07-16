using System;
using System.Drawing;
using System.Runtime.CompilerServices; // Required for ConditionalWeakTable
using System.Windows.Forms;

namespace Engine.Editor.WinFormsApp1
{
    using global::WinFormsApp1;

    public static class ComponentCardFactory
    {
        // 1. Define the state object
        private class CardState
        {
            public bool IsExpanded { get; set; } = true;
        }

        // 2. Thread-safe, leak-free weak lookup table for states
        private static readonly ConditionalWeakTable<object, CardState> _cardStates =
            new ConditionalWeakTable<object, CardState>();

        public static object? SelectedComponentInstance
        {
            get; private set;
        }
        private static Panel? _lastSelectedCardPanel = null;
        private static Label? _lastSelectedHeaderLabel = null;

        // Theme Definitions
        private static readonly Color DarkHeaderColor = Color.FromArgb(60, 60, 60);
        private static readonly Color DarkBodyColor = SystemColors.Control;
        private static readonly Color SelectedHeaderColor = Color.FromArgb(40, 100, 180);
        private static readonly Color SelectedBodyColor = Color.FromArgb(220, 230, 245);

        public static void ClearSelection()
        {
            SelectedComponentInstance = null;
            if(_lastSelectedCardPanel != null)
                _lastSelectedCardPanel.BackColor = DarkBodyColor;
            if(_lastSelectedHeaderLabel != null)
                _lastSelectedHeaderLabel.BackColor = DarkHeaderColor;
            _lastSelectedCardPanel = null;
            _lastSelectedHeaderLabel = null;
        }

        public static Panel CreateCard(string componentName, object componentInstance, int width, object? previouslySelectedInstance = null)
        {
            int headerHeight = 26;

            var filteredWrapper = new FilteredPropertyWrapper(componentInstance);
            int gridHeight = CalculateGridHeight(filteredWrapper);

            // 3. Fetch or automatically initialize the persisted state for this unique component
            var state = _cardStates.GetValue(componentInstance, _ => new CardState());

            Panel cardPanel = new Panel()
            {
                Width = width - 25,
                // Use state to set initial height
                Height = state.IsExpanded ? (headerHeight + gridHeight) : headerHeight,
                Margin = new Padding(5, 5, 5, 10),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = DarkBodyColor
            };

            string cleanName = componentName.Replace("Component", "");
            Label headerLabel = new Label()
            {
                // Use state to set initial arrow indicator
                Text = state.IsExpanded ? $"  ▼  {cleanName}" : $"  ►  {cleanName}",
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
                Tag = componentInstance,
                // Use state to set initial visibility
                Visible = state.IsExpanded
            };

            Action markAsSelected = () =>
            {
                if(_lastSelectedCardPanel != null && _lastSelectedCardPanel != cardPanel)
                {
                    _lastSelectedCardPanel.BackColor = DarkBodyColor;
                }
                if(_lastSelectedHeaderLabel != null && _lastSelectedHeaderLabel != headerLabel)
                {
                    _lastSelectedHeaderLabel.BackColor = DarkHeaderColor;
                }

                SelectedComponentInstance = componentInstance;
                _lastSelectedCardPanel = cardPanel;
                _lastSelectedHeaderLabel = headerLabel;

                cardPanel.BackColor = SelectedBodyColor;
                headerLabel.BackColor = SelectedHeaderColor;
            };

            // 4. If this component was previously selected before the rebuild, restore selection instantly!
            if(componentInstance == previouslySelectedInstance)
            {
                markAsSelected();
            }

            headerLabel.Click += (s, e) =>
            {
                markAsSelected();

                propGrid.Visible = !propGrid.Visible;

                // 5. Commit the expanded/collapsed state update back to our out-of-band cache
                state.IsExpanded = propGrid.Visible;

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