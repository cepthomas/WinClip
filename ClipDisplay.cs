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
            pe.Graphics.DrawImage(Clip.Thumbnail, 0, 0);

            //switch (Clip)
            //{
            //    case PlainTextClip cltxt:
            //        pe.Graphics.DrawString(cltxt.ShortText, Font, Brushes.Black, ClientRectangle);
            //        break;

            //    case RtfTextClip clrtf:
            //        pe.Graphics.DrawString(clrtf.ShortText, Font, Brushes.Black, ClientRectangle);
            //        break;

            //    case ImageClip climg:
            //        pe.Graphics.DrawImage(climg.Thumbnail, 0, 0);
            //        break;

            //    default:
            //        pe.Graphics.Clear(Color.SpringGreen);
            //        //_logger.Error("TODO error");
            //        break;
            //}
        }
    }
}
