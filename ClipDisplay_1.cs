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


namespace WinClip
{
    /// <summary>
    /// One selectable clip item.
    /// </summary>
    public partial class ClipDisplay_1 : UserControl
    {
        /// <summary>For owner use.</summary>
        public int Id { get; set; } = -1;

        /// <summary>Used for unspecified states.</summary>
        readonly SolidBrush _defaultForeBrush = new(Color.Black);

        /// <summary>Used for unspecified states.</summary>
        readonly SolidBrush _defaultBackBrush = new(Color.White);


        /// <summary>Original clipboard data. TODO persist clip data.</summary>
        public object? Data { get; set; } = null;

        /// <summary>Flavor.</summary>
        public ClipType Ctype { get; set; } = ClipType.Empty;

        /// <summary>For display.</summary>
        public string Text { get; set; } = "";

        /// <summary>For display.</summary>
        public Bitmap? Bitmap { get; set; } = null;

        /// <summary>Who sourced it.</summary>
        public string OriginatingApp { get; set; } = "Unknown";

        /// <summary>Who sourced it.</summary>
        public string OriginatingTitle { get; set; } = "Unknown";
        
        
        /// <summary>
        /// Readable contents.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            List<string> ls = [
                $"Ctype:[{Ctype}]",
                $"Data:[{Data}]",
                $"From App:[{OriginatingApp}]",
                $"From Title:[{OriginatingTitle}]"
                ];

            return string.Join(Environment.NewLine, ls);
        }




        // #region Events
        // /// <summary>Tell the boss.</summary>
        // public event EventHandler<ClipEventArgs>? ClipRequest;

        // public enum ClipRequestType { Click, DoubleClick }

        // public class ClipEventArgs(ClipRequestType ce) : EventArgs
        // {
        //     public ClipRequestType EventType { get; private set; } = ce;
        // }
        // #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClipDisplay_1()
        {
            Size = new Size(200, 50);

            BorderStyle = BorderStyle.FixedSingle;

        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _defaultForeBrush.Dispose();
                _defaultBackBrush.Dispose();
                // _stateTypes.ForEach(st => { st.Value.ForeBrush.Dispose(); st.Value.BackBrush.Dispose(); });
            }
            base.Dispose(disposing);
        }

        // /// <summary>
        // /// User control event.
        // /// </summary>
        // /// <param name="sender"></param>
        // /// <param name="e"></param>
        // void Control_Click(object? sender, EventArgs e)
        // {
        //     ClipRequest?.Invoke(this, new ClipEventArgs(ClipRequestType.Click));
        // }

        protected override void OnPaint(PaintEventArgs pe)
        {
            //// Setup.
            //pe.Graphics.Clear(BackColor);

            // for (int row = 0; row < _rows; row++)
            // {
            //     for (int col = 0; col < _cols; col++)
            //     {
            //         SolidBrush fb = _defaultForeBrush;
            //         SolidBrush bb = _defaultBackBrush;

            //         int ind = row * _cols + col;

            //         if(ind < _indicators.Count)
            //         {
            //             int state = _indicators[ind].State;
            //             if (_stateTypes.ContainsKey(state))
            //             {
            //                 fb = _stateTypes[state].ForeBrush;
            //                 bb = _stateTypes[state].BackBrush;
            //             }

            //             int x = col * _indWidth;
            //             int y = row * _indHeight;
            //             Rectangle r = new(x, y, _indWidth, _indHeight);
            //             pe.Graphics.FillRectangle(bb, r);

            //             // Text
            //             string text = _indicators[ind].Text;
            //             SizeF stext = pe.Graphics.MeasureString(text, Font);
            //             pe.Graphics.DrawString(text, Font, fb, x + 5, y + (_indHeight - stext.Height) / 2);
            //         }
            //     }
            // }
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

            // picImage.Hide();
            // rtbText.Show();
            // rtbText.Clear();

            // if(ctype == ClipType.RichText) // TODO ideally make a bmp of the rt - for now just colorize it to indicate.
            // {
            //     var tcol = rtbText.SelectionColor;
            //     rtbText.SelectionColor = Color.Red;
            //     rtbText.AppendText(sb.ToString());
            //     rtbText.SelectionColor = tcol;
            // }
            // else
            // {
            //     rtbText.Text = sb.ToString();
            // }
        }

        /// <summary>
        /// Image specific setup.
        /// </summary>
        /// <param name="bmp">The image.</param>
        /// <param name="fit">Fit or clip.</param>
        public void SetImage(Bitmap bmp, bool fit)
        {
            // picImage.Show();
            // rtbText.Hide();
            // picImage.Image = fit ? bmp.Resize(Width, Height) : bmp;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        public void SetOther(string text)
        {
            // picImage.Show();
            // rtbText.Hide();
            // rtbText.Text = text;
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetEmpty()
        {
            // picImage.Hide();
            // rtbText.Show();
            // rtbText.Text = "Empty";
        }
    }
}
