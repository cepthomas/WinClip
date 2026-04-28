using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinClip
{
    public partial class Dev : Form
    {
        public Dev()
        {
            InitializeComponent();

            Location = new(500, 100);
            //Location = Common.Settings.FormGeometry.Location;
            //Visible = false; // doesn't work - like C:\Dev\Misc\NLab\TrayExForm - use Minimized?

            ///// Init controls.
            tvInfo.BackColor = Color.Cornsilk;
            tvInfo.Matchers =
            [
                new("ERR ", Color.Red),
                new("WRN ", Color.Green),
            ];
            btnClear.Click += (_, __) => tvInfo.Clear();

            lblLetter.Text = UserSettings.Settings.HotKey.Key;

            ///// Debug stuff
            //if (_debug)
            //{
            //    var sdir = MiscUtils.GetSourcePath();
            //    var fn = Path.Combine(sdir, "Test", "ross.txt");
            //    AddClip(new PlainTextClip(new DataObject(File.ReadAllText(fn))));
            //    fn = Path.Combine(sdir, "Test", "ex.rtf");
            //    AddClip(new RtfTextClip(new DataObject(RtfTextClip.TYPE_NAME, (File.ReadAllText(fn)))));
            //    fn = Path.Combine(sdir, "Test", "ex.png");
            //    AddClip(new ImageClip(new DataObject(Bitmap.FromFile(fn))));
            //}


            //// Diagnostics.
            //lblCtrl.BackColor = ctrlKey ? Color.LimeGreen : SystemColors.Control;
            //lblAlt.BackColor = altKey ? Color.LimeGreen : SystemColors.Control;
            //lblWin.BackColor = winKey ? Color.LimeGreen : SystemColors.Control;
            //lblLetter.BackColor = letterPressed ? Color.LimeGreen : SystemColors.Control;
        }

        public void Tell(string s)
        {
            tvInfo.Append(s);
        }
    }
}
