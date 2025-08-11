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

        private void btnParse_Click(object sender, EventArgs e)
        {
            txtOutput.Clear();
            _controller.SolveFromInput(txtInput.Text, AppendOutput);

            
        }

        private void AppendOutput(string text)
        {
            txtOutput.AppendText(text);
        }

    }
}
