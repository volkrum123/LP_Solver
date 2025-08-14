using System.Text.RegularExpressions;
using System.Windows.Forms;
using LP_Solver.Controllers;

namespace LP_Solver
{
    public partial class Form1 : Form
    {
        private LPController _controller;
        public Form1()
        {
            InitializeComponent();
            _controller = new LPController();
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            comboBox1.Items.Add("Solve using:");
            comboBox1.Items.Add("Primal Simplex");
            comboBox1.Items.Add("Revised Primal Simplex");
            comboBox1.Items.Add("Branch & Bound Simplex");
            comboBox1.Items.Add("Cutting Plane Algorithm");
            comboBox1.Items.Add("Branch & Bound Knapsack");

            comboBox1.SelectedIndex = 0;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0)
            {
                return;
            }
            switch (comboBox1.SelectedIndex)
            {
                case 1:
                    txtOutput.Clear();
                    _controller.SolveFromInput(txtInput.Text, AppendOutput);
                    break;
            }
            comboBox1.SelectedIndex = 0;
        }
        private void AppendOutput(string text)
        {
            txtOutput.AppendText(text);
        }

        private void btnLoadFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog.Title = "Select LP Model File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Read file content
                    string lpModelText = File.ReadAllText(openFileDialog.FileName);

                    // Put it into the txtInput box for preview
                    txtInput.Text = lpModelText;

                }
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog.Title = "Save Output As";
                saveFileDialog.FileName = "LP_Output.txt"; // default filename

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveFileDialog.FileName, txtOutput.Text);
                    MessageBox.Show("Output saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
