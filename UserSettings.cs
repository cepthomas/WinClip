using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Design;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using System.Windows.Forms;


namespace WinClip
{
    [Serializable]
    public sealed class UserSettings : SettingsCore
    {
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public HotKey HotKey { get; set; } = new(); // TODO

        [DisplayName("Max Clips")]
        [Description("Max size of clip cache.")]
        [Browsable(true)]
        public int MaxClips { get; set; } = 20;


        // [DisplayName("Output Device")]
        // [Description("Valid output device.")]
        // [Browsable(true)]
        // [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
        // public string OutputDevice { get; set; } = "???";

        // [DisplayName("Draw Color")]
        // [Description("The color used for active control surfaces.")]
        // [Browsable(true)]
        // [JsonConverter(typeof(JsonColorConverter))]
        // public Color DrawColor { get; set; } = Color.Red;

        // [DisplayName("Selected Color")]
        // [Description("The color used for when control is selected.")]
        // [Browsable(true)]
        // [JsonConverter(typeof(JsonColorConverter))]
        // public Color SelectedColor { get; set; } = Color.Blue;

        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;

        [DisplayName("File Log Level")]
        [Description("Log level for UI notification.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Debug;

        //[Browsable(false)]
        //public bool LogMidi { get; set; } = false;
    }

    [Serializable]
    public sealed class HotKey
    {
        //[DisplayName("Max Clips")]
        //[Description("Max size of clip cache.")]
        //[Browsable(true)]
        public string Key { get; set; } = "?";
        public bool Ctrl { get; set; } = false;
        public bool Alt { get; set; } = false;
        public bool Shift { get; set; } = false;
        public bool Win { get; set; } = false;
    }
}
