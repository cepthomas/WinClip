using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Printing;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace ClipboardEx
{
    /// <summary>
    /// One selectable clip item.
    /// </summary>
    public partial class ClipDisplay : UserControl
    {
        /// <summary>For owner use.</summary>
        public int Id { get; set; } = -1;

        #region Events
        /// <summary>Tell the boss.</summary>
        public event EventHandler<ClipEventArgs>? ClipRequest;

        public enum ClipRequestType { Click, DoubleClick }

        public class ClipEventArgs(ClipRequestType ce) : EventArgs
        {
            public ClipRequestType EventType { get; private set; } = ce;
        }
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClipDisplay()
        {
            InitializeComponent();

            rtbText.Dock = DockStyle.Fill;
            rtbText.ScrollBars = RichTextBoxScrollBars.Horizontal;
            rtbText.WordWrap = false;

            picImage.Dock = DockStyle.Fill;

            // Intercept UI.
            picImage.Click += Control_Click;
            rtbText.Click += Control_Click;
        }

        /// <summary>
        /// User control event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Control_Click(object? sender, EventArgs e)
        {
            ClipRequest?.Invoke(this, new ClipEventArgs(ClipRequestType.Click));
        }

        /// <summary>
        /// Text specific setup.
        /// </summary>
        /// <param name="ctype"></param>
        /// <param name="text"></param>
        public void SetText(ClipType ctype, string text)
        {
            const int PEEK_SIZE = 1000;
            const int NUM_LINES = 4; // size to fit control

            // Show just a part with leading ws removed.
            bool more = text.Length > PEEK_SIZE;
            var s = text.Left(PEEK_SIZE);
            var ls = s.SplitByTokens("\r\n");
            more |= ls.Count > NUM_LINES;
            StringBuilder sb = new();
            for (int i = 0; i < Math.Min(ls.Count, more ? NUM_LINES-1 : NUM_LINES); i++)
            {
                sb.AppendLine(ls[i]);
            }
            if (more)
            {
                sb.AppendLine("...");
            }

            picImage.Hide();
            rtbText.Show();
            rtbText.Clear();

            if(ctype == ClipType.RichText) // TODO ideally make a bmp of the rt - for now just colorize it to indicate.
            {
                var tcol = rtbText.SelectionColor;
                rtbText.SelectionColor = Color.Red;
                rtbText.AppendText(sb.ToString());
                rtbText.SelectionColor = tcol;
            }
            else
            {
                rtbText.Text = sb.ToString();
            }
        }

        /// <summary>
        /// Image specific setup.
        /// </summary>
        /// <param name="bmp">The image.</param>
        /// <param name="fit">Fit or clip.</param>
        public void SetImage(Bitmap bmp, bool fit)
        {
            picImage.Show();
            rtbText.Hide();
            picImage.Image = fit ? bmp.Resize(Width, Height) : bmp;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        public void SetOther(string text)
        {
            picImage.Show();
            rtbText.Hide();
            rtbText.Text = text;
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetEmpty()
        {
            picImage.Hide();
            rtbText.Show();
            rtbText.Text = "Empty";
        }
    }
}
