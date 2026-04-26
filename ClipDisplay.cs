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
    [ToolboxItem(false), Browsable(false)] // not useable in designer
    public class ClipDisplay : UserControl
    {
        #region Properties
        /// <summary>What to show.</summary>
        public ClipBase Clip { get { return _clip; } }
        #endregion

        #region Fields
        /// <summary>The clip.</summary>
        ClipBase _clip;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClipDisplay(ClipBase clip)
        {
            _clip = clip;
            BorderStyle = BorderStyle.FixedSingle;
        }

        /// <summary>
        /// Draw me.
        /// </summary>
        /// <param name="pe"></param>
        protected override void OnPaint(PaintEventArgs pe)
        {
            pe.Graphics.Clear(BackColor);

            switch (clipd.Clip)
            {
                case TextClip cltxt:
                    pe.Graphics.DrawString(cltxt.ShortText, Font, Brushes.Black, ClientRectangle);
                    break;

                case ImageClip climg:
                    var bmp = climg.BitmapData;
                    // remove from list.
                    Controls.Remove(clipd);
                    _clips.Remove(clipd);
                    // Push into sys clipboard which will move it to the top.
                    _suppressClipboardUpdate = true;
                    Clipboard.SetData(ImageClip.IMG_TYPE_NAME, bmp);
                    break;

                default:
                    _logger.Error("TODO error");
                    break;
            }




            switch (_clip.DataType)
            {
                case ClipType.PlainText:
                case ClipType.RichText:
                    pe.Graphics.DrawString(_clip.ShortText, Font, Brushes.Black, ClientRectangle);
                    break;

                case ClipType.Bitmap:
                    pe.Graphics.DrawImage(_clip.Thumbnail, 0, 0);
                    break;

                default:
                    pe.Graphics.Clear(Color.SpringGreen);
                    break;
            }
        }
    }
}
