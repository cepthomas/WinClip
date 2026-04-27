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
        public Bitmap Rendering { get; protected set; }

        /// <summary>For rendering. Set this global before creating clips!</summary>
        public static Size DrawArea { get; set; }
        #endregion

        #region Fields
        /// <summary>Assign ids.</summary>
        static int _nextId = 1;

        /// <summary>Original formats supported.</summary>
        protected List<string> _formats = [];

        /// <summary>TODO configurable or calculated.</summary>
        protected const int _shortTextLen = 32;
        #endregion

        /// <summary>Constructor</summary>
        protected ClipBase()
        {
            Id = _nextId++;

            // Default.
            Rendering = new(DrawArea.Width, DrawArea.Height);
            using Graphics gr = Graphics.FromImage(Rendering);
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

        #region Conversion utilities
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
                Size = DrawArea
            };
            Rendering = new Bitmap(DrawArea.Width, DrawArea.Height);
            rtb.DrawToBitmap(Rendering, new Rectangle(0, 0, DrawArea.Width, DrawArea.Height));
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
                Size = DrawArea
            };
            Rendering = new Bitmap(DrawArea.Width, DrawArea.Height);
            rtb.DrawToBitmap(Rendering, new Rectangle(0, 0, DrawArea.Width, DrawArea.Height));
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
            Content = (string)data.GetData(TYPE_NAME);
            _formats = [.. data.GetFormats()];
            _shortText = Content.Left(_shortTextLen);
            RenderText(_shortText);
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
            Content = (string)data.GetData(TYPE_NAME);
            _shortText = RtfToText(Content).Left(_shortTextLen);
            _formats = [.. data.GetFormats()];
            RenderRtf(Content);
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
            Content = (Bitmap)data.GetData(TYPE_NAME);
            // Make a thumbnail scaled to available real estate.
            float ratio = (float)DrawArea.Height / Content.Height;
            int tnWidth = (int)(Content.Width * ratio);
            int tnHeight = DrawArea.Height;
            Rendering = Content.Resize(tnWidth, tnHeight);
            //Rendering.Save("out.png");
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
        IDataObject? _data = null;
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
                _formats = _data.GetFormats().ToList();
            }

            // Big X.
            Rendering = new(DrawArea.Width, DrawArea.Height);
            using (Graphics gr = Graphics.FromImage(Rendering))
            {
                Pen pen = new(Color.Purple, 4);
                int pad = 8;
                gr.DrawLine(pen, pad, pad, DrawArea.Width - pad, DrawArea.Height - pad);
                gr.DrawLine(pen, pad, DrawArea.Height - pad, DrawArea.Width - pad, pad);
            }
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
