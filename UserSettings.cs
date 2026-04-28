using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks;


namespace WinClip
{
    [Serializable]
    public sealed class UserSettings : SettingsCore
    {
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public HotKey HotKey { get; set; } = new();

        [DisplayName("Max Clips")]
        [Description("Max size of clip cache.")]
        public int MaxClips { get; set; } = 20;

        [DisplayName("Clip Size")]
        [Description("Size in UI.")]
        public Size ClipSize { get; set; } = new(200, 80);

        [DisplayName("Display Font")]
        [Description("The font to use for plain text clip displays.")]
        [JsonConverter(typeof(JsonFontConverter))]
        public Font DisplayFont { get; set; } = new("Consolas", 12, FontStyle.Regular, GraphicsUnit.Point, 0);

        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;

        [DisplayName("File Log Level")]
        [Description("Log level for UI notification.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Debug;

        // TODO configurable or calculated?
        [Browsable(false)]
        public int ShortTextLen { get; set; } = 60;
    }
}
