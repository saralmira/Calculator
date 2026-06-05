using LoreSoft.Calculator.Properties;
using LoreSoft.MathExpressions;
using LoreSoft.MathExpressions.Metadata;
using LoreSoft.MathExpressions.UnitConversion;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using EvalResult = LoreSoft.MathExpressions.MathEvaluator.EvalResult;

namespace LoreSoft.Calculator
{
    public partial class CalculatorForm : Form
    {
        struct EvaluateData
        {
            public EvalResult Result;
            public string Answer;
            public int AppendLength;
        }

        private const string tabSpace = "\t\t\t";
        private MathEvaluator _eval = new MathEvaluator();
        private List<string> _history = new List<string>();
        private int _historyIndex = 0;
        private Stopwatch watch = new Stopwatch();
        private VariablesForm vform = new VariablesForm();
        private bool lockTextChange = false;
        private bool lockClearVariables = true;

        public CalculatorForm()
        {
            InitializeComponent();
            // Load and apply theme, then apply settings
            ThemeManager.InitializeFromStorage();
            ThemeManager.ApplyTheme(this);
            // Enable keyboard shortcut for theme toggle
            this.KeyPreview = true;
            this.KeyDown += CalculatorForm_KeyDown;
            InitializeSettings();
            Application.Idle += new EventHandler(OnApplicationIdle);
            vform.Owner = this;
            vform.Location = new Point(Location.X + Width, Location.Y);
            timer1.Stop();

            if (historyRichTextBox.Text.Length > 0)
                EvaluateRichTextBox();
        }

        private void InitializeSettings()
        {
            SuspendLayout();

            if (Settings.Default["CalculatorLocation"] != null)
                Location = Settings.Default.CalculatorLocation;
            if (Settings.Default["CalculatorSize"] != null)
                Size = Settings.Default.CalculatorSize;
            if (Settings.Default["CalculatorWindowState"] != null)
                WindowState = Settings.Default.CalculatorWindowState;
            if (Settings.Default["HistoryFont"] != null)
                historyRichTextBox.Font = Settings.Default.HistoryFont;
            if (Settings.Default["InputFont"] != null)
                inputTextBox.Font = Settings.Default.InputFont;
            if (Settings.Default["HistoryText"] != null)
                historyRichTextBox.Text = Settings.Default.HistoryText;

            replaceCalculatorToolStripMenuItem.Checked = (Application.ExecutablePath.Equals(
                ImageFileOptions.GetDebugger(CalculatorConstants.WindowsCalculatorName),
                StringComparison.OrdinalIgnoreCase));

            allowOnlyOneInstanceToolStripMenuItem.Checked = Settings.Default.IsSingleInstance;

            ResumeLayout(true);
        }

        private void OnApplicationIdle(object sender, EventArgs e)
        {
            numLockToolStripStatusLabel.Text = NativeMethods.IsNumLockOn ? "Êý×ÖËø¶¨" : string.Empty;
            answerToolStripStatusLabel.Text = "´ð°¸: " + _eval.Answer;

            undoToolStripMenuItem.Enabled = inputTextBox.ContainsFocus && inputTextBox.CanUndo;
            undoToolStripButton.Enabled = undoToolStripMenuItem.Enabled;
            undoContextStripMenuItem.Enabled = undoToolStripMenuItem.Enabled;

            cutToolStripMenuItem.Enabled = inputTextBox.ContainsFocus && inputTextBox.CanSelect;
            cutToolStripButton.Enabled = cutToolStripMenuItem.Enabled;
            cutContextStripMenuItem.Enabled = cutToolStripMenuItem.Enabled;

            pasteToolStripMenuItem.Enabled = inputTextBox.ContainsFocus && Clipboard.ContainsText();
            pasteToolStripButton.Enabled = pasteToolStripMenuItem.Enabled;
            pasteContextStripMenuItem.Enabled = pasteToolStripMenuItem.Enabled;
        }

        private void SetInputFromHistory()
        {
            inputTextBox.Text = _history[_historyIndex];
            inputTextBox.Select(inputTextBox.TextLength, 0);
            inputTextBox.Focus();
        }

        private bool Lock() { if (lockTextChange) return false; lockTextChange = true; return true; }
        private void Unlock() { lockTextChange = false; }

        private int AppendData(string expression, bool success, string answer)
        {
            int oldLength = historyRichTextBox.Text.Length;
            historyRichTextBox.AppendText(expression);
            // historyRichTextBox.AppendText(Environment.NewLine);
            int maxAnsChar = 18;
            int baseTabWidth = Math.Max(Width - 75 - maxAnsChar * 20, 300);
            historyRichTextBox.SelectionTabs = new int[] { baseTabWidth, baseTabWidth + 15, baseTabWidth + 15, baseTabWidth + 15, baseTabWidth + 15 };
            historyRichTextBox.AppendText(tabSpace);
            if (!success)
                historyRichTextBox.SelectionColor = Color.Maroon;
            //else
            //    historyRichTextBox.SelectionColor = Color.Blue;
            historyRichTextBox.SelectionFont = new Font(historyRichTextBox.Font, FontStyle.Bold);
            historyRichTextBox.AppendText(answer);
            historyRichTextBox.AppendText(Environment.NewLine);
            return historyRichTextBox.Text.Length - oldLength;
        }

        private int AppendComment(string comment)
        {
            int oldLength = historyRichTextBox.Text.Length;
            historyRichTextBox.SelectionColor = Color.Green;
            historyRichTextBox.SelectionFont = new Font(historyRichTextBox.Font, FontStyle.Italic);
            historyRichTextBox.AppendText(comment + Environment.NewLine);
            return historyRichTextBox.Text.Length - oldLength;
        }

        private EvaluateData Evaluate(string expression, bool append = true, bool ignoreRegular = false, bool addHistory = false)
        {
            EvaluateData ret = new EvaluateData()
            {
                Result = new EvalResult() { Result = 0, Regular = true },
                Answer = string.Empty,
                AppendLength = 0
            };

            int tab_id = expression.IndexOf('\t');
            if (tab_id > -1)
                expression = expression.Substring(0, tab_id);

            var trimExpress = expression.Trim();
            if (string.IsNullOrEmpty(trimExpress))
            {
                if (append)
                {
                    int oldLen = historyRichTextBox.Text.Length;
                    historyRichTextBox.AppendText(Environment.NewLine);
                    ret.AppendLength = historyRichTextBox.Text.Length - oldLen;
                }

                return ret;
            }

            if (expression[0] == ';' || expression[0] == '/')
            {
                if (append)
                {
                    ret.AppendLength = AppendComment(expression);
                }

                return ret;
            }

            // get value from variables
            if (ignoreRegular)
            {
                int id = trimExpress.IndexOf('=');
                if (id > -1)
                {
                    var variableName = trimExpress.Substring(0, id).Trim();
                    if (!string.IsNullOrEmpty(variableName) && GetVariables().TryGetValue(variableName, out var variableValue))
                    {
                        expression = variableName + " = " + ToString(variableValue);
                    }
                }
            }

            bool success = true;

            try
            {
                var r = _eval.Evaluate(expression);
                ret.Answer = ToString(r.Result);
                ret.Result = r;
            }
            catch (Exception ex)
            {
                ret.Answer = ex.Message;
                success = false;
            }

            if (append)
            {
                ret.AppendLength = AppendData(expression, success, ret.Answer);
            }

            if (addHistory)
            {
                _history.Add(expression);
                _historyIndex = 0;
            }

            return ret;
        }

        public void EvaluateRichTextBox(bool clearVariables = true, bool ignoreRegular = false, bool outputVariables = true)
        {
            if (!Lock())
                return;

            historyRichTextBox.SuspendLayout();

            string currentText = historyRichTextBox.Text;
            int currentRow = 0, currentIndent = 0, currentSel = historyRichTextBox.SelectionStart;

            historyRichTextBox.ResetText();

            if (currentSel > 0)
            {
                var frontStr = currentSel >= currentText.Length ? currentText : currentText.Substring(0, currentSel);
                currentRow = frontStr.Count((c) => { return c == '\n'; });
                currentIndent = Math.Max(0, currentSel - (frontStr.LastIndexOf('\n') + 1));
            }

            if (clearVariables)
                InitVariables();

            using (StringReader sr = new StringReader(currentText))
            {
                int row = 0;
                int row_begin = 0;

                watch.Reset();
                watch.Start();

                while (sr.Peek() >= 0)
                {
                    var line = sr.ReadLine();
                    string expression = line;

                    var eval = Evaluate(expression, true, ignoreRegular, false);

                    if (++row <= currentRow)
                        row_begin += eval.AppendLength;
                }

                watch.Stop();
                timerToolStripStatusLabel.Text = watch.Elapsed.TotalMilliseconds + " ºÁÃë";

                historyRichTextBox.SelectionStart = row_begin + currentIndent;
            }

            if (!ignoreRegular && outputVariables)
                vform.OutputVariable(GetVariables(), true);

            Unlock();

            historyRichTextBox.ScrollToCaret();
            historyRichTextBox.ResumeLayout();
        }

        private void Eval(string input)
        {
            if (!Lock())
                return;

            historyRichTextBox.SuspendLayout();

            watch.Reset();
            watch.Start();

            Evaluate(input, true, false, true);

            watch.Stop();
            timerToolStripStatusLabel.Text = watch.Elapsed.TotalMilliseconds + " ºÁÃë";

            historyRichTextBox.ScrollToCaret();
            historyRichTextBox.ResumeLayout();

            inputTextBox.ResetText();
            inputTextBox.Focus();
            inputTextBox.Select();
            Unlock();
        }

        private void CalculatorForm_Load(object sender, EventArgs e)
        {
            historyRichTextBox.AutoWordSelection = false;
            inputTextBox.Focus();
            inputTextBox.Select();
        }

        private void CalculatorForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+T toggles the theme
            if (e.Control && e.KeyCode == Keys.T)
            {
                toggleThemeToolStripButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private void CalculatorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Default.CalculatorLocation = Location;
            Settings.Default.CalculatorSize = Size;
            Settings.Default.CalculatorWindowState = WindowState;
            Settings.Default.HistoryFont = historyRichTextBox.Font;
            Settings.Default.InputFont = inputTextBox.Font;
            Settings.Default.HistoryText = historyRichTextBox.Text;
            Settings.Default.Save();
        }

        private void inputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter && inputTextBox.TextLength > 0)
            {
                Eval(inputTextBox.Text);
                e.Handled = true;
                return;
            }

            if (e.KeyData == Keys.Up && _history.Count > 0)
            {
                _historyIndex--;

                if (_historyIndex < 0)
                    _historyIndex = _history.Count - 1;

                SetInputFromHistory();
                e.Handled = true;
                return;
            }

            if (e.KeyData == Keys.Down && _history.Count > 0)
            {
                _historyIndex++;
                if (_historyIndex >= _history.Count)
                    _historyIndex = 0;

                SetInputFromHistory();
                e.Handled = true;
                return;
            }
        }

        public VariableDictionary GetVariables()
        {
            return _eval.Variables;
        }

        public void InitVariables()
        {
            _eval.InitVariables();
            vform.OutputVariable(GetVariables(), false);
        }

        private void inputTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (inputTextBox.TextLength != 0 || !OperatorExpression.IsSymbol(e.KeyChar))
                return;

            inputTextBox.Text = MathEvaluator.AnswerVariable + e.KeyChar;
            inputTextBox.Select(inputTextBox.TextLength, 0);
            e.Handled = true;

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutForm about = new AboutForm();
            about.ShowDialog(this);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = saveFileDialog.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog.FileName, historyRichTextBox.Text);
            }
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            inputTextBox.Undo();
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            inputTextBox.Cut();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (historyRichTextBox.ContainsFocus)
                historyRichTextBox.Copy();
            else
                inputTextBox.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            inputTextBox.Paste();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            inputTextBox.SelectAll();
        }

        private void clearHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lockClearVariables = false;
            historyRichTextBox.ResetText();
        }

        private void historyFontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fontDialog.Font = historyRichTextBox.Font;
            DialogResult result = fontDialog.ShowDialog(this);
            if (result != DialogResult.OK)
                return;

            historyRichTextBox.Font = fontDialog.Font;
        }

        private void inputFontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fontDialog.Font = inputTextBox.Font;
            DialogResult result = fontDialog.ShowDialog(this);
            if (result != DialogResult.OK)
                return;

            inputTextBox.Font = fontDialog.Font;
        }

        private void replaceCalculatorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (replaceCalculatorToolStripMenuItem.Checked)
                ImageFileOptions.SetDebugger(
                    CalculatorConstants.WindowsCalculatorName,
                    Application.ExecutablePath);
            else
                ImageFileOptions.ClearDebugger(
                    CalculatorConstants.WindowsCalculatorName);
        }

        private void function_Click(object sender, EventArgs e)
        {
            ToolStripItem item = sender as ToolStripItem;
            if (item == null || item.Tag == null)
                return;

            string insert = item.Tag.ToString();

            int start = inputTextBox.SelectionStart;
            int length = inputTextBox.SelectionLength;
            int pad = insert.IndexOf('|');


            if (pad < 0 && length == 0)
                pad = insert.Length;
            else if (pad >= 0 && length > 0)
                pad = insert.Length;

            inputTextBox.SuspendLayout();
            inputTextBox.Paste(insert.Replace("|", inputTextBox.SelectedText));
            inputTextBox.Select(start + pad + length, 0);
            inputTextBox.ResumeLayout();
        }

        private void AddToConvertMenuItem<T>(ToolStripMenuItem p)
            where T : struct, IComparable, IFormattable, IConvertible
        {
            Type enumType = typeof(T);
            int[] a = (int[])Enum.GetValues(enumType);

            p.DropDownItems.Clear();
            for (int x = 0; x < a.Length; x++)
            {
                MemberInfo parentInfo = GetMemberInfo(enumType, Enum.GetName(enumType, x));
                string parrentKey = AttributeReader.GetAbbreviation(parentInfo);
                string parrentName = AttributeReader.GetDescription(parentInfo);

                ToolStripMenuItem t = new ToolStripMenuItem(parrentName)
                {
                    BackColor = p.BackColor,
                    ForeColor = p.ForeColor
                };
                p.DropDownItems.Add(t);

                for (int i = 0; i < a.Length; i++)
                {
                    if (x == i)
                        continue;

                    MemberInfo childInfo = GetMemberInfo(enumType, Enum.GetName(enumType, i));
                    string childName = AttributeReader.GetDescription(childInfo);
                    string childKey = AttributeReader.GetAbbreviation(childInfo);

                    string key = string.Format(
                        CultureInfo.InvariantCulture,
                        ConvertExpression.ExpressionFormat,
                        parrentKey,
                        childKey);

                    ToolStripMenuItem s = new ToolStripMenuItem(childName)
                    {
                        BackColor = t.BackColor,
                        ForeColor = t.ForeColor
                    };
                    s.Click += new EventHandler(convert_Click);
                    s.Tag = key;

                    t.DropDownItems.Add(s);
                }
            }
        }

        private static MemberInfo GetMemberInfo(Type type, string name)
        {
            MemberInfo[] info = type.GetMember(name);
            if (info == null || info.Length == 0)
                return null;

            return info[0];
        }

        private void lengthToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            if (lengthToolStripMenuItem.DropDownItems.Count > 1)
                return;

            AddToConvertMenuItem<LengthUnit>(lengthToolStripMenuItem);
        }

        private void massToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            if (massToolStripMenuItem.DropDownItems.Count > 1)
                return;

            AddToConvertMenuItem<MassUnit>(massToolStripMenuItem);

        }

        private void speedToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            if (speedToolStripMenuItem.DropDownItems.Count > 1)
                return;

            AddToConvertMenuItem<SpeedUnit>(speedToolStripMenuItem);

        }

        private void temperatureToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            if (temperatureToolStripMenuItem.DropDownItems.Count > 1)
                return;

            AddToConvertMenuItem<TemperatureUnit>(temperatureToolStripMenuItem);
        }

        private void timeToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            if (timeToolStripMenuItem.DropDownItems.Count > 1)
                return;

            AddToConvertMenuItem<TimeUnit>(timeToolStripMenuItem);
        }

        private void volumeToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            if (volumeToolStripMenuItem.DropDownItems.Count > 1)
                return;

            AddToConvertMenuItem<VolumeUnit>(volumeToolStripMenuItem);
        }

        private void convert_Click(object sender, EventArgs e)
        {
            ToolStripItem item = sender as ToolStripItem;
            if (item == null || item.Tag == null)
                return;

            string insert = item.Tag.ToString();
            int start = inputTextBox.SelectionStart;
            int length = inputTextBox.SelectionLength;
            int pad = insert.Length;

            inputTextBox.SuspendLayout();
            inputTextBox.Paste(insert);
            inputTextBox.Select(start + pad + length, 0);
            inputTextBox.ResumeLayout();
        }

        private void allowOnlyOneInstanceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.IsSingleInstance = allowOnlyOneInstanceToolStripMenuItem.Checked;
            Settings.Default.Save();
        }

        private void CalculatorForm_LocationChanged(object sender, EventArgs e)
        {
            vform.Location = new Point(Location.X + Width, Location.Y);
        }

        private void historyRichTextBox_TextChanged(object sender, EventArgs e)
        {
            if (!lockTextChange)
            {
                timer1.Interval = 400;
                timer1.Start();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            EvaluateRichTextBox(!lockClearVariables);
            lockClearVariables = true;
        }

        private void toggleThemeToolStripButton_Click(object sender, EventArgs e)
        {
            lockClearVariables = true;
            ThemeManager.ToggleTheme();
        }

        private void CalculatorForm_Shown(object sender, EventArgs e)
        {
            vform.Show();
            this.Focus();
        }

        private static string ToString(decimal value)
        {
            return decimal.Round(value, 15).ToString();
        }
    }
}
