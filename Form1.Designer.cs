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
            btnLoadFile = new Button();
            btnExport = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(757, 23);
            label1.Name = "label1";
            label1.Size = new Size(106, 20);
            label1.TabIndex = 0;
            label1.Text = "Type in model:";
            // 
            // txtInput
            // 
            txtInput.Location = new Point(631, 46);
            txtInput.Multiline = true;
            txtInput.Name = "txtInput";
            txtInput.ScrollBars = ScrollBars.Vertical;
            txtInput.Size = new Size(400, 150);
            txtInput.TabIndex = 1;
            // 
            // txtOutput
            // 
            txtOutput.Font = new Font("Consolas", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtOutput.Location = new Point(64, 211);
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
            comboBox1.Location = new Point(64, 23);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(240, 31);
            comboBox1.TabIndex = 4;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // btnLoadFile
            // 
            btnLoadFile.Location = new Point(349, 24);
            btnLoadFile.Name = "btnLoadFile";
            btnLoadFile.Size = new Size(94, 29);
            btnLoadFile.TabIndex = 5;
            btnLoadFile.Text = "Upload LP";
            btnLoadFile.UseVisualStyleBackColor = true;
            btnLoadFile.Click += btnLoadFile_Click;
            // 
            // btnExport
            // 
            btnExport.Location = new Point(485, 528);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(159, 29);
            btnExport.TabIndex = 6;
            btnExport.Text = "Export solved LP";
            btnExport.UseVisualStyleBackColor = true;
            btnExport.Click += btnExport_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1085, 569);
            Controls.Add(btnExport);
            Controls.Add(btnLoadFile);
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
        private Button btnLoadFile;
        private Button btnExport;
    }
}
