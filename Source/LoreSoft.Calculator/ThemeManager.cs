using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LoreSoft.Calculator
{
    // Centralized theme management with light/dark support
    public static class ThemeManager
    {
        public enum ThemeMode { Light, Dark }
        public static ThemeMode CurrentTheme { get; private set; } = ThemeMode.Light;

        private static string ThemeDir { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LoreSoft");
        private static string ThemeFile { get; } = Path.Combine(ThemeDir, "theme.config");

        // Light theme palette
        private static readonly Color Light_BackColor = Color.FromArgb(246, 248, 250);
        private static readonly Color Light_PanelColor = Color.FromArgb(235, 245, 255);
        private static readonly Color Light_ForeColor = Color.FromArgb(45, 45, 48);
        private static readonly Color Light_AccentColor = Color.FromArgb(0, 122, 204);
        private static readonly Color Light_ButtonColor = Color.FromArgb(210, 226, 245);
        private static readonly Color Light_ButtonTextColor = Color.FromArgb(15, 15, 15);
        private static readonly Color Light_HighlightTextColor = Color.FromArgb(70, 70, 70);
        private static readonly Color Light_AlternatingBackColor = Color.FromArgb(251, 248, 232);

        // Dark theme palette
        private static readonly Color Dark_BackColor = Color.FromArgb(34, 34, 34);
        private static readonly Color Dark_PanelColor = Color.FromArgb(44, 44, 44);
        private static readonly Color Dark_ForeColor = Color.FromArgb(230, 230, 230);
        private static readonly Color Dark_AccentColor = Color.FromArgb(0x66, 0xA3, 0xFF);
        private static readonly Color Dark_ButtonColor = Color.FromArgb(64, 64, 64);
        private static readonly Color Dark_ButtonTextColor = Color.White;
        private static readonly Color Dark_HighlightTextColor = Color.White;
        private static readonly Color Dark_AlternatingBackColor = Color.FromArgb(52, 52, 52);

        // Apply theme recursively to a form and its child controls
        public static void ApplyTheme(Form form)
        {
            if (form == null) return;
            form.BackColor = (CurrentTheme == ThemeMode.Dark) ? Dark_BackColor : Light_BackColor;
            foreach (Control c in form.Controls)
            {
                ApplyThemeToControl(c);
            }
        }

        private static void ApplyThemeToControl(Control c)
        {
            if (c == null) return;
            var themeDark = (CurrentTheme == ThemeMode.Dark);

            // Panels/GroupBox/TabPage backgrounds
            if (c is Panel || c is GroupBox || c is TabPage)
                c.BackColor = themeDark ? Dark_PanelColor : Light_PanelColor;

            // Foreground colors for labels
            if (c is Label)
            {
                c.BackColor = themeDark ? Color.FromArgb(119, 136, 153) : Color.FromArgb(158, 170, 182);
                c.ForeColor = themeDark ? Dark_ForeColor : Light_ForeColor;
            }

            // Buttons
            if (c is Button btn)
            {
                btn.BackColor = themeDark ? Dark_ButtonColor : Light_ButtonColor;
                btn.ForeColor = themeDark ? Dark_ButtonTextColor : Light_ButtonTextColor;
                btn.FlatStyle = FlatStyle.System;
            }

            // Text inputs
            if (c is TextBox tb)
            {
                tb.BackColor = themeDark ? Light_HighlightTextColor : Dark_HighlightTextColor;
                tb.ForeColor = themeDark ? Dark_ForeColor : Light_ForeColor;
            }
            if (c is RichTextBox rtb)
            {
                rtb.BackColor = themeDark ? Light_HighlightTextColor : Dark_HighlightTextColor;
                rtb.ForeColor = themeDark ? Dark_ForeColor : Light_ForeColor;
            }

            // DataGridView
            if (c is DataGridView dgv)
            {
                dgv.BackColor = themeDark ? Color.FromArgb(40, 40, 40) : Color.White;
                dgv.ForeColor = themeDark ? Dark_ForeColor : Light_ForeColor;
                dgv.EnableHeadersVisualStyles = false;
                dgv.ColumnHeadersDefaultCellStyle.BackColor = themeDark ? Dark_PanelColor : Light_PanelColor;
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = dgv.ForeColor;
                dgv.DefaultCellStyle.SelectionForeColor = dgv.ForeColor;
                dgv.DefaultCellStyle.SelectionBackColor = themeDark ? Color.FromArgb(0, 120, 215) : Color.FromArgb(155, 202, 239);
                // Subtle alternating row colors for readability
                dgv.RowsDefaultCellStyle.BackColor = themeDark ? Color.FromArgb(40, 40, 40) : Color.White;
                dgv.AlternatingRowsDefaultCellStyle.BackColor = themeDark ? Dark_AlternatingBackColor : Light_AlternatingBackColor;
            }

            // TabControl
            if (c is TabControl tc)
            {
                tc.BackColor = themeDark ? Dark_PanelColor : Light_PanelColor;
                tc.ForeColor = themeDark ? Dark_ForeColor : Light_ForeColor;
            }

            // Menu & tool strips
            if (c is MenuStrip ms)
            {
                ms.BackColor = themeDark ? Dark_BackColor : Light_BackColor;
                ms.ForeColor = themeDark ? Dark_ForeColor : Light_ForeColor;
            }
            if (c is ToolStrip ts)
            {
                ts.BackColor = themeDark ? Dark_BackColor : Light_BackColor;
                ts.ForeColor = themeDark ? Dark_ForeColor : Light_ForeColor;
            }

            // Recurse
            foreach (Control child in c.Controls)
            {
                ApplyThemeToControl(child);
            }
        }

        // Toggle theme and persist
        public static void ToggleTheme()
        {
            CurrentTheme = (CurrentTheme == ThemeMode.Light) ? ThemeMode.Dark : ThemeMode.Light;
            SaveThemeToStorage();
            ApplyThemeToAllOpenForms();
        }

        private static void ApplyThemeToAllOpenForms()
        {
            foreach (Form f in Application.OpenForms)
            {
                ApplyTheme(f);
            }
        }

        public static void InitializeFromStorage()
        {
            try
            {
                if (!Directory.Exists(ThemeDir))
                    Directory.CreateDirectory(ThemeDir);
                if (File.Exists(ThemeFile))
                {
                    string v = File.ReadAllText(ThemeFile).Trim();
                    CurrentTheme = string.Equals(v, "Dark", StringComparison.OrdinalIgnoreCase) ? ThemeMode.Dark : ThemeMode.Light;
                }
                else
                {
                    CurrentTheme = ThemeMode.Light;
                    SaveThemeToStorage();
                }
            }
            catch
            {
                CurrentTheme = ThemeMode.Light;
            }
            // Apply immediately so UI reflects preference
            ApplyThemeToAllOpenForms();
        }

        private static void SaveThemeToStorage()
        {
            try
            {
                File.WriteAllText(ThemeFile, CurrentTheme == ThemeMode.Dark ? "Dark" : "Light");
            }
            catch
            {
                // ignore storage errors to avoid affecting UX
            }
        }
    }
}
