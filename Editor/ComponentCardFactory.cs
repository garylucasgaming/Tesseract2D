using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Editor
{
    using global::WinFormsApp1;
    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using WinFormsApp1;

    namespace WinFormsApp1
    {
        public static class ComponentCardFactory
        {
            public static Panel CreateCard(string componentName, object componentInstance, int width)
            {
                int headerHeight = 26;

                // Wrap the component instance to hide the junk
                var filteredWrapper = new FilteredPropertyWrapper(componentInstance);
                int gridHeight = CalculateGridHeight(filteredWrapper);

                Panel cardPanel = new Panel()
                {
                    Width = width - 25,
                    Height = headerHeight + gridHeight,
                    Margin = new Padding(5, 5, 5, 10),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = SystemColors.Control
                };

                Label headerLabel = new Label()
                {
                    Text = $" ■ {componentName.Replace("Component", "")}",
                    Location = new Point(0, 0),
                    Width = cardPanel.Width,
                    Height = headerHeight,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    BackColor = Color.FromArgb(60, 60, 60),
                    ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                PropertyGrid propGrid = new PropertyGrid()
                {
                    Location = new Point(0, headerHeight),
                    Width = cardPanel.Width,
                    Height = gridHeight,
                    // 👇 FIX: Pass the wrapped object so the grid hides blacklisted items
                    SelectedObject = filteredWrapper,
                    ToolbarVisible = false,
                    HelpVisible = false,
                    PropertySort = PropertySort.Categorized,
                    Tag = componentInstance // Store the original instance for reference

                };

                
                  cardPanel.Controls.Add(headerLabel);
                cardPanel.Controls.Add(propGrid);

                return cardPanel;
            }

            private static int CalculateGridHeight(FilteredPropertyWrapper wrappedTarget)
            {
                if(wrappedTarget == null)
                    return 45;

                // 👇 FIX: Query the wrapper directly for the clean, post-filtered count
                var properties = wrappedTarget.GetProperties();
                int rowCount = properties.Count;

                if(rowCount == 0)
                    return 45;

                return (rowCount * 22) + 25;
            }
        }
    }
}
