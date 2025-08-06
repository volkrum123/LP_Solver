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
            txtFilePath = new TextBox();
            btnLoadFile = new Button();
            btnExportOutput = new Button();
            lstOutput = new ListBox();
            btnSolveSimplex = new Button();
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
            // txtFilePath
            // 
            txtFilePath.Location = new Point(184, 46);
            txtFilePath.Name = "txtFilePath";
            txtFilePath.Size = new Size(428, 27);
            txtFilePath.TabIndex = 1;
            // 
            // btnLoadFile
            // 
            btnLoadFile.Location = new Point(51, 46);
            btnLoadFile.Name = "btnLoadFile";
            btnLoadFile.Size = new Size(94, 29);
            btnLoadFile.TabIndex = 2;
            btnLoadFile.Text = "Load file";
            btnLoadFile.UseVisualStyleBackColor = true;
            // 
            // btnExportOutput
            // 
            btnExportOutput.Location = new Point(371, 94);
            btnExportOutput.Name = "btnExportOutput";
            btnExportOutput.Size = new Size(94, 29);
            btnExportOutput.TabIndex = 3;
            btnExportOutput.Text = "Export";
            btnExportOutput.UseVisualStyleBackColor = true;
            // 
            // lstOutput
            // 
            lstOutput.FormattingEnabled = true;
            lstOutput.Location = new Point(3, 141);
            lstOutput.Name = "lstOutput";
            lstOutput.Size = new Size(785, 304);
            lstOutput.TabIndex = 4;
            // 
            // btnSolveSimplex
            // 
            btnSolveSimplex.Location = new Point(231, 94);
            btnSolveSimplex.Name = "btnSolveSimplex";
            btnSolveSimplex.Size = new Size(94, 29);
            btnSolveSimplex.TabIndex = 5;
            btnSolveSimplex.Text = "Primal";
            btnSolveSimplex.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnSolveSimplex);
            Controls.Add(lstOutput);
            Controls.Add(btnExportOutput);
            Controls.Add(btnLoadFile);
            Controls.Add(txtFilePath);
            Controls.Add(label1);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion


        private Label label1;
        private TextBox txtFilePath;
        private Button btnLoadFile;
        private Button btnExportOutput;
        private ListBox lstOutput;
        private Button btnSolveSimplex;
    }
}
