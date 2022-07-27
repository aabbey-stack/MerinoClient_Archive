using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace MerinoClient.Features.QoL;

//all credit goes to XSOverlay documentation (manual integration section): https://xiexe.github.io/XSOverlayDocumentation/#/NotificationsAPI?id=c-manual-integration

internal class XSNotificationsLite : FeatureComponent
{
    private const int MaxPort = 42069;
    private static IPEndPoint _endPoint;
    private static Socket _broadcastSocket;

    public XSNotificationsLite()
    {
        var processesByName = Process.GetProcessesByName("XSOverlay");
        if (processesByName.Length != 0)
        {
            if (Main.streamerMode)
            {
                CanNotify = false;
                return;
            }

            var broadcastIp = IPAddress.Parse("127.0.0.1");
            _broadcastSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _endPoint = new IPEndPoint(broadcastIp, MaxPort);
            CanNotify = true;
        }
        else
        {
            CanNotify = false;
        }
    }

    public static bool CanNotify { private set; get; }

    public override string OriginalAuthor => "abbey and んなあぁ";
    public override string FeatureName => GetType().Name;

    public static bool SendNotification(string title, string content, string icon = "default",
        string audioPath = "default",
        float timeout = 3.0f, float height = 175.0f, float opacity = 1.0f, float volume = 0.7f,
        string sourceApp = "MerinoClient")
    {
        try
        {
            if (!CanNotify || !Config.XSIntegration.Value) return false;
            if (!Config.XSIntegrationSound.Value) audioPath = string.Empty;
            var msg = new XSOMessage
            {
                MessageType = 1,
                Title = title,
                Content = content,
                Height = height,
                SourceApp = sourceApp,
                Timeout = timeout,
                Volume = volume,
                AudioPath = audioPath,
                Icon = icon,
                Opacity = opacity
            };
            var byteBuffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg));
            _broadcastSocket.SendTo(byteBuffer, _endPoint);
            return true;
        }
        catch (Exception e)
        {
            MerinoLogger.Error("An exception occurred while trying to send a notification to XSOverlay: " + e);
            return false;
        }
    }


    private struct XSOMessage
    {
        public int
            MessageType { get; set; } // 1 = Notification Popup, 2 = MediaPlayer Information, will be extended later on.

/*
            public int Index { get; set; } //Only used for Media Player, changes the icon on the wrist.
*/
        public float Volume { get; set; } // Notification sound volume.

        public string
            AudioPath
        {
            get;
            set;
        } //File path to .ogg audio file. Can be "default", "error", or "warning". Notification will be silent if left empty.

        public float Timeout { get; set; } //How long the notification will stay on screen for in seconds
        public string Title { get; set; } //Notification title, supports Rich Text Formatting

        public string
            Content
        {
            get;
            set;
        } //Notification content, supports Rich Text Formatting, if left empty, notification will be small.

        public string
            Icon
        {
            get;
            set;
        } //Base64 Encoded image, or file path to image. Can also be "default", "error", or "warning"

        public float
            Height
        {
            get;
            set;
        } //Height notification will expand to if it has content other than a title.Default is 175

        public float
            Opacity { get; set; } //Opacity of the notification, to make it less intrusive.Setting to 0 will set to 1.

/*
            public bool UseBase64Icon { get; set; } //Set to true if using Base64 for the icon image
*/
        public string SourceApp { get; set; } //Somewhere to put your app name for debugging purposes
    }
}