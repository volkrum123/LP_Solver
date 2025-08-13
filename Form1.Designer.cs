namespace LP_Solver
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            txtInput = new TextBox();
            txtOutput = new RichTextBox();
            comboBox1 = new ComboBox();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(278, 9);
            label1.Name = "label1";
            label1.Size = new Size(174, 20);
            label1.TabIndex = 0;
            label1.Text = "Load LP model input file:";
            // 
            // txtInput
            // 
            txtInput.Location = new Point(410, 48);
            txtInput.Multiline = true;
            txtInput.Name = "txtInput";
            txtInput.ScrollBars = ScrollBars.Vertical;
            txtInput.Size = new Size(400, 150);
            txtInput.TabIndex = 1;
            // 
            // txtOutput
            // 
            txtOutput.Font = new Font("Consolas", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtOutput.Location = new Point(12, 217);
            txtOutput.Name = "txtOutput";
            txtOutput.ReadOnly = true;
            txtOutput.Size = new Size(967, 306);
            txtOutput.TabIndex = 3;
            txtOutput.Text = "";
            // 
            // comboBox1
            // 
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.FlatStyle = FlatStyle.Flat;
            comboBox1.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(28, 46);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(240, 31);
            comboBox1.TabIndex = 4;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1085, 569);
            Controls.Add(comboBox1);
            Controls.Add(txtOutput);
            Controls.Add(txtInput);
            Controls.Add(label1);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load_1;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion


        private Label label1;
        private TextBox txtInput;
        private RichTextBox txtOutput;
        private ComboBox comboBox1;
    }
}
