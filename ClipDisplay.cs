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
    public partial class ClipDisplay : UserControl
    {
        #region Properties
        /// <summary>For owner use.</summary>
        public int Id { get; set; } = -1;

        /// <summary>Original clipboard data. TODO? persist clip data.</summary>
        public IDataObject? Data
        {
            get { return _data; }
            set
            {
                _data = value;
                if (value is null) DataType = ClipType.Empty;
                var fmts = value.GetFormats().ToHashSet();
                if (fmts.Contains("System.Drawing.Bitmap")) DataType = ClipType.Bitmap;
                else if (fmts.Contains("Rich Text Format")) DataType = ClipType.RichText;
                else if (fmts.Contains("System.String")) DataType = ClipType.PlainText;
                else DataType = ClipType.Other;
            }
        }
        IDataObject? _data = null;

        /// <summary>Flavor.</summary>
        public ClipType DataType { get; private set; }

        /// <summary>For display.</summary>
        public string? ShortText { get; set; } = null;

        /// <summary>For display.</summary>
        public Image? Thumbnail { get; set; } = null;

        /// <summary>Who sourced it.</summary>
        public string OriginatingApp { get; set; } = "Unknown";

        /// <summary>Who sourced it.</summary>
        public string OriginatingTitle { get; set; } = "Unknown";
        #endregion

        #region Fields
        /// <summary>Assign ids.</summary>
        static int _nextId = 1;

        /// <summary>Used for unspecified states.</summary>
        readonly SolidBrush _defaultForeBrush = new(Color.Black);

        /// <summary>Used for unspecified states.</summary>
        readonly SolidBrush _defaultBackBrush = new(Color.White);
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClipDisplay()
        {
            Size = new Size(200, 50);
            BorderStyle = BorderStyle.FixedSingle;
            Id = _nextId++;
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

        /// <summary>
        /// Draw me.
        /// </summary>
        /// <param name="pe"></param>
        protected override void OnPaint(PaintEventArgs pe)
        {
            // Setup.
            pe.Graphics.Clear(BackColor);

            switch (DataType)
            {
                case ClipType.PlainText:
                case ClipType.RichText:
                    ///SizeF stext = pe.Graphics.MeasureString(ShortText, Font);
                    pe.Graphics.DrawString(ShortText, Font, _defaultForeBrush, ClientRectangle);
                    break;

                case ClipType.Bitmap:
                    pe.Graphics.DrawImage(Thumbnail, 0, 0);
                    break;

                default:
                    pe.Graphics.Clear(Color.SpringGreen);
                    break;
            }
        }

        /// <summary>
        /// Readable contents.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            List<string> ls = [
                $"Id:[{Id} DataType:[{DataType}]",
                $"Data:[{Data}] [{Data.GetHashCode()}]",
                $"From App:[{OriginatingApp}] Title:[{OriginatingTitle}]",
                "Formats:" ];

            Data.GetFormats().ForEach(s => ls.Add("  " + s));

            switch (DataType)
            {
                case ClipType.Bitmap:
                    var bmp = (Bitmap)Data.GetData(typeof(Bitmap));
                    var tn = Thumbnail;
                    if (bmp is null) { ls.Add($"Bitmap IS NULL!!!!!"); }
                    else { ls.Add($"Bitmap W:{bmp.Width} H:{bmp.Height}"); }
                    ls.Add($"Thumbnail W:{tn.Width} H:{tn.Height}");
                    break;

                case ClipType.PlainText:
                    var pt = Data.GetData(typeof(string));
                    ls.Add($"PlainText");
                    break;

                case ClipType.RichText:
                    var rt = Data.GetData(typeof(string));
                    ls.Add($"RichText");
                    break;

                case ClipType.Empty:
                    ls.Add($"Empty");
                    break;

                default:
                    ls.Add($"WTF!!!!");
                    break;
            }

            return string.Join(Environment.NewLine, ls);
        }

        ///// <summary>
        ///// Text specific setup.
        ///// </summary>
        ///// <param name="ctype"></param>
        ///// <param name="text"></param>
        //public void SetText(ClipType ctype, string text)
        //{
        //    const int PEEK_SIZE = 1000;
        //    const int NUM_LINES = 4; // size to fit control

        //    // Show just a part with leading ws removed.
        //    bool more = text.Length > PEEK_SIZE;
        //    var s = text.Left(PEEK_SIZE);
        //    var ls = s.SplitByTokens("\r\n");
        //    more |= ls.Count > NUM_LINES;
        //    StringBuilder sb = new();
        //    for (int i = 0; i < Math.Min(ls.Count, more ? NUM_LINES-1 : NUM_LINES); i++)
        //    {
        //        sb.AppendLine(ls[i]);
        //    }
        //    if (more)
        //    {
        //        sb.AppendLine("...");
        //    }
        //}
    }
}
