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
        readonly ClipBase _clip;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClipDisplay(ClipBase clip)
        {
            _clip = clip;
            BorderStyle = BorderStyle.FixedSingle;
            Width = ClipBase.DrawArea.Width;
            Height = ClipBase.DrawArea.Height;
        }

        /// <summary>
        /// Draw me.
        /// </summary>
        /// <param name="pe"></param>
        protected override void OnPaint(PaintEventArgs pe)
        {
            pe.Graphics.Clear(BackColor);
            //pe.Graphics.DrawRectangle(Pens.Black, 5, 5, 100, 100);
            pe.Graphics.DrawImage(Clip.Rendering, 0, 0, Clip.Rendering.Width, Clip.Rendering.Height);
        }
    }
}
