using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace BootImgProfiler
{
    public sealed class MainForm : Form
    {
        // Images searched inside the selected folder, in priority order.
        private static readonly string[] TargetNames =
            { "vendor_boot.img", "boot.img", "xbl_config.img" };

        private readonly TextBox _pathBox;
        private readonly Button _browseButton;
        private readonly Button _startButton;
        private readonly TextBox _iomemBox;
        private readonly Button _iomemBrowseButton;
        private readonly TextBox _output;

        public MainForm()
        {
            Text = "Boot Image Profiler";
            Width = 760;
            Height = 520;
            MinimumSize = new Size(600, 380);
            Font = new Font("Segoe UI", 9f);
            StartPosition = FormStartPosition.CenterScreen;

            var pathLabel = new Label
            {
                Text = "Folder:",
                Left = 12,
                Top = 15,
                Width = 50,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _pathBox = new TextBox
            {
                Left = 66,
                Top = 12,
                Width = 560,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true
            };

            _browseButton = new Button
            {
                Text = "Select folder…",
                Left = 636,
                Top = 10,
                Width = 108,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _browseButton.Click += OnBrowse;

            _startButton = new Button
            {
                Text = "Start",
                Left = 636,
                Top = 44,
                Width = 108,
                Height = 30,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Enabled = false
            };
            _startButton.Click += OnStart;

            var iomemLabel = new Label
            {
                Text = "/proc/iomem\n(optional):",
                Left = 12,
                Top = 47,
                Width = 50,
                Height = 32,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _iomemBox = new TextBox
            {
                Left = 66,
                Top = 50,
                Width = 452,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true
            };

            _iomemBrowseButton = new Button
            {
                Text = "Load .txt…",
                Left = 524,
                Top = 48,
                Width = 100,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _iomemBrowseButton.Click += OnBrowseIomem;

            _output = new TextBox
            {
                Left = 12,
                Top = 86,
                Width = 732,
                Height = 386,
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
            Controls.Add(iomemLabel);
            Controls.Add(_iomemBox);
            Controls.Add(_iomemBrowseButton);
            Controls.Add(_output);
        }

        private void OnBrowse(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select the folder that contains the .img files";
                dlg.ShowNewFolderButton = false;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _pathBox.Text = dlg.SelectedPath;
                    _startButton.Enabled = true;
                    _output.Clear();
                }
            }
        }

        private void OnBrowseIomem(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select a saved /proc/iomem text dump";
                dlg.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    _iomemBox.Text = dlg.FileName;
            }
        }

        private void OnStart(object sender, EventArgs e)
        {
            string folder = _pathBox.Text;
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                MessageBox.Show(this, "Please select a valid folder first.",
                    "No folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _startButton.Enabled = false;
            Cursor = Cursors.WaitCursor;
            var sb = new StringBuilder();
            try
            {
                sb.AppendLine("Folder: " + folder);
                sb.AppendLine(new string('-', 68));

                string chosenKernel = "";
                string chosenKernelFrom = "";
                string chosenPhys = "";
                string chosenPhysFrom = "";
                int scanned = 0;

                foreach (string name in TargetNames)
                {
                    string full = FindFile(folder, name);
                    if (full == null)
                    {
                        sb.AppendLine("[skip] " + name + " : not in folder");
                        continue;
                    }

                    scanned++;
                    BootImageInfo info;
                    try
                    {
                        info = BootImage.Analyze(full);
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine("[err ] " + name + " : " + ex.Message);
                        continue;
                    }

                    sb.AppendLine("[file] " + info.FileName +
                                  "  (" + info.ImageKind + ", header v" + info.HeaderVersion + ")");

                    // kernel
                    if (info.HasKernelAddr)
                    {
                        string raw = "0x" + info.RawKernelAddr.ToString("x8");
                        if (info.KernelIsAbsolute)
                        {
                            sb.AppendLine("        kernel_addr = " + raw + "  (absolute -> usable)");
                            if (chosenKernel == "")
                            {
                                chosenKernel = raw;
                                chosenKernelFrom = info.FileName + " " + info.KernelSource;
                            }
                        }
                        else
                        {
                            sb.AppendLine("        kernel_addr = " + raw +
                                          "  (offset placeholder, not an absolute load)");
                        }
                    }
                    else
                    {
                        sb.AppendLine("        kernel_addr : " + info.KernelSource);
                    }

                    // phys_offset
                    if (info.PhysOffset.HasValue)
                    {
                        string po = "0x" + info.PhysOffset.Value.ToString("x8");
                        sb.AppendLine("        phys_offset = " + po +
                                      "  (" + info.PhysOffsetSource + ")");
                        if (chosenPhys == "")
                        {
                            chosenPhys = po;
                            chosenPhysFrom = info.FileName;
                        }
                    }
                    else
                    {
                        sb.AppendLine("        phys_offset : " + info.PhysOffsetSource);
                    }

                    if (!string.IsNullOrEmpty(info.Notes))
                        sb.AppendLine("        note: " + info.Notes);

                    sb.AppendLine();
                }

                if (scanned == 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("None of boot.img / vendor_boot.img / xbl_config.img " +
                                  "were found in this folder.");
                }

                // /proc/iomem dump, if provided, is authoritative and overrides
                // whatever (placeholder) values the images produced.
                if (!string.IsNullOrEmpty(_iomemBox.Text) && File.Exists(_iomemBox.Text))
                {
                    sb.AppendLine();
                    sb.AppendLine("[iomem] " + _iomemBox.Text);
                    try
                    {
                        IomemResult iomem = IomemParser.Parse(_iomemBox.Text);
                        if (iomem.SystemRamBase.HasValue)
                        {
                            chosenPhys = "0x" + iomem.SystemRamBase.Value.ToString("x8");
                            chosenPhysFrom = "/proc/iomem: System RAM";
                            sb.AppendLine("        System RAM  = " + chosenPhys);
                        }
                        else
                        {
                            sb.AppendLine("        System RAM  : not found in dump");
                        }

                        if (iomem.KernelCodeBase.HasValue)
                        {
                            chosenKernel = "0x" + iomem.KernelCodeBase.Value.ToString("x8");
                            chosenKernelFrom = "/proc/iomem: Kernel code";
                            sb.AppendLine("        Kernel code = " + chosenKernel);
                        }
                        else
                        {
                            sb.AppendLine("        Kernel code : not found in dump");
                        }
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine("        parse failed: " + ex.Message);
                    }
                }

                sb.AppendLine(new string('-', 68));
                sb.AppendLine("RESULT");
                sb.AppendLine("  p0_kernel_phys_load = " +
                    (chosenKernel != "" ? chosenKernel + "   [" + chosenKernelFrom + "]"
                                        : "(not found in these images)"));
                sb.AppendLine("  p0_phys_offset      = " +
                    (chosenPhys != "" ? chosenPhys + "   [" + chosenPhysFrom + "]"
                                      : "(not found in these images)"));

                // Write profile.json into the selected folder.
                string outPath = Path.Combine(folder, "profile.json");
                string json =
                    "{" + Environment.NewLine +
                    "  \"p0_phys_offset\": \"" + chosenPhys + "\"," + Environment.NewLine +
                    "  \"p0_kernel_phys_load\": \"" + chosenKernel + "\"" + Environment.NewLine +
                    "}" + Environment.NewLine;
                File.WriteAllText(outPath, json, new UTF8Encoding(false));
                sb.AppendLine();
                sb.AppendLine("Wrote: " + outPath);

                if (chosenKernel == "" || chosenPhys == "")
                {
                    sb.AppendLine();
                    sb.AppendLine("Missing values are not stored in these images on GKI/" +
                                  "Qualcomm devices. Get them from a rooted device:");
                    sb.AppendLine("  adb shell su -c \"cat /proc/iomem\"");
                    sb.AppendLine("  System RAM start -> phys_offset ; Kernel code start -> kernel_phys_load");
                }
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

        // Case-insensitive lookup of a file name inside a folder.
        private static string FindFile(string folder, string name)
        {
            string direct = Path.Combine(folder, name);
            if (File.Exists(direct)) return direct;
            foreach (string f in Directory.GetFiles(folder))
                if (string.Equals(Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase))
                    return f;
            return null;
        }
    }
}
