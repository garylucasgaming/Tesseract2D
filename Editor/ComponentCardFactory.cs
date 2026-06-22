using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Editor
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    namespace WinFormsApp1
    {
        public static class ComponentCardFactory
        {
            /// <summary>
            /// Creates a styled visual container housing a header banner and an isolated PropertyGrid for a specific component.
            /// </summary>
            public static Panel CreateCard(string componentName, object componentInstance, int width)
            {
                int headerHeight = 26;
                int gridHeight = CalculateGridHeight(componentInstance);

                // 1. Create the main card frame panel with an explicit total height
                Panel cardPanel = new Panel()
                {
                    Width = width - 25,                 // Safety buffer for the FlowLayoutPanel's vertical scrollbar
                    Height = headerHeight + gridHeight,  // Force the card frame open so it cannot collapse to a line
                    Margin = new Padding(5, 5, 5, 10),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = SystemColors.Control
                };

                // 2. Create the Header Banner UI
                Label headerLabel = new Label()
                {
                    Text = $" ■ {componentName.Replace("Component", "")}",
                    Location = new Point(0, 0),         // Use absolute positioning to bypass docking calculations
                    Width = cardPanel.Width,
                    Height = headerHeight,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    BackColor = Color.FromArgb(60, 60, 60),
                    ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                // 3. Create the dedicated inner PropertyGrid
                PropertyGrid propGrid = new PropertyGrid()
                {
                    Location = new Point(0, headerHeight), // Sit perfectly beneath the header banner
                    Width = cardPanel.Width,
                    Height = gridHeight,
                    SelectedObject = componentInstance,
                    ToolbarVisible = false,     // Hide the redundant ABC/Category top icons
                    HelpVisible = false,        // Hide the description box to keep the layout tight
                    PropertySort = PropertySort.Categorized
                };

                // Add controls directly to the frame panel
                cardPanel.Controls.Add(headerLabel);
                cardPanel.Controls.Add(propGrid);

                return cardPanel;
            }

            /// <summary>
            /// Estimates the required height for the property grid based on how many fields the component exposes.
            /// </summary>
            private static int CalculateGridHeight(object target)
            {
                if(target == null)
                    return 45;
                var properties = System.ComponentModel.TypeDescriptor.GetProperties(target);

                int rowCount = 0;
                foreach(System.ComponentModel.PropertyDescriptor prop in properties)
                {
                    // Skip internal engine properties or pointers to prevent visual pollution
                    if(prop.IsReadOnly || prop.Name == "ParentTransform" || prop.Name == "ContextScene")
                        continue;
                    rowCount++;
                }

                // A fallback minimal height buffer if a component has no fields yet
                if(rowCount == 0)
                    return 45;

                // Standard row height in a modern WinForms grid environment is roughly 22 pixels per entry, plus margins
                return (rowCount * 22) + 25;
            }
        }
    }
}
