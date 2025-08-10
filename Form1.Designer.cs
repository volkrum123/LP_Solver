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
            btnParse = new Button();
            txtOutput = new RichTextBox();
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
            txtInput.Location = new Point(184, 46);
            txtInput.Multiline = true;
            txtInput.Name = "txtInput";
            txtInput.ScrollBars = ScrollBars.Vertical;
            txtInput.Size = new Size(400, 150);
            txtInput.TabIndex = 1;
            // 
            // btnParse
            // 
            btnParse.Location = new Point(51, 46);
            btnParse.Name = "btnParse";
            btnParse.Size = new Size(94, 29);
            btnParse.TabIndex = 2;
            btnParse.Text = "Load file";
            btnParse.UseVisualStyleBackColor = true;
            btnParse.Click += btnParse_Click;
            // 
            // txtOutput
            // 
            txtOutput.Font = new Font("Consolas", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtOutput.Location = new Point(60, 223);
            txtOutput.Name = "txtOutput";
            txtOutput.ReadOnly = true;
            txtOutput.Size = new Size(342, 173);
            txtOutput.TabIndex = 3;
            txtOutput.Text = "";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1085, 569);
            Controls.Add(txtOutput);
            Controls.Add(btnParse);
            Controls.Add(txtInput);
            Controls.Add(label1);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion


        private Label label1;
        private TextBox txtInput;
        private Button btnParse;
        private RichTextBox txtOutput;
    }
}
