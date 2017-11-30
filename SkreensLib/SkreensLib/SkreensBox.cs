// Copyright (c) 2012-2017 Skreens Entertainment Technologies Incorporated - http://skreens.com
//
// Redistribution and use in source and binary forms, with or without modification, are
// permitted provided that the following conditions are met:
//
// Redistributions of source code must retain the above copyright notice, this list of
// conditions and the following disclaimer.
//
// Redistributions in binary form must reproduce the above copyright notice, this list of
// conditions and the following disclaimer in the documentation and/or other materials
// provided with the distribution.
//
// Neither the name of Skreens Entertainment Technologies Incorporated nor the names of its
// contributors may be used to endorse or promote products derived from this software without
// specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS
// OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER
// IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
// OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.CrestronWebSocketClient;
using Crestron.SimplSharp.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SkreensLib
{
    public delegate void WebSocketOnConnectCallback(short error);
    public delegate void WebSocketOnDisconnectCallback(short error);
    public delegate void WebSocketOnSendCallback(short error);
    public delegate void WebSocketOnReceiveCallback(short opcode, short error);

    public struct SkreensLayout
    {
        public int id;
        public string name;
    }

    public enum SkreensKey
    {
        UP = 1,
        DOWN,
        LEFT,
        RIGHT,
        SELECT,
        TEXT_LEFT,
        TEXT_RIGHT,
        TEXT_SELECT,
        EXIT
    }

    public class SkreensWebSocket
    {
        private WebSocketClient wsc;

        public WebSocketOnConnectCallback OnConnect { get; set; }
        public WebSocketOnDisconnectCallback OnDisconnect { get; set; }
        public WebSocketOnSendCallback OnSend { get; set; }
        public WebSocketOnReceiveCallback OnReceive { get; set; }

        public string RxBuffer;

        public SkreensWebSocket()
        {
        }

        public void Connect(string url)
        {
            wsc = new WebSocketClient();
            wsc.URL = url;
            wsc.ConnectionCallBack = ConnectCallback;
            wsc.DisconnectCallBack = DisconnectCallback;
            wsc.SendCallBack = SendCallback;
            wsc.ReceiveCallBack = ReceiveCallback;

            try
            {
                wsc.ConnectAsync();
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine("Exception in Connect: {0}", ex.Message);
            }
        }

        public void Disconnect()
        {
            try
            {
                wsc.DisconnectAsync(null);
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine("Exception in Disconnect: {0}", ex.Message);
            }
        }

        public void Send(string msg)
        {
            try
            {
                byte[] bytes = Encoding.ASCII.GetBytes(msg.ToCharArray());
                wsc.SendAsync(bytes, (uint)bytes.Length, WebSocketClient.WEBSOCKET_PACKET_TYPES.LWS_WS_OPCODE_07__TEXT_FRAME);
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine("Exception in Send: {0}", ex.Message);
            }
        }

        private int ConnectCallback(WebSocketClient.WEBSOCKET_RESULT_CODES error)
        {
            if (OnConnect != null)
                OnConnect((short)error);

            wsc.ReceiveAsync();

            return (int)error;
        }

        private int DisconnectCallback(WebSocketClient.WEBSOCKET_RESULT_CODES error, Object obj)
        {
            if (OnDisconnect != null)
                OnDisconnect((short)error);

            return (int)error;
        }

        private int SendCallback(WebSocketClient.WEBSOCKET_RESULT_CODES error)
        {
            if (OnSend != null)
                OnSend((short)error);

            return (int)error;
        }

        private int ReceiveCallback(byte[] bytes, uint bytesLength, WebSocketClient.WEBSOCKET_PACKET_TYPES opcode, WebSocketClient.WEBSOCKET_RESULT_CODES error)
        {
            if (OnReceive != null)
            {
                try
                {
                    RxBuffer = Encoding.ASCII.GetString(bytes, 0, (int)bytesLength);
                    OnReceive((short)opcode, (short)error);
                }
                catch (Exception ex)
                {
                    CrestronConsole.PrintLine("Exception in ReceiveCallback: {0}", ex.Message);
                }
            }

            if (error == WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SUCCESS)
                wsc.ReceiveAsync();

            return (int)error;
        }
    }

    public class RestClient
    {
        public RestClient()
        {
        }

        public virtual void Initialize()
        {
        }

        protected string GET(string host, string url, string accept, bool debug)
        {
            HttpClient client = new HttpClient();
            client.Accept = accept;

            HttpClientRequest request = new HttpClientRequest();
            request.Url.Parse(String.Format("http://{0}/{1}", host, url));

            if (debug)
                CrestronConsole.PrintLine("GET: url={0}", request.Url.ToString());

            string text = null;

            try
            {
                HttpClientResponse response = client.Dispatch(request);
                text = response.ContentString;
            }
            catch (HttpException e)
            {
                CrestronConsole.PrintLine("Caught HttpException in GET: {0}", e.Message);
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine("Caught Exception in GET: {0}", e.Message);
            }

            return text;
        }

        protected string PUT(string host, string url, string content, string accept, bool debug)
        {
            HttpClient client = new HttpClient();
            client.Accept = accept;

            HttpClientRequest request = new HttpClientRequest();
            request.Url.Parse(String.Format("http://{0}/{1}", host, url));
            request.RequestType = RequestType.Put;
            request.ContentString = content;

            if (debug)
                CrestronConsole.PrintLine("PUT: url={0}, content={1}", request.Url.ToString(), content);

            string text = null;

            try
            {
                HttpClientResponse response = client.Dispatch(request);
                text = response.ContentString;
            }
            catch (HttpException e)
            {
                CrestronConsole.PrintLine("Caught HttpException in PUT: {0}", e.Message);
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine("Caught Exception in PUT: {0}", e.Message);
            }

            return text;
        }

        protected string POST(string host, string url, string content, string accept, bool debug)
        {
            HttpClient client = new HttpClient();
            client.Accept = accept;

            HttpClientRequest request = new HttpClientRequest();
            request.Url.Parse(String.Format("http://{0}/{1}", host, url));
            request.RequestType = RequestType.Post;
            request.ContentString = content;

            if (debug)
                CrestronConsole.PrintLine("POST: url={0}, content={1}", request.Url.ToString(), content);

            string text = null;

            try
            {
                HttpClientResponse response = client.Dispatch(request);
                text = response.ContentString;
            }
            catch (HttpException e)
            {
                CrestronConsole.PrintLine("Caught HttpException in POST: {0}", e.Message);
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine("Caught Exception in POST: {0}", e.Message);
            }

            return text;
        }

        protected string DELETE(string host, string url, string accept, bool debug)
        {
            HttpClient client = new HttpClient();
            client.Accept = accept;

            HttpClientRequest request = new HttpClientRequest();
            request.Url.Parse(String.Format("http://{0}/{1}", host, url));
            request.RequestType = RequestType.Delete;
            //request.ContentString = content;

            if (debug)
                CrestronConsole.PrintLine("DELETE: url={0}", request.Url.ToString());
                //CrestronConsole.PrintLine("DELETE: url={0}, content={1}", request.Url.ToString(), content);

            string text = null;

            try
            {
                HttpClientResponse response = client.Dispatch(request);
                text = response.ContentString;
            }
            catch (HttpException e)
            {
                CrestronConsole.PrintLine("Caught HttpException in DELETE: {0}", e.Message);
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine("Caught Exception in DELETE: {0}", e.Message);
            }

            return text;
        }
    }

    public class SkreensBox : RestClient
    {
        private SkreensWebSocket ws;
        private string host;
        private string wsURL;
        private int[] layoutId;
        
        private bool debug;
        private string debugHost;

        public ushort layoutCount;
        public string[] layoutName;
        public short[] mixerLevels;

        public SkreensBox()
        {
            layoutCount = 0;
            ws = new SkreensWebSocket();
        }

        public void Initialize(string newHost)
        {
            host = newHost;
            debug = false;
            debugHost = newHost + ":8081";

            try
            {
                wsURL = String.Format("ws://{0}/1/sockets", newHost);
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine("Caught exception in Initialize: {0}", ex.Message);
            }
        }

        public void Debug(short enable)
        {
            if (enable == 0)
                debug = false;
            else
                debug = true;

            CrestronConsole.PrintLine("SkreensLib debugging enabled: {0}", debug);
        }

        public void Connect()
        {
            ws.Connect(wsURL);
        }

        public void Disconnect()
        {
            ws.Disconnect();
        }

        public void GetLayouts()
        {
            string text;

            if (debug)
                GET(debugHost, "1/layouts", "application/json", debug);
            
            text = GET(host, "1/layouts", "application/json", debug);

            try
            {
                JArray json = JArray.Parse(text);

                layoutCount = (ushort)json.Count;
                layoutName = new string[layoutCount];
                layoutId = new int[layoutCount];

                int i = 0;

                foreach (JObject obj in json)
                {
                    layoutName[i] = obj.Value<string>("name");
                    layoutId[i] = obj.Value<int>("id");
                    i++;
                }
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine("Caught exception in GetLayouts: {0}", ex.Message);
            }
        }

        public short GetActiveLayout()
        {
            string text;

            if (debug)
                GET(debugHost, "1/window-manager/layouts", "application/json", debug);
            
            text = GET(host, "1/window-manager/layout", "application/json", debug);

            try
            {
                JObject currentLayout = JObject.Parse(text);

                int currentId = currentLayout.Value<int>("id");

                for (int i = 0; i < layoutId.Length; i++)
                {
                    if (layoutId[i] == currentId)
                    {
                        return (short)i;
                    }
                }
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine("Caught exception in GetActiveLayout: {0}", ex.Message);
            }

            return -1;
        }

        public void SelectLayout(ushort n)
        {
            JObject json = new JObject();
            json.Add("id", layoutId[n]);

            string text;

            if (debug)
                PUT(debugHost, "1/window-manager/layout", json.ToString(), "application/json", debug);

            text = PUT(host, "1/window-manager/layout", json.ToString(), "application/json", debug);
            
            if (debug)
                CrestronConsole.PrintLine("SelectLayout: {0}", text);
        }

        public void GetMixerLevels()
        {
            string text;

            if (debug)
                GET(debugHost, "1/audio-config", "application/json", debug);
            
            text = GET(host, "1/audio-config", "application/json", debug);

            if (debug)
                CrestronConsole.PrintLine(text);

            try
            {
                JObject audioConfig = JObject.Parse(text);
                JArray volumeLevels = (JArray)audioConfig["mixed_hdmi_volumes"];

                int i = 0;
                mixerLevels = new short[volumeLevels.Count];

                foreach (int level in volumeLevels)
                    mixerLevels[i++] = (short)level;
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine("Caught Exception in GetMixerLevels: {0}", ex.Message);
            }
        }

        public void SetMixerLevels()
        {
            JArray volumes = new JArray(mixerLevels);
            JObject json = new JObject();
            json.Add("mixed_hdmi_volumes", volumes);

            string text;

            if (debug)
                PUT(debugHost, "1/audio-config", json.ToString(), "application/json", debug);
            
            text = PUT(host, "1/audio-config", json.ToString(), "application/json", debug);
        }

        public void ShowScreen(string screen)
        {
            JObject json = new JObject();
            json.Add("screen", screen);

            string text;

            if (debug)
                POST(debugHost, "1/osd", json.ToString(), "application/json", debug);

            text = POST(host, "1/osd", json.ToString(), "application/json", debug);

            if (debug)
                CrestronConsole.PrintLine("ShowHelpScreen: {0}", text);
        }

        public void Cursor(short key)
        {
            JObject json = new JObject();
            string url = "1/keyboard/control-character";

            switch ((SkreensKey)key)
            {
                case SkreensKey.UP: json.Add("control_character", "up");
                    break;
                case SkreensKey.DOWN: json.Add("control_character", "down");
                    break;
                case SkreensKey.LEFT: json.Add("control_character", "left");
                    break;
                case SkreensKey.RIGHT: json.Add("control_character", "right");
                    break;
                case SkreensKey.SELECT: json.Add("control_character", "return");
                    break;
                case SkreensKey.TEXT_LEFT: json.Add("text", "z"); url = "1/keyboard/text";
                    break;
                case SkreensKey.TEXT_RIGHT: json.Add("text", "x"); url = "1/keyboard/text";
                    break;
                case SkreensKey.TEXT_SELECT: json.Add("text", "f"); url = "1/keyboard/text";
                    break;
                default:
                    json = null;
                    break;
            }

            string text;

            if (json == null)
            {
                if (debug)
                    DELETE(debugHost, "1/osd", "application/json", debug);

                text = DELETE(host, "1/osd", "application/json", debug);
            }
            else
            {
                if (debug)
                    POST(debugHost, url, json.ToString(), "application/json", debug);

                text = POST(host, url, json.ToString(), "application/json", debug);
            }

            if (debug)
                CrestronConsole.PrintLine("Cursor: {0}", text);
        }

        public void Test()
        {
            CrestronConsole.PrintLine("-- MARK --");
        }

        public void WebSocketSend(string msg)
        {
            ws.Send(msg);
        }
    }
}