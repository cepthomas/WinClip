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
        public ClipBase Clip { get; init; }
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClipDisplay(ClipBase clip)
        {
            Clip = clip;
            BorderStyle = BorderStyle.FixedSingle;
            Width = UserSettings.Settings.ClipSize.Width;
            Height = UserSettings.Settings.ClipSize.Height;
        }

        /// <summary>
        /// Draw me.
        /// </summary>
        /// <param name="pe"></param>
        protected override void OnPaint(PaintEventArgs pe)
        {
            pe.Graphics.Clear(BackColor);
            pe.Graphics.DrawImage(Clip.Thumbnail, 0, 0, Clip.Thumbnail.Width, Clip.Thumbnail.Height);
        }
    }
}
