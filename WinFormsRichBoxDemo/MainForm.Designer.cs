namespace WinFormsRichBoxDemo
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.richTextBoxLog = new System.Windows.Forms.RichTextBox();
            this.topPanel = new System.Windows.Forms.Panel();
            this.btnClear = new WinFormsRichBoxDemo.CustomButton();
            this.btnAddLog = new WinFormsRichBoxDemo.CustomButton();
            this.btnRoot = new WinFormsRichBoxDemo.CustomButton();
            this.topPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // richTextBoxLog
            //
            this.richTextBoxLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBoxLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxLog.Location = new System.Drawing.Point(0, 56);
            this.richTextBoxLog.Name = "richTextBoxLog";
            this.richTextBoxLog.Size = new System.Drawing.Size(640, 344);
            this.richTextBoxLog.TabIndex = 0;
            this.richTextBoxLog.Text = "";
            //
            // topPanel
            //
            this.topPanel.Controls.Add(this.btnClear);
            this.topPanel.Controls.Add(this.btnAddLog);
            this.topPanel.Controls.Add(this.btnRoot);
            this.topPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.topPanel.Location = new System.Drawing.Point(0, 0);
            this.topPanel.Name = "topPanel";
            this.topPanel.Padding = new System.Windows.Forms.Padding(12, 10, 12, 10);
            this.topPanel.Size = new System.Drawing.Size(640, 56);
            this.topPanel.TabIndex = 1;
            //
            // btnClear
            //
            this.btnClear.CornerRadius = 8;
            this.btnClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClear.Location = new System.Drawing.Point(232, 10);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(96, 36);
            this.btnClear.TabIndex = 2;
            this.btnClear.UseVisualStyleBackColor = false;
            //
            // btnAddLog
            //
            this.btnAddLog.CornerRadius = 8;
            this.btnAddLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddLog.Location = new System.Drawing.Point(126, 10);
            this.btnAddLog.Name = "btnAddLog";
            this.btnAddLog.Size = new System.Drawing.Size(96, 36);
            this.btnAddLog.TabIndex = 1;
            this.btnAddLog.UseVisualStyleBackColor = false;
            //
            // btnRoot
            //
            this.btnRoot.CornerRadius = 8;
            this.btnRoot.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRoot.Location = new System.Drawing.Point(12, 10);
            this.btnRoot.Name = "btnRoot";
            this.btnRoot.Size = new System.Drawing.Size(104, 36);
            this.btnRoot.TabIndex = 0;
            this.btnRoot.UseVisualStyleBackColor = false;
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(640, 400);
            this.Controls.Add(this.richTextBoxLog);
            this.Controls.Add(this.topPanel);
            this.MinimumSize = new System.Drawing.Size(480, 320);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "RichTextBox + Custom Button Demo";
            this.topPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.RichTextBox richTextBoxLog;
        private System.Windows.Forms.Panel topPanel;
        private CustomButton btnRoot;
        private CustomButton btnAddLog;
        private CustomButton btnClear;
    }
}
