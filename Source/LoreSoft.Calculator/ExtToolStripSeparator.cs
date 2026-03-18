using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static LoreSoft.Calculator.ThemeManager;

namespace LoreSoft.Calculator
{
    internal class ExtToolStripSeparator : ToolStripSeparator
    {
        public ExtToolStripSeparator()
        {
            this.Paint += ExtendedToolStripSeparator_Paint;
        }

        private void ExtendedToolStripSeparator_Paint(object sender, PaintEventArgs e)
        {
            // Get the separator's width and height.
            ToolStripSeparator toolStripSeparator = (ToolStripSeparator)sender;
            int width = toolStripSeparator.Width;
            int height = toolStripSeparator.Height;

            // Choose the colors for drawing.
            // I've used Color.White as the foreColor.
            Color foreColor = CurrentTheme == ThemeMode.Dark ? Dark_ForeColor : Light_ForeColor;
            // Color.Teal as the backColor.
            Color backColor = CurrentTheme == ThemeMode.Dark ? Dark_BackColor : Light_BackColor;

            // Fill the background.
            e.Graphics.FillRectangle(new SolidBrush(backColor), 0, 0, width, height);

            // Draw the line.
            e.Graphics.DrawLine(new Pen(foreColor), 4, height / 2, width - 4, height / 2);
        }
    }
}
