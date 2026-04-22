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
    public class ClipDisplay : UserControl
    {
        #region Types
        /// <summary>For internal management.</summary>
        public enum ClipType { Empty, PlainText, RichText, Bitmap, Other };
        #endregion

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

            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Draw me.
        /// </summary>
        /// <param name="pe"></param>
        protected override void OnPaint(PaintEventArgs pe)
        {
            pe.Graphics.Clear(BackColor);

            switch (DataType)
            {
                case ClipType.PlainText:
                case ClipType.RichText:
                    pe.Graphics.DrawString(ShortText, Font, Brushes.Black, ClientRectangle);
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
        /// Readable contents with detail.
        /// </summary>
        /// <returns></returns>
        public string Format()
        {
            var dfmts = string.Join("|", Data.GetFormats());

            List<string> ls = [
                $"Clip {Id}",
                $"DataType:[{DataType}]",
                $"Data:[{Data}] [{Data.GetHashCode()}]",
                $"Origin App:[{OriginatingApp}] Title:[{OriginatingTitle}]",
                $"Formats:[{dfmts}]" ];

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
                    break;

                case ClipType.RichText:
                    var rt = Data.GetData(typeof(string));
                    break;

                case ClipType.Empty:
                    ls.Add($"EMPTY!!!!!!");
                    break;

                default:
                    ls.Add($"WTF!!!!!");
                    break;
            }

            return string.Join(Environment.NewLine + "  ", ls);
        }

        /// <summary>
        /// Readable contents.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sid = $"Clip {Id}";
            string sdet;

            switch (DataType)
            {
                case ClipType.Bitmap:
                    var bmp = (Bitmap)Data.GetData(typeof(Bitmap));
                    if (bmp is null) { sdet = $"Bitmap IS NULL!!!!!"; }
                    else { sdet = $"Bitmap W:{bmp.Width} H:{bmp.Height}"; }
                    break;

                case ClipType.PlainText:
                    sdet = $"PlainText";
                    break;

                case ClipType.RichText:
                    sdet = $"RichText";
                    break;

                case ClipType.Empty:
                    sdet = "EMPTY!!!!!!";
                    break;

                default:
                    sdet = "WTF???";
                    break;
            }

            return $"{sid} {sdet}";
        }
    }
}
