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
            panel1 = new Panel();
            comboBoxSensitivity = new ComboBox();
            label2 = new Label();
            txtSensitivityInput = new TextBox();
            label3 = new Label();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.ForeColor = SystemColors.HotTrack;
            label1.Location = new Point(267, 16);
            label1.Name = "label1";
            label1.Size = new Size(215, 23);
            label1.TabIndex = 0;
            label1.Text = "Manually Enter LP model:";
            // 
            // txtInput
            // 
            txtInput.Location = new Point(265, 46);
            txtInput.Multiline = true;
            txtInput.Name = "txtInput";
            txtInput.ScrollBars = ScrollBars.Vertical;
            txtInput.Size = new Size(583, 150);
            txtInput.TabIndex = 1;
            // 
            // txtOutput
            // 
            txtOutput.BackColor = SystemColors.ButtonHighlight;
            txtOutput.BorderStyle = BorderStyle.FixedSingle;
            txtOutput.Font = new Font("Consolas", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtOutput.ForeColor = SystemColors.InfoText;
            txtOutput.Location = new Point(265, 239);
            txtOutput.Name = "txtOutput";
            txtOutput.ReadOnly = true;
            txtOutput.Size = new Size(1187, 360);
            txtOutput.TabIndex = 3;
            txtOutput.Text = "";
            // 
            // comboBox1
            // 
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.FlatStyle = FlatStyle.Flat;
            comboBox1.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(5, 111);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(240, 31);
            comboBox1.TabIndex = 4;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // btnLoadFile
            // 
            btnLoadFile.BackColor = SystemColors.HotTrack;
            btnLoadFile.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnLoadFile.ForeColor = SystemColors.ButtonFace;
            btnLoadFile.Location = new Point(46, 11);
            btnLoadFile.Name = "btnLoadFile";
            btnLoadFile.Size = new Size(144, 38);
            btnLoadFile.TabIndex = 5;
            btnLoadFile.Text = "Upload LP model";
            btnLoadFile.UseVisualStyleBackColor = false;
            btnLoadFile.Click += btnLoadFile_Click;
            // 
            // btnExport
            // 
            btnExport.BackColor = SystemColors.HotTrack;
            btnExport.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnExport.ForeColor = SystemColors.ButtonFace;
            btnExport.Location = new Point(46, 55);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(144, 39);
            btnExport.TabIndex = 6;
            btnExport.Text = "Export solved LP";
            btnExport.UseVisualStyleBackColor = false;
            btnExport.Click += btnExport_Click;
            // 
            // panel1
            // 
            panel1.BackColor = SystemColors.ActiveCaption;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(comboBoxSensitivity);
            panel1.Controls.Add(btnLoadFile);
            panel1.Controls.Add(comboBox1);
            panel1.Controls.Add(btnExport);
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(250, 599);
            panel1.TabIndex = 7;
            // 
            // comboBoxSensitivity
            // 
            comboBoxSensitivity.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxSensitivity.FlatStyle = FlatStyle.Flat;
            comboBoxSensitivity.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            comboBoxSensitivity.FormattingEnabled = true;
            comboBoxSensitivity.Location = new Point(5, 364);
            comboBoxSensitivity.Name = "comboBoxSensitivity";
            comboBoxSensitivity.Size = new Size(240, 31);
            comboBoxSensitivity.TabIndex = 7;
            comboBoxSensitivity.SelectedIndexChanged += comboBoxSensitivity_SelectedIndexChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.ForeColor = SystemColors.HotTrack;
            label2.Location = new Point(265, 216);
            label2.Name = "label2";
            label2.Size = new Size(213, 23);
            label2.TabIndex = 8;
            label2.Text = "Desplayed Solved Model:";
            // 
            // txtSensitivityInput
            // 
            txtSensitivityInput.Location = new Point(904, 46);
            txtSensitivityInput.Name = "txtSensitivityInput";
            txtSensitivityInput.Size = new Size(216, 27);
            txtSensitivityInput.TabIndex = 9;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.ForeColor = SystemColors.HotTrack;
            label3.Location = new Point(903, 14);
            label3.Name = "label3";
            label3.Size = new Size(220, 23);
            label3.TabIndex = 10;
            label3.Text = "Apply Sensitivity changes:";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1464, 611);
            Controls.Add(label3);
            Controls.Add(txtSensitivityInput);
            Controls.Add(label2);
            Controls.Add(panel1);
            Controls.Add(txtOutput);
            Controls.Add(txtInput);
            Controls.Add(label1);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load_1;
            panel1.ResumeLayout(false);
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
        private Panel panel1;
        private Label label2;
        private ComboBox comboBoxSensitivity;
        private TextBox txtSensitivityInput;
        private Label label3;
    }
}
