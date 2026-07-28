using Engine.Core.Serialization;
using Engine.Core.Utilities;
using Engine.Editor.WinFormsApp1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp1
{
    public partial class Form1
    {
        // --- Cascading Inspector Fields ---
        private System.Windows.Forms.ListBox ScreenControlsListBox;
        private System.Windows.Forms.ListBox ControlMembersListBox;
        private System.Windows.Forms.Button ConfirmEcsBindingButton;

        private void InitializeScreenBindingsInspector()
        {
            if(ScreenBindingsPanel != null)
            {
                ScreenBindingsPanel.Controls.Clear();

                // 3-Column Split Layout for Tiered Inspection
                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 3,
                    RowCount = 2,
                    ColumnStyles =
                    {
                        new ColumnStyle(SizeType.Percent, 33F),
                        new ColumnStyle(SizeType.Percent, 34F),
                        new ColumnStyle(SizeType.Percent, 33F)
                    },
                    RowStyles = { new RowStyle(SizeType.Percent, 85F), new RowStyle(SizeType.Percent, 15F) }
                };

                // Tier 1: Screen Controls (e.g., PercentBarInstance, MusicSlider)
                var col1Panel = new Panel { Dock = DockStyle.Fill };
                var col1Label = new Label { Text = "1. Screen Controls", Dock = DockStyle.Top, Height = 22, Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold) };
                ScreenControlsListBox = new ListBox { Dock = DockStyle.Fill };
                col1Panel.Controls.Add(ScreenControlsListBox);
                col1Panel.Controls.Add(col1Label);

                // Tier 2: Control Members (Properties, Methods, Events of selected control)
                var col2Panel = new Panel { Dock = DockStyle.Fill };
                var col2Label = new Label { Text = "2. Control Properties / Events", Dock = DockStyle.Top, Height = 22, Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold) };
                ControlMembersListBox = new ListBox { Dock = DockStyle.Fill };
                col2Panel.Controls.Add(ControlMembersListBox);
                col2Panel.Controls.Add(col2Label);

                // Tier 3: Target ECS Property (Placeholder for GameObject Component selection)
                var col3Panel = new Panel { Dock = DockStyle.Fill };
                var col3Label = new Label { Text = "3. Target ECS Property", Dock = DockStyle.Top, Height = 22, Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold) };
                var targetEcsInfoLabel = new Label { Text = "(Select target component property from inspector context)", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
                col3Panel.Controls.Add(targetEcsInfoLabel);
                col3Panel.Controls.Add(col3Label);

                layout.Controls.Add(col1Panel, 0, 0);
                layout.Controls.Add(col2Panel, 1, 0);
                layout.Controls.Add(col3Panel, 2, 0);

                // Action Button spanning the bottom row
                ConfirmEcsBindingButton = new Button { Text = "Generate Live Data-Binding Link", Dock = DockStyle.Fill };
                ConfirmEcsBindingButton.Click += ConfirmEcsBindingButton_Click;
                layout.Controls.Add(ConfirmEcsBindingButton, 0, 1);
                layout.SetColumnSpan(ConfirmEcsBindingButton, 3);

                ScreenBindingsPanel.Controls.Add(layout);
            }

            // Tier 1 Selection Event: When a screen is chosen from the main list view
            if(UIListView != null)
            {
                UIListView.SelectedIndexChanged += (s, e) =>
                {
                    if(UIListView.SelectedItems.Count == 0)
                    {
                        ClearBindingsInspector();
                        return;
                    }
                    string selectedScreen = UIListView.SelectedItems[0].Text;
                    LoadScreenControlsViaReflection(selectedScreen);
                };
            }

            // Tier 2 Selection Event: When a control on the screen is clicked, inspect its type members
            if(ScreenControlsListBox != null)
            {
                ScreenControlsListBox.SelectedIndexChanged += (s, e) =>
                {
                    if(ScreenControlsListBox.SelectedItem == null)
                        return;

                    if(ScreenControlsListBox.SelectedItem is ControlMemberItem item)
                    {
                        InspectControlTypeMembers(item.MemberType);
                    }
                };
            }
        }

        // Helper wrapper to store type metadata in the list box
        private class ControlMemberItem
        {
            public string DisplayName
            {
                get; set;
            }
            public Type MemberType
            {
                get; set;
            }
            public override string ToString() => DisplayName;
        }

        private void LoadScreenControlsViaReflection(string screenName)
        {
            ScreenControlsListBox?.Items.Clear();
            ControlMembersListBox?.Items.Clear();

            if(!EditorContextManager.IsProjectLoaded)
                return;

            // Search for the screen class in the hot-loaded gameplay assembly or current domain
            Type screenType = _scriptManager.CurrentAssembly?.GetType(screenName)
                              ?? AppDomain.CurrentDomain.GetAssemblies()
                                  .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                                  .FirstOrDefault(t => t.Name == screenName);

            if(screenType == null)
            {
                Log.Warning($"[UI Editor] Could not resolve type for screen class: {screenName}. Ensure project is compiled.");
                return;
            }

            // Use Reflection to get all public properties defined on the generated screen class (e.g., PercentBarInstance, MusicSlider)
            var properties = screenType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach(var prop in properties)
            {
                ScreenControlsListBox.Items.Add(new ControlMemberItem
                {
                    DisplayName = $"{prop.Name} ({prop.PropertyType.Name})",
                    MemberType = prop.PropertyType
                });
            }

            Log.Info($"[UI Editor] Reflected {properties.Length} controls/properties for screen {screenName}");
        }

        private void InspectControlTypeMembers(Type controlType)
        {
            ControlMembersListBox?.Items.Clear();

            if(controlType == null)
                return;

            // 1. Get Public Properties (e.g., BarPercent, Value, Text)
            foreach(var prop in controlType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                ControlMembersListBox.Items.Add($"[Prop] {prop.Name} ({prop.PropertyType.Name})");
            }

            // 2. Get Public Methods
            foreach(var method in controlType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
            {
                if(method.IsSpecialName)
                    continue; // Skip property getters/setters
                ControlMembersListBox.Items.Add($"[Method] {method.Name}");
            }

            // 3. Get Events
            foreach(var ev in controlType.GetEvents(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                ControlMembersListBox.Items.Add($"[Event] {ev.Name}");
            }
        }

        private void ConfirmEcsBindingButton_Click(object sender, EventArgs e)
        {
            if(UIListView == null || UIListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a UI screen first.", "Binding Error");
                return;
            }

            string selectedScreen = UIListView.SelectedItems[0].Text;
            string selectedControl = ScreenControlsListBox?.SelectedItem?.ToString() ?? string.Empty;
            string selectedMember = ControlMembersListBox?.SelectedItem?.ToString() ?? string.Empty;

            // Query ComponentCardFactory for the active ECS target
            object targetComponent = ComponentCardFactory.SelectedComponentInstance;
            string targetProperty = ComponentCardFactory.SelectedPropertyName;

            // Validate selections
            if(string.IsNullOrEmpty(selectedControl) || string.IsNullOrEmpty(selectedMember))
            {
                MessageBox.Show("Please select both a screen control and a member (property/event) to bind.", "UI Selection Missing");
                return;
            }

            if(targetComponent == null || string.IsNullOrEmpty(targetProperty))
            {
                MessageBox.Show("Please select a target ECS Component card and click a specific property in the Scene Inspector.", "ECS Target Missing");
                return;
            }

            // Success: Both contexts are successfully acquired
            Log.Info($"[UI Editor] Generated Data-Binding Link:\n" +
                     $"  UI Screen: {selectedScreen}\n" +
                     $"  UI Member: {selectedControl} -> {selectedMember}\n" +
                     $"  ECS Target: {targetComponent.GetType().Name}.{targetProperty}");

            MessageBox.Show($"Successfully linked UI [{selectedMember}] to [{targetComponent.GetType().Name}.{targetProperty}]!", "ECS UI Linker Active");
        }

        private void ClearBindingsInspector()
        {
            ScreenControlsListBox?.Items.Clear();
            ControlMembersListBox?.Items.Clear();
        }
    }
}
