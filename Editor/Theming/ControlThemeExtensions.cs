using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Editor.Theming
{
    public static class ControlThemeExtensions
    {
        public static void ApplySynthwaveTheme(this Control control)
        {
            control.BackColor = SynthwaveTheme.BackgroundDark;
            control.ForeColor = SynthwaveTheme.TextPrimary;



            foreach(Control child in control.Controls)
            {
                if(child is Panel || child is GroupBox || child is FlowLayoutPanel || child is SplitContainer)
                {
                    
                    child.BackColor = SynthwaveTheme.SurfaceDark;
                    child.ForeColor = SynthwaveTheme.TextPrimary;
                    
                }
                else if(child is Button btn)
                {
                    btn.BackColor = SynthwaveTheme.SurfaceLight;
                    btn.ForeColor = SynthwaveTheme.NeonCyan;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderColor = SynthwaveTheme.NeonCyan;
                }
                else if(child is PropertyGrid propGrid)
                {
                    propGrid.BackColor = SynthwaveTheme.BackgroundDark;
                    propGrid.ViewBackColor = SynthwaveTheme.BackgroundDark;
                    propGrid.ViewForeColor = SynthwaveTheme.TextPrimary;
                    propGrid.CategoryForeColor = SynthwaveTheme.NeonCyan;
                    propGrid.LineColor = SynthwaveTheme.SurfaceDark;
                }
                else if(child is DataGridView dgv)
                {
                    dgv.BackgroundColor = SynthwaveTheme.BackgroundDark;
                    dgv.GridColor = SynthwaveTheme.SurfaceDark;
                    dgv.DefaultCellStyle.BackColor = SynthwaveTheme.SurfaceDark;
                    dgv.DefaultCellStyle.ForeColor = SynthwaveTheme.TextPrimary;
                    dgv.ColumnHeadersDefaultCellStyle.BackColor = SynthwaveTheme.SurfaceLight;
                    dgv.ColumnHeadersDefaultCellStyle.ForeColor = SynthwaveTheme.NeonPink;
                }
                else if(child is ListView l)
                {
                    l.BackColor = SynthwaveTheme.BackgroundDark;
                    l.ForeColor = SynthwaveTheme.TextPrimary;

                }
                else if(child is TreeView tv)
                {
                    tv.BackColor = SynthwaveTheme.BackgroundDark;
                    tv.ForeColor = SynthwaveTheme.TextPrimary;

                }
                else if(child is Label lbl)
                {
                    lbl.BackColor = SynthwaveTheme.BackgroundDark;
                    lbl.ForeColor = SynthwaveTheme.TextPrimary;
                }
                else if(child is TextBox tb)
                {
                    tb.BackColor = SynthwaveTheme.SurfaceDark;
                    tb.ForeColor = SynthwaveTheme.TextPrimary;
                }
                else if (child is RichTextBox rtb)
                {
                    rtb.BackColor = SynthwaveTheme.SurfaceDark;
                    rtb.ForeColor = SynthwaveTheme.TextPrimary;
                }
                else if(child is ComboBox cb)
                {
                    cb.BackColor = SynthwaveTheme.SurfaceDark;
                    cb.ForeColor = SynthwaveTheme.TextPrimary;
                }
                else if(child is MenuStrip ms)
                {
                    ms.BackColor = SynthwaveTheme.SurfaceDark;
                    ms.ForeColor = SynthwaveTheme.TextPrimary;
                    foreach(ToolStripMenuItem item in ms.Items)
                    {
                        item.BackColor = SynthwaveTheme.SurfaceDark;
                        item.ForeColor = SynthwaveTheme.TextPrimary;
                        foreach(ToolStripItem dropDownItem in item.DropDownItems)
                        {
                            dropDownItem.BackColor = SynthwaveTheme.SurfaceDark;
                            dropDownItem.ForeColor = SynthwaveTheme.TextPrimary;
                        }
                    }
                }
                else if(child is ToolStrip ts)
                {
                    ts.BackColor = SynthwaveTheme.SurfaceDark;
                    ts.ForeColor = SynthwaveTheme.TextPrimary;
                    foreach(var item in ts.Items)
                    {
                       if(item is ToolStripButton tsb)
                        {
                            tsb.BackColor = SynthwaveTheme.SurfaceDark;
                            tsb.ForeColor = SynthwaveTheme.TextPrimary;
                        }
                       if(item is ToolStripLabel tsl)
                        {
                            tsl.BackColor = SynthwaveTheme.SurfaceDark;
                            tsl.ForeColor = SynthwaveTheme.TextPrimary;
                        }
                       if(item is ToolStripDropDownButton tsddb)
                        {
                            tsddb.BackColor = SynthwaveTheme.SurfaceDark;
                            tsddb.ForeColor = SynthwaveTheme.TextPrimary;
                        }
                       if(item is ToolStripMenuItem tsmi)
                        {
                            tsmi.BackColor = SynthwaveTheme.SurfaceDark;
                            tsmi.ForeColor = SynthwaveTheme.TextPrimary;
                        }
                    }
                }
                else if(child is TabControl tc)
                {
                    
                    tc.BackColor = SynthwaveTheme.SurfaceDark;
                    tc.ForeColor = SynthwaveTheme.TextPrimary;
                    foreach(TabPage tp in tc.TabPages)
                    {
                        
                        tp.UseVisualStyleBackColor = false;
                        tp.BackColor = SynthwaveTheme.SurfaceDark;
                        tp.ForeColor = SynthwaveTheme.TextPrimary;
                    }
                }
               

                
                if(child.HasChildren)
                {
                    child.ApplySynthwaveTheme();
                }
            }
        }

        private static void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tabControl = sender as TabControl;
            var tabPage = tabControl.TabPages[e.Index];

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            // Select theme colors based on whether the tab is active
            Color backColor = isSelected ? SynthwaveTheme.NeonPink : SynthwaveTheme.SurfaceLight;
            Color foreColor = isSelected ? SynthwaveTheme.BackgroundDark : SynthwaveTheme.TextPrimary;

            // Draw the tab background
            using(var brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            // Draw the tab text centered
            var paddedBounds = new Rectangle(e.Bounds.X, e.Bounds.Y + 2, e.Bounds.Width, e.Bounds.Height - 2);
            TextRenderer.DrawText(e.Graphics, tabPage.Text, tabControl.Font, paddedBounds, foreColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
