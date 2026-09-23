using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace BootImgProfiler
{
    public sealed class MainForm : Form
    {
        private readonly TextBox _pathBox;
        private readonly Button _browseButton;
        private readonly Button _startButton;
        private readonly TextBox _output;

        public MainForm()
        {
            Text = "Boot Image Profiler";
            Width = 720;
            Height = 480;
            MinimumSize = new Size(560, 360);
            Font = new Font("Segoe UI", 9f);
            StartPosition = FormStartPosition.CenterScreen;

            var pathLabel = new Label
            {
                Text = "boot.img:",
                Left = 12,
                Top = 15,
                Width = 60,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _pathBox = new TextBox
            {
                Left = 78,
                Top = 12,
                Width = 500,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true
            };

            _browseButton = new Button
            {
                Text = "Select file…",
                Left = 588,
                Top = 10,
                Width = 100,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _browseButton.Click += OnBrowse;

            _startButton = new Button
            {
                Text = "Start",
                Left = 588,
                Top = 44,
                Width = 100,
                Height = 30,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Enabled = false
            };
            _startButton.Click += OnStart;

            _output = new TextBox
            {
                Left = 12,
                Top = 84,
                Width = 676,
                Height = 344,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 9.5f),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                         AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };

            Controls.Add(pathLabel);
            Controls.Add(_pathBox);
            Controls.Add(_browseButton);
            Controls.Add(_startButton);
            Controls.Add(_output);
        }

        private void OnBrowse(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select a boot image";
                dlg.Filter = "Boot images (*.img)|*.img|All files (*.*)|*.*";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _pathBox.Text = dlg.FileName;
                    _startButton.Enabled = true;
                    _output.Clear();
                }
            }
        }

        private void OnStart(object sender, EventArgs e)
        {
            string path = _pathBox.Text;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                MessageBox.Show(this, "Please select a valid boot.img first.",
                    "No file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _startButton.Enabled = false;
            Cursor = Cursors.WaitCursor;
            var sb = new StringBuilder();
            try
            {
                BootImageInfo info = BootImage.Analyze(path);

                string kernel = info.KernelPhysLoad != 0
                    ? "0x" + info.KernelPhysLoad.ToString("x8")
                    : "";
                string physOffset = info.PhysOffset.HasValue
                    ? "0x" + info.PhysOffset.Value.ToString("x8")
                    : "";

                sb.AppendLine("File            : " + path);
                sb.AppendLine("Header version  : " + info.HeaderVersion);
                sb.AppendLine("Page size       : " + info.PageSize);
                sb.AppendLine();
                sb.AppendLine("p0_kernel_phys_load : " +
                    (kernel != "" ? kernel : "(not available)"));
                sb.AppendLine("    source          : boot header kernel_addr field");
                sb.AppendLine();
                sb.AppendLine("p0_phys_offset      : " +
                    (physOffset != "" ? physOffset : "(not available)"));
                sb.AppendLine("    source          : " + info.PhysOffsetSource);

                if (!string.IsNullOrEmpty(info.Notes))
                {
                    sb.AppendLine();
                    sb.AppendLine("Note: " + info.Notes);
                }

                // Write profile.json next to the boot image.
                string outPath = Path.Combine(
                    Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".",
                    "profile.json");
                string json =
                    "{" + Environment.NewLine +
                    "  \"p0_phys_offset\": \"" + physOffset + "\"," + Environment.NewLine +
                    "  \"p0_kernel_phys_load\": \"" + kernel + "\"" + Environment.NewLine +
                    "}" + Environment.NewLine;
                File.WriteAllText(outPath, json, new UTF8Encoding(false));

                sb.AppendLine();
                sb.AppendLine("Wrote: " + outPath);
            }
            catch (Exception ex)
            {
                sb.AppendLine("ERROR: " + ex.Message);
            }
            finally
            {
                _output.Text = sb.ToString();
                Cursor = Cursors.Default;
                _startButton.Enabled = true;
            }
        }
    }
}
