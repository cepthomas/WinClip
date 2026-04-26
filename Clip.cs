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

        /// <summary>For rendering. Set this before creating clips!</summary>
        public static Size DrawArea { get; set; }
        #endregion

        #region Fields
        /// <summary>Assign ids.</summary>
        static int _nextId = 1;

        /// <summary>Original formats supported.</summary>
        protected List<string> _formats = [];
        #endregion

        /// <summary>Constructor</summary>
        protected ClipBase()
        {
            Id = _nextId++;

            // Default big X.
            Thumbnail = new(DrawArea.Width, DrawArea.Height);
            using (Graphics gr = Graphics.FromImage(Thumbnail))
            {
                Pen pen = new(Color.Purple, 4);
                int pad = 8;
                gr.DrawLine(pen, pad, pad, DrawArea.Width - pad, DrawArea.Height - pad);
                gr.DrawLine(pen, pad, DrawArea.Height - pad, DrawArea.Width - pad, pad);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rtf"></param>
        /// <returns></returns>
        protected void RenderRtf(string rtf)
        {
            using RichTextBox rtb = new();
            rtb.Rtf = rtf;
            rtb.Size = DrawArea;
            Thumbnail = new Bitmap(DrawArea.Width, DrawArea.Height);
            rtb.DrawToBitmap(Thumbnail, new Rectangle(0, 0, DrawArea.Width, DrawArea.Height));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        protected void RenderText(string text)
        {
            using RichTextBox rtb = new();
            rtb.Text = text;
            rtb.Size = DrawArea;
            Thumbnail = new Bitmap(DrawArea.Width, DrawArea.Height);
            rtb.DrawToBitmap(Thumbnail, new Rectangle(0, 0, DrawArea.Width, DrawArea.Height));
        }

        protected string RtfToText(string rtf)
        {
            using RichTextBox rtb = new();
            rtb.Rtf = rtf;
            return rtb.Text;
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
    }

    /// <summary>Plain text.</summary>
    [Serializable]
    public class PlainTextClip : ClipBase
    {
        //PlainTextClip Formats:[System.String|UnicodeText|Text]

        #region Properties
        /// <summary>Actual content.</summary>
        public string Data { get; private set; }

        ///// <summary>For display.</summary>
        //public string ShortText { get; private set; }
        #endregion

        #region Fields
        public const string TYPE_NAME = "System.String";
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data"></param>
        public PlainTextClip(IDataObject data)
        {
            Data = (string)data.GetData(TYPE_NAME);
            //ShortText = Data.Left(80); //TODO configurable or calculated
            _formats = [.. data.GetFormats()];
        }

        /// <inheritdoc />
        public override string Format()
        {
            List<string> ls = [
                $"PlainTextClip:[{Id}]",
                $"Formats:[{string.Join("|", _formats)}]",
                $"Text:[{Data.Left(80)}]",
            ];
            return string.Join(Environment.NewLine + "  ", ls);
        }

        /// <inheritdoc />
        public override IDataObject? ToData()
        {
            var dobj = new DataObject(Data);
            return dobj;
        }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"PlainTextClip:[{Id}] Text:[{Data.Left(80)}]";
        }
    }

    /// <summary>RTF text.</summary>
    [Serializable]
    public class RtfTextClip : ClipBase
    {
        //RtfTextClip: Formats:[Rich Text Format|Rich Text Format Without Objects|RTF As Text|System.String|UnicodeText|Text|RichEdit Binary|EnterpriseDataProtectionId]

        #region Properties
        /// <summary>Actual content.</summary>
        public string Data { get; private set; }

        ///// <summary>For display.</summary>
        //public string ShortText { get; private set; }
        #endregion

        #region Fields
        public const string TYPE_NAME = "Rich Text Format";
        public string _shortText;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data"></param>
        public RtfTextClip(IDataObject data)
        {
            Data = (string)data.GetData(TYPE_NAME);
            _shortText = RtfToText(Data).Left(80); //TODO configurable or calculated
            _formats = [.. data.GetFormats()];
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
            return new DataObject(Data);
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
        //ImageClip: Formats:[Preferred DropEffect|System.Drawing.Bitmap|Bitmap|PNG]

        #region Properties
        /// <summary>Actual content.</summary>
        public Bitmap Data { get; private set; }
        #endregion

        #region Fields
        public const string TYPE_NAME = "System.Drawing.Bitmap";
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data"></param>
        public ImageClip(IDataObject data)
        {
            Data = (Bitmap)data.GetData(typeof(Bitmap));
            // Make a thumbnail scaled to available real estate.
            int tnWidth = DrawArea.Width * DrawArea.Height / Data.Height;
            int tnHeight = DrawArea.Height;
            Thumbnail = Data.Resize(tnWidth, tnHeight);
            _formats = data.GetFormats().ToList();
        }

        /// <inheritdoc />
        public override string Format()
        {
            List<string> ls = [
                $"ImageClip:[{Id}]",
                $"Formats:[{string.Join("|", _formats)}]",
                $"Bitmap W:{Data.Width} H:{Data.Height}",
            ];
            return string.Join(Environment.NewLine + "  ", ls);
        }

        /// <inheritdoc />
        public override IDataObject? ToData()
        {
            return new DataObject(Data);
        }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"ImageClip:[{Id}] W:{Data.Width} H:{Data.Height}";
        }
    }

    /// <summary>Could be unknown/empty/unsupported TODO.</summary>
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
