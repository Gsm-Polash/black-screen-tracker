using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsRichBoxDemo
{
    /// <summary>
    /// Demo form: a white RichTextBox log (entries written bold + black)
    /// plus a custom-styled "Root" button and a couple of sample actions.
    /// </summary>
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            // RichTextBox styling: white background, bold black text.
            richTextBoxLog.BackColor = Color.White;
            richTextBoxLog.ForeColor = Color.Black;
            richTextBoxLog.Font = new Font("Consolas", 9.75f, FontStyle.Bold);
            richTextBoxLog.ReadOnly = true;

            btnRoot.Text = "Root";
            btnRoot.NormalColor = Color.FromArgb(220, 38, 38);   // red = privileged action
            btnRoot.HoverColor = Color.FromArgb(185, 28, 28);
            btnRoot.PressColor = Color.FromArgb(153, 27, 27);
            btnRoot.Click += BtnRoot_Click;

            btnAddLog.Text = "Add Log";
            btnAddLog.Click += BtnAddLog_Click;

            btnClear.Text = "Clear";
            btnClear.NormalColor = Color.FromArgb(107, 114, 128);
            btnClear.HoverColor = Color.FromArgb(75, 85, 99);
            btnClear.PressColor = Color.FromArgb(55, 65, 81);
            btnClear.Click += (s, e) => richTextBoxLog.Clear();

            AppendLog("Application started.");
        }

        private void BtnRoot_Click(object sender, EventArgs e)
        {
            AppendLog("Root button clicked — running privileged action...", Color.DarkRed);
        }

        private void BtnAddLog_Click(object sender, EventArgs e)
        {
            AppendLog("Sample log entry #" + (++_logCounter));
        }

        private int _logCounter;

        /// <summary>
        /// Appends one bold log line to the RichTextBox.
        /// Pass a color to highlight a specific entry (e.g. errors);
        /// the RichTextBox background itself always stays white.
        /// </summary>
        private void AppendLog(string message, Color? color = null)
        {
            richTextBoxLog.SelectionStart = richTextBoxLog.TextLength;
            richTextBoxLog.SelectionLength = 0;
            richTextBoxLog.SelectionColor = color ?? Color.Black;
            richTextBoxLog.SelectionFont = new Font(richTextBoxLog.Font, FontStyle.Bold);
            richTextBoxLog.AppendText(string.Format("[{0:HH:mm:ss}] {1}{2}", DateTime.Now, message, Environment.NewLine));
            richTextBoxLog.SelectionColor = richTextBoxLog.ForeColor;
            richTextBoxLog.ScrollToCaret();
        }
    }
}
