using LoreSoft.Calculator.Properties;
using LoreSoft.MathExpressions;
using LoreSoft.MathExpressions.Metadata;
using LoreSoft.MathExpressions.UnitConversion;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace LoreSoft.Calculator
{
    public partial class CalculatorForm : Form
    {
        struct HistoryData
        {
            public string Expression;
            public bool Success;
            public bool Regular;
        }

        private const string tabSpace = "\t\t\t";
        private MathEvaluator _eval = new MathEvaluator();
        private List<HistoryData> _history = new List<HistoryData>();
        private int _historyIndex = 0;
        private Stopwatch watch = new Stopwatch();
        private VariablesForm vform = new VariablesForm();
        private bool lockTextChange = false;
        private bool lockClearVariables = false;

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
            numLockToolStripStatusLabel.Text = NativeMethods.IsNumLockOn ? "数字锁定" : string.Empty;
            answerToolStripStatusLabel.Text = "答案: " + _eval.Answer;

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
            inputTextBox.Text = _history[_historyIndex].Expression;
            inputTextBox.Select(inputTextBox.TextLength, 0);
            inputTextBox.Focus();
        }

        private bool Lock() { if (lockTextChange) return false; lockTextChange = true; return true; }
        private void Unlock() { lockTextChange = false; }

        private void AppendData(HistoryData data, string answer)
        {
            bool success = data.Success;

            if (!data.Regular)
            {
                var variableName = data.Expression.Trim();
                int id = variableName.IndexOf('=');
                if (id > -1)
                    variableName = variableName.Substring(0, id).Trim();

                success = GetVariables().TryGetValue(variableName, out var value);
                answer = value.ToString();
            }

            AppendData(data.Expression, success, answer);
        }

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

        public void EvaluateAll(bool shouldLock = false)
        {
            if (shouldLock)
            {
                if (!Lock())
                    return;
            }

            historyRichTextBox.SuspendLayout();

            historyRichTextBox.ResetText();

            foreach (HistoryData data in _history)
            {
                if (data.Regular)
                {
                    string line_ans;

                    try
                    {
                        var r = _eval.Evaluate(data.Expression);
                        line_ans = r.Result.ToString();
                    }
                    catch (Exception ex)
                    {
                        line_ans = ex.Message;
                    }

                    AppendData(data, line_ans);
                }
                else
                {
                    AppendData(data, null);
                }
            }

            historyRichTextBox.ScrollToCaret();
            historyRichTextBox.ResumeLayout();

            if (shouldLock)
                Unlock();
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
                var frontStr = currentText.Substring(0, currentSel);
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

                    int tab_id = expression.IndexOf('\t');
                    if (tab_id > -1)
                        expression = expression.Substring(0, tab_id);

                    if (string.IsNullOrEmpty(expression.Trim()))
                    {
                        var append = Environment.NewLine;
                        historyRichTextBox.AppendText(append);
                        if (++row <= currentRow)
                            row_begin += append.Length;
                        continue;
                    }

                    if (ignoreRegular)
                    {
                        int eId = expression.IndexOf('=');
                        if (eId > -1)
                        {
                            var variableName = expression.Substring(0, eId).Trim();
                            if (!string.IsNullOrEmpty(variableName) && GetVariables().TryGetValue(variableName, out double variableValue))
                                expression = variableName + " = " + variableValue.ToString();
                        }
                    }

                    string line_ans;
                    bool success = true;

                    try
                    {
                        var r = _eval.Evaluate(expression);
                        line_ans = r.Result.ToString();
                    }
                    catch (Exception ex)
                    {
                        line_ans = ex.Message;
                        success = false;
                    }

                    int append_len = AppendData(expression, success, line_ans);
                    if (++row <= currentRow)
                        row_begin += append_len;
                }

                watch.Stop();
                timerToolStripStatusLabel.Text = watch.Elapsed.TotalMilliseconds + " 毫秒";

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

            string answer;
            bool hasError = false;
            bool regular = true;

            watch.Reset();
            watch.Start();
            try
            {
                vform.InputVariable(GetVariables());
                var r = _eval.Evaluate(input);
                answer = r.Result.ToString();
                regular = r.Regular;
                vform.OutputVariable(GetVariables(), true);
            }
            catch (Exception ex)
            {
                answer = ex.Message;
                hasError = true;
            }
            watch.Stop();
            timerToolStripStatusLabel.Text = watch.Elapsed.TotalMilliseconds + " 毫秒";

            // EvaluateAll();

            _history.Add(new HistoryData { Expression = input, Success = !hasError, Regular = regular });
            _historyIndex = 0;

            historyRichTextBox.SuspendLayout();

            AppendData(_history[_history.Count - 1], answer);

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
            lockClearVariables = false;
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
    }
}
