using LoreSoft.MathExpressions;
using System.Windows.Forms;

namespace LoreSoft.Calculator
{
    public partial class VariablesForm : Form
    {
        public VariablesForm()
        {
            InitializeComponent();
            ThemeManager.InitializeFromStorage();
            ThemeManager.ApplyTheme(this);
        }

        public void InputVariable(VariableDictionary dict)
        {
            foreach (DataGridViewRow r in dataGridView1.Rows) {
                var name = r.Cells[0].EditedFormattedValue as string;
                if (string.IsNullOrEmpty(name))
                    continue;

                var value = r.Cells[1].EditedFormattedValue as string;
                if (string.IsNullOrEmpty(value))
                    continue;
                dict[name] = double.Parse(value);
            }
        }

        public void OutputVariable(VariableDictionary dict, bool keep = false)
        {
            this.SuspendLayout();
            // this.Enabled = false;

            if (keep)
            {
                foreach (var v in dict)
                {
                    bool found = false;
                    foreach (DataGridViewRow r in dataGridView1.Rows)
                    {
                        var name = r.Cells[0].EditedFormattedValue as string;
                        if (string.IsNullOrEmpty(name))
                            continue;

                        if (v.Key == name.Trim())
                        {
                            r.Cells[1].Value = v.Value;
                            found = true;
                            break;
                        }
                    }

                    if (found)
                        continue;

                    dataGridView1.Rows.Add(new object[] { v.Key, v.Value });
                }
            }
            else
            {
                dataGridView1.Rows.Clear();

                foreach (var v in dict)
                    dataGridView1.Rows.Add(new object[] { v.Key, v.Value });
            }
            
            // this.Enabled = true;
            this.ResumeLayout();
        }

        public void UpdateVariable(string name, double value)
        {
            if (name == null)
                return;

            this.SuspendLayout();
            this.Enabled = false;
            foreach (DataGridViewRow r in dataGridView1.Rows)
            {
                var r_n = r.Cells[0].Value as string;
                if (r_n == name)
                {
                    r.Cells[1].Value = value;
                    return;
                }
            }

            dataGridView1.Rows.Add(new object[] { name, value });
            this.Enabled = true;
            this.ResumeLayout();
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == 0)
                return;

            if (Owner != null && Owner is CalculatorForm cForm)
            {
                int row = e.RowIndex;
                int col = e.ColumnIndex;

                if (col == 1)
                {
                    var name = dataGridView1.Rows[row].Cells[0].Value as string;
                    var value = dataGridView1.Rows[row].Cells[1].Value as string;
                    if (!string.IsNullOrEmpty(value)
                        && !string.IsNullOrEmpty(name)
                        && double.TryParse(value, out double dbl))
                    {
                        cForm.GetVariables()[name] = dbl;
                        cForm.EvaluateRichTextBox(false, false, true);
                    }
                }
            }
        }
    }
}
