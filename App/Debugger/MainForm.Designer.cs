namespace Debugger
{
    partial class MainForm
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
            txtData = new TextBox();
            SuspendLayout();
            // 
            // txtData
            // 
            txtData.Dock = DockStyle.Bottom;
            txtData.Location = new Point(0, 427);
            txtData.Name = "txtData";
            txtData.Size = new Size(800, 23);
            txtData.TabIndex = 0;
            txtData.TextChanged += txtData_TextChanged;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(txtData);
            DoubleBuffered = true;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "MainForm";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtData;
    }
}
