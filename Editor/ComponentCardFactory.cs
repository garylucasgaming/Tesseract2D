using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices; // Required for ConditionalWeakTable
using System.Windows.Forms;

namespace Engine.Editor.WinFormsApp1
{
    using Engine.Editor.Theming;
    using global::WinFormsApp1;

    public static class ComponentCardFactory
    {
        private class CardState
        {
            public bool IsExpanded { get; set; } = true;
        }

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
       
        public static string? SelectedPropertyName
        {
            get; private set;
        }

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

        public static Panel CreateCard(string componentName, object componentInstance, int defaultWidth = 260, object? previouslySelectedInstance = null)
        {
            int headerHeight = 26;

            var filteredWrapper = new FilteredPropertyWrapper(componentInstance);
            int gridHeight = CalculateGridHeight(filteredWrapper);

            var state = _cardStates.GetValue(componentInstance, _ => new CardState());

            // 💡 FIX: Keep explicit Width, DO NOT use Anchor in FlowLayoutPanel
            Panel cardPanel = new Panel()
            {
                Width = defaultWidth > 50 ? defaultWidth : 260,
                Height = state.IsExpanded ? (headerHeight + gridHeight) : headerHeight,
                Margin = new Padding(3, 3, 3, 6),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = DarkBodyColor
            };

            string cleanName = componentName.Replace("Component", "");
            Label headerLabel = new Label()
            {
                Text = state.IsExpanded ? $"  ▼  {cleanName}" : $"  ►  {cleanName}",
                Height = headerHeight,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = DarkHeaderColor,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Top
            };

            PropertyGrid propGrid = new PropertyGrid()
            {
                Height = gridHeight,
                SelectedObject = filteredWrapper,
                ToolbarVisible = false,
                HelpVisible = false,
                PropertySort = PropertySort.Categorized,
                Tag = componentInstance,
                Visible = state.IsExpanded,
                Dock = DockStyle.Fill
            };
            
            ConfigurePropertyGridDropdowns(propGrid);

            // 💡 Responsive Auto-Resize logic safe for FlowLayoutPanel
            cardPanel.ParentChanged += (s, e) =>
            {
                if(cardPanel.Parent is Control parent)
                {
                    EventHandler resizeHandler = (src, args) =>
                    {
                        if(parent.IsDisposed || cardPanel.IsDisposed)
                            return;

                        int availableWidth = parent.ClientSize.Width - cardPanel.Margin.Left - cardPanel.Margin.Right;

                        if(parent is ScrollableControl scrollable && scrollable.VerticalScroll.Visible)
                        {
                            availableWidth -= SystemInformation.VerticalScrollBarWidth;
                        }

                        // Ensure we only apply valid non-zero widths
                        if(availableWidth > 80 && cardPanel.Width != availableWidth)
                        {
                            cardPanel.Width = availableWidth;
                        }
                    };

                    // Re-bind to avoid duplicate handlers
                    parent.Resize -= resizeHandler;
                    parent.Resize += resizeHandler;

                    // Trigger resize once parent layout initializes
                    if(parent.IsHandleCreated)
                    {
                        parent.BeginInvoke(resizeHandler);
                    }
                }
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

                // Capture currently selected property from this card's property grid
                SelectedPropertyName = propGrid.SelectedGridItem?.PropertyDescriptor?.Name ?? propGrid.SelectedGridItem?.Label;
            };

            if(componentInstance == previouslySelectedInstance)
            {
                markAsSelected();
            }

            headerLabel.Click += (s, e) =>
            {
                markAsSelected();

                propGrid.Visible = !propGrid.Visible;
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
            propGrid.SelectedGridItemChanged += (s, e) =>
            {
                markAsSelected();
                if(e.NewSelection?.PropertyDescriptor != null)
                {
                    SelectedPropertyName = e.NewSelection.PropertyDescriptor.Name;
                }
                else if(e.NewSelection != null)
                {
                    SelectedPropertyName = e.NewSelection.Label;
                }
            };

            cardPanel.Controls.Add(propGrid);
            cardPanel.Controls.Add(headerLabel);

            return cardPanel;
        }

        private static void ConfigurePropertyGridDropdowns(PropertyGrid propGrid)
        {
            object? gridView = propGrid.GetType()
                .GetField("gridView", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(propGrid);

            if(gridView == null)
                return;

            // Splitter alignment
            MethodInfo? moveSplitterMethod = gridView.GetType()
                .GetMethod("MoveSplitterTo", BindingFlags.NonPublic | BindingFlags.Instance);

            Action adjustSplitter = () =>
            {
                try
                {
                    if(propGrid.Width > 50 && moveSplitterMethod != null)
                    {
                        int labelWidth = (int) (propGrid.Width * 0.38);
                        moveSplitterMethod.Invoke(gridView, new object[] { labelWidth });
                    }
                }
                catch { }
            };

            propGrid.Resize += (s, e) => adjustSplitter();
            propGrid.HandleCreated += (s, e) => propGrid.BeginInvoke(adjustSplitter);

            // Fetch active cell editor and dropdown popup holder
            FieldInfo? editField = gridView.GetType().GetField("edit", BindingFlags.NonPublic | BindingFlags.Instance);
            Control? gridEdit = editField?.GetValue(gridView) as Control;

            FieldInfo? dropDownHolderField = gridView.GetType().GetField("dropDownHolder", BindingFlags.NonPublic | BindingFlags.Instance);
            Control? dropDownHolder = dropDownHolderField?.GetValue(gridView) as Control;

            if(dropDownHolder != null)
            {
                EventHandler? activeTextChangedHandler = null;
                KeyEventHandler? activeKeyDownHandler = null;

                dropDownHolder.VisibleChanged += (s, e) =>
                {
                    if(dropDownHolder.Visible)
                    {
                        ListBox? listBox = FindChildControl<ListBox>(dropDownHolder);
                        if(listBox != null)
                        {
                            // Store the complete original choice list
                            List<object> originalItems = listBox.Items.Cast<object>().ToList();
                            listBox.Tag = originalItems;

                            // Dynamic width sizing based on longest string
                            int maxTextWidth = 0;
                            using(Graphics g = listBox.CreateGraphics())
                            {
                                foreach(var item in originalItems)
                                {
                                    if(item == null)
                                        continue;
                                    int textWidth = (int) g.MeasureString(item.ToString(), listBox.Font).Width;
                                    if(textWidth > maxTextWidth)
                                        maxTextWidth = textWidth;
                                }
                            }
                            int requiredWidth = Math.Max(maxTextWidth + SystemInformation.VerticalScrollBarWidth + 24, 160);
                            if(requiredWidth > dropDownHolder.Width)
                            {
                                dropDownHolder.Width = requiredWidth;
                            }

                            if(gridEdit != null)
                            {
                                // Unbind previous events to avoid double firing
                                if(activeTextChangedHandler != null)
                                    gridEdit.TextChanged -= activeTextChangedHandler;
                                if(activeKeyDownHandler != null)
                                    gridEdit.KeyDown -= activeKeyDownHandler;

                                // LIVE FILTER: Filter ListBox items in real-time as user types in the cell
                                activeTextChangedHandler = (src, args) =>
                                {
                                    if(!dropDownHolder.Visible)
                                        return;

                                    string filter = gridEdit.Text.Trim();
                                    var fullList = listBox.Tag as List<object>;
                                    if(fullList == null)
                                        return;

                                    listBox.BeginUpdate();
                                    listBox.Items.Clear();
                                    foreach(var item in fullList)
                                    {
                                        if(item == null)
                                            continue;
                                        string itemText = item.ToString() ?? "";
                                        if(string.IsNullOrEmpty(filter) || itemText.Contains(filter, StringComparison.OrdinalIgnoreCase))
                                        {
                                            listBox.Items.Add(item);
                                        }
                                    }

                                    if(listBox.Items.Count > 0)
                                    {
                                        listBox.SelectedIndex = 0;
                                    }
                                    listBox.EndUpdate();
                                };

                                // Keyboard controls while typing inside the cell
                                activeKeyDownHandler = (src, keyArgs) =>
                                {
                                    if(!dropDownHolder.Visible)
                                        return;

                                    if(keyArgs.KeyCode == Keys.Down)
                                    {
                                        listBox.Focus();
                                        if(listBox.Items.Count > 0 && listBox.SelectedIndex < 0)
                                            listBox.SelectedIndex = 0;
                                        keyArgs.Handled = true;
                                    }
                                    else if(keyArgs.KeyCode == Keys.Escape)
                                    {
                                        dropDownHolder.Visible = false;
                                        keyArgs.Handled = true;
                                    }
                                };

                                gridEdit.TextChanged += activeTextChangedHandler;
                                gridEdit.KeyDown += activeKeyDownHandler;
                            }
                        }
                    }
                };
            }
        }

        private static T? FindChildControl<T>(Control parent) where T : Control
        {
            foreach(Control child in parent.Controls)
            {
                if(child is T typedChild)
                    return typedChild;

                var nested = FindChildControl<T>(child);
                if(nested != null)
                    return nested;
            }
            return null;
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