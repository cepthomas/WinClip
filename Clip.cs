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
    /// <summary>Abstract base class for all clips.</summary>
    [Serializable]
    public abstract class ClipBase
    {
        #region Properties
        /// <summary>Keep track of things.</summary>
        public int Id { get;  } = -1;

        /// <summary>For display.</summary>
        public Bitmap Thumbnail { get; protected set; }
        #endregion

        #region Fields
        /// <summary>Assign ids.</summary>
        static int _nextId = 1;

        /// <summary>Clipboard formats supported.</summary>
        protected List<string> _formats = [];
        #endregion

        /// <summary>Constructor</summary>
        protected ClipBase()
        {
            Id = _nextId++;

            // Default.
            Thumbnail = new(UserSettings.Settings.ClipSize.Width, UserSettings.Settings.ClipSize.Height);
            using Graphics gr = Graphics.FromImage(Thumbnail);
            gr.Clear(Color.LightYellow);
        }

        #region Abstract functions
        /// <summary>
        /// Readable contents with detail.
        /// </summary>
        /// <returns></returns>
        public abstract string Format();

        /// <summary>
        /// Convert to clipboard format.
        /// </summary>
        /// <returns></returns>
        public abstract IDataObject? ToData();
        #endregion

        #region Conversion utilities TODO put in nbot/nbui?
        /// <summary>
        /// Make a bitmap from text.
        /// </summary>
        /// <param name="rtf"></param>
        protected void RenderRtf(string rtf)
        {
            using RichTextBox rtb = new()
            {
                BorderStyle = BorderStyle.None,
                Rtf = rtf,
                Size = UserSettings.Settings.ClipSize,
                ScrollBars = RichTextBoxScrollBars.None
            };
            Thumbnail = new Bitmap(rtb.Width, rtb.Height);
            rtb.DrawToBitmap(Thumbnail, new Rectangle(0, 0, rtb.Width, rtb.Height));
        }

        /// <summary>
        /// Make a bitmap from text.
        /// </summary>
        /// <param name="text"></param>
        protected void RenderText(string text)
        {
            using RichTextBox rtb = new()
            {
                BorderStyle = BorderStyle.None,
                Text = text,
                Size = UserSettings.Settings.ClipSize,
                Font = UserSettings.Settings.DisplayFont,
                ScrollBars = RichTextBoxScrollBars.None
            };
            Thumbnail = new Bitmap(rtb.Width, rtb.Height);
            rtb.DrawToBitmap(Thumbnail, new Rectangle(0, 0, rtb.Width, rtb.Height));
        }

        /// <summary>
        /// Extract plain text from rtf.
        /// </summary>
        /// <param name="rtf"></param>
        /// <returns></returns>
        protected string RtfToText(string rtf)
        {
            using RichTextBox rtb = new()
            {
                Rtf = rtf
            };
            return rtb.Text;
        }
        #endregion
    }

    /// <summary>Plain text.</summary>
    [Serializable]
    public class PlainTextClip : ClipBase
    {
        #region Properties
        /// <summary>Actual content.</summary>
        public string Content { get; private set; }
        #endregion

        #region Fields
        // Formats: System.String, UnicodeText, Text
        public const string TYPE_NAME = "System.String";
        public string _shortText;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data"></param>
        public PlainTextClip(IDataObject data)
        {
            var sdata = data.GetData(TYPE_NAME);
            Content = (string)sdata!;
            _formats = [.. data.GetFormats()];
            _shortText = Content.Left(UserSettings.Settings.ShortTextLen);
            RenderText(_shortText);
            
            Thumbnail.Save("pt.png");
        }

        /// <inheritdoc />
        public override string Format()
        {
            List<string> ls = [
                $"PlainTextClip:[{Id}]",
                $"Formats:[{string.Join("|", _formats)}]",
                $"Text:[{_shortText}]",
            ];
            return string.Join(Environment.NewLine + "  ", ls);
        }

        /// <inheritdoc />
        public override IDataObject? ToData()
        {
            var dobj = new DataObject(Content);
            return dobj;
        }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"PlainTextClip:[{Id}] Text:[{_shortText}]";
        }
    }

    /// <summary>RTF text.</summary>
    [Serializable]
    public class RtfTextClip : ClipBase
    {
        #region Properties
        /// <summary>Actual content.</summary>
        public string Content { get; private set; }
        #endregion

        #region Fields
        // Formats: Rich Text Format, Rich Text Format Without Objects, RTF As Text, System.String,
        //     UnicodeText, Text, RichEdit Binary, EnterpriseDataProtectionId
        public const string TYPE_NAME = "Rich Text Format";
        public string _shortText;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data"></param>
        public RtfTextClip(IDataObject data)
        {
            var sdata = data.GetData(TYPE_NAME);
            Content = (string)sdata!;
            _shortText = RtfToText(Content).Left(UserSettings.Settings.ShortTextLen);
            _formats = [.. data.GetFormats()];
            RenderRtf(Content);
            Thumbnail.Save("rtf.png");
        }

        /// <inheritdoc />
        public override string Format()
        {
            List<string> ls = [
                $"RtfTextClip:[{Id}]",
                $"Formats:[{string.Join("|", _formats)}]",
                $"Text:[{_shortText}]",
            ];
            return string.Join(Environment.NewLine + "  ", ls);
        }

        /// <inheritdoc />
        public override IDataObject? ToData()
        {
            return new DataObject(Content);
        }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"RtfTextClip:[{Id}] Text:[{_shortText}]";
        }
    }

    /// <summary>Image.</summary>
    [Serializable]
    public class ImageClip : ClipBase
    {
        #region Properties
        /// <summary>Actual content.</summary>
        public Bitmap Content { get; private set; }
        #endregion

        #region Fields
        // Formats: Preferred DropEffect, System.Drawing.Bitmap, Bitmap, PNG
        public const string TYPE_NAME = "System.Drawing.Bitmap";
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data"></param>
        public ImageClip(IDataObject data)
        {
            var idata = data.GetData(TYPE_NAME);
            Content = (Bitmap)idata!;
            // Make a thumbnail scaled to available real estate.
            float ratio = (float)UserSettings.Settings.ClipSize.Height / Content.Height;
            int tnWidth = (int)(Content.Width * ratio);
            int tnHeight = UserSettings.Settings.ClipSize.Height;
            Thumbnail = Content.Resize(tnWidth, tnHeight);
            _formats = [.. data.GetFormats()];
        }

        /// <inheritdoc />
        public override string Format()
        {
            List<string> ls = [
                $"ImageClip:[{Id}]",
                $"Formats:[{string.Join("|", _formats)}]",
                $"Bitmap W:{Content.Width} H:{Content.Height}",
            ];
            return string.Join(Environment.NewLine + "  ", ls);
        }

        /// <inheritdoc />
        public override IDataObject? ToData()
        {
            return new DataObject(Content);
        }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"ImageClip:[{Id}] W:{Content.Width} H:{Content.Height}";
        }
    }

    /// <summary>Could be unknown/empty/unsupported.</summary>
    [Serializable]
    public class DefaultClip : ClipBase
    {
        #region Fields
        readonly IDataObject? _data = null;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data"></param>
        public DefaultClip(IDataObject? data)
        {
            _data = data;
            if (_data is not null)
            {
                _formats = [.. _data.GetFormats()];
            }

            // Big X.
            Thumbnail = new(UserSettings.Settings.ClipSize.Width, UserSettings.Settings.ClipSize.Height);
            using Graphics gr = Graphics.FromImage(Thumbnail);
            Pen pen = new(Color.Purple, 4);
            int pad = 8;
            gr.DrawLine(pen, pad, pad, UserSettings.Settings.ClipSize.Width - pad, UserSettings.Settings.ClipSize.Height - pad);
            gr.DrawLine(pen, pad, UserSettings.Settings.ClipSize.Height - pad, UserSettings.Settings.ClipSize.Width - pad, pad);
        }

        /// <inheritdoc />
        public override string Format()
        {
            List<string> ls = [
                $"DefaultClip {Id}",
                $"Formats:[{string.Join("|", _formats)}]" ];
            return string.Join(Environment.NewLine + "  ", ls);
        }

        /// <inheritdoc />
        public override IDataObject? ToData()
        {
            return _data;
        }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"DefaultClip:[{Id}]";
        }
    }
}
