using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinFormsRichBoxDemo
{
    /// <summary>
    /// A flat, rounded-corner button with hover/press color states.
    /// Reusable on any form: drop an instance on the designer surface
    /// (or add it in code, as MainForm does) and tweak the Color
    /// properties / CornerRadius to restyle it.
    /// </summary>
    public class CustomButton : Button
    {
        private int _cornerRadius = 8;
        private Color _normalColor = Color.FromArgb(37, 99, 235);   // blue
        private Color _hoverColor = Color.FromArgb(29, 78, 216);
        private Color _pressColor = Color.FromArgb(23, 63, 175);

        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = value; Invalidate(); }
        }

        public Color NormalColor
        {
            get { return _normalColor; }
            set { _normalColor = value; BackColor = value; Invalidate(); }
        }

        public Color HoverColor
        {
            get { return _hoverColor; }
            set { _hoverColor = value; }
        }

        public Color PressColor
        {
            get { return _pressColor; }
            set { _pressColor = value; }
        }

        public CustomButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = _normalColor;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold);
            Cursor = Cursors.Hand;
            Height = 36;
            MinimumSize = new Size(90, 32);

            MouseEnter += (s, e) => BackColor = _hoverColor;
            MouseLeave += (s, e) => BackColor = _normalColor;
            MouseDown += (s, e) => BackColor = _pressColor;
            MouseUp += (s, e) => BackColor = _hoverColor;
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var path = RoundedRect(ClientRectangle, _cornerRadius))
            using (var brush = new SolidBrush(BackColor))
            {
                Region = new Region(path);
                g.FillPath(brush, path);
            }

            TextRenderer.DrawText(
                g,
                Text,
                Font,
                ClientRectangle,
                ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;

            if (d <= 0 || d >= Math.Min(bounds.Width, bounds.Height))
            {
                path.AddRectangle(bounds);
                return path;
            }

            var arc = new Rectangle(bounds.X, bounds.Y, d, d);
            path.AddArc(arc, 180, 90);

            arc.X = bounds.Right - d;
            path.AddArc(arc, 270, 90);

            arc.Y = bounds.Bottom - d;
            path.AddArc(arc, 0, 90);

            arc.X = bounds.X;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}
