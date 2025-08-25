using System;
using System.IO;
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
            comboBox1.Items.Add("Dual Simplex");
            comboBox1.Items.Add("Revised Primal Simplex");
            comboBox1.Items.Add("Branch & Bound Simplex");
            comboBox1.Items.Add("Cutting Plane Algorithm");
            comboBox1.Items.Add("Branch & Bound Knapsack");
            comboBox1.Items.Add("Nonlinear");
            comboBox1.SelectedIndex = 0;

            // Optional: better alignment for ASCII output
            txtOutput.Font = new System.Drawing.Font("Consolas", 9f);
            txtOutput.WordWrap = false;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0) return;

            switch (comboBox1.SelectedIndex)
            {
                case 1: // Primal Simplex
                    txtOutput.Clear();
                    _controller.SolveFromInput(txtInput.Text, AppendOutput);
                    break;

                case 2: // Dual Simplex
                    txtOutput.Clear();
                    _controller.DualSolveFromInput(txtInput.Text, AppendOutput);
                    break;
                case 3:
                    txtOutput.Clear();
                    _controller.DualSolveFromInput(txtInput.Text, AppendOutput);
                    break;
                case 4:
                    txtOutput.Clear();
                    _controller.BranchAndBoundSolveFromInput(txtInput.Text, AppendOutput);
                    break;
                case 5:
                    txtOutput.Clear();
                    _controller.CuttingPlaneSolveFromInput(txtInput.Text, AppendOutput);
                    break;

                case 6: // Branch & Bound Knapsack (LP-style expected)
                    txtOutput.Clear();
                    try
                    {
                        // Expect LP/IP-style like:
                        // max +2 +2 +3 +5 +2 +4
                        // +11 +8 +6 +14 +10 +10 <= 40
                        // bin bin bin bin bin bin
                        bool hasObj = Regex.IsMatch(txtInput.Text, @"\b(max|min)\b", RegexOptions.IgnoreCase);
                        bool hasCap = Regex.IsMatch(txtInput.Text, @"<=", RegexOptions.IgnoreCase);
                        bool hasBin = Regex.IsMatch(txtInput.Text, @"\bbin\b", RegexOptions.IgnoreCase);

                        if (!(hasObj && hasCap && hasBin))
                        {
                            MessageBox.Show(
                                "Please use LP/IP-style input for Branch & Bound Knapsack:\n\n" +
                                "max +2 +2 +3 +5 +2 +4\n" +
                                "+11 +8 +6 +14 +10 +10 <= 40\n" +
                                "bin bin bin bin bin bin",
                                "Input format",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                            return;
                        }

                        // Parse LP and solve Knapsack via B&B (your LP-style method)
                        _controller.SolveKnapsackFromInput(txtInput.Text, AppendOutput);
                    }
                    catch (Exception ex)
                    {
                        AppendOutput($"Error: {ex.Message}\r\n");
                    }
                    break;
                case 7: // Nonlinear (Golden Section)
                    txtOutput.Clear();
                    try
                    {
                        _controller.SolveNonlinearFromInput(txtInput.Text, AppendOutput);
                    }
                    catch (Exception ex)
                    {
                        AppendOutput($"Error: {ex.Message}\r\n");
                    }
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

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
        }
    }
}
