#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityWebSocket;

namespace ET
{
    public class WChannel: AChannel
    {
        private readonly WService Service;
        
        private WebSocket webSocket;

        private readonly Queue<MemoryBuffer> waitSend = new();
        
        public WChannel(long id, IPEndPoint ipEndPoint, WService service)
        {
            this.Service = service;
            this.Id = id;
            
            WebSocket ws = new($"ws://{ipEndPoint}");

            this.RemoteAddress = ipEndPoint;

            // Subscribe to the WS events
            ws.OnOpen += OnOpen;
            ws.OnClose += OnClosed;
            ws.OnError += OnError;
            ws.OnMessage += OnRead;

            // Start connecting to the server
            ws.ConnectAsync();
        }
        
        public override void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }
            
            this.Id = 0;

            this.webSocket.CloseAsync();
            this.webSocket = null;
        }

        public void Send(MemoryBuffer memoryBuffer)
        {
            if (this.webSocket == null)
            {
                this.waitSend.Enqueue(memoryBuffer);
                return;
            }

            SendOne(memoryBuffer);
        }

        private void SendOne(MemoryBuffer memoryBuffer)
        {
            byte[] data = new byte[memoryBuffer.Length - memoryBuffer.Position];
            Array.Copy(memoryBuffer.GetBuffer(), memoryBuffer.Position, data, 0, data.Length);
            this.webSocket.SendAsync(data);
        }

        private void OnOpen(object obj, OpenEventArgs ws)
        {
            if (ws == null)
            {
                this.OnError(ErrorCore.ERR_WebsocketConnectError);
                return;
            }

            if (this.IsDisposed)
            {
                return;
            }

            this.webSocket = (WebSocket)obj;

            while (this.waitSend.Count > 0)
            {
                MemoryBuffer memoryBuffer = this.waitSend.Dequeue();
                this.SendOne(memoryBuffer);
            }
        }

        /// <summary>
        /// Called when we received a text message from the server
        /// </summary>
        private void OnRead(object obj, MessageEventArgs messageEventArgs)
        {
            if (this.IsDisposed)
            {
                return;
            }
            
            MemoryBuffer memoryBuffer = this.Service.Fetch();
            memoryBuffer.Write(messageEventArgs.RawData);
            memoryBuffer.Seek(0, SeekOrigin.Begin);
            this.Service.ReadCallback(this.Id, memoryBuffer);
        }

        /// <summary>
        /// Called when the web socket closed
        /// </summary>
        private void OnClosed(object obj, CloseEventArgs closeEventArgs)
        {
            if (this.IsDisposed)
            {
                return;
            }
            
            Log.Error($"wchannel closed: {closeEventArgs.Code} {closeEventArgs.Reason} {closeEventArgs.StatusCode} {closeEventArgs.WasClean}");
            this.OnError(0);
        }

        /// <summary>
        /// Called when an error occured on client side
        /// </summary>
        private void OnError(object obj, UnityWebSocket.ErrorEventArgs errorEventArgs)
        {
            if (this.IsDisposed)
            {
                return;
            }
            
            Log.Error($"WChannel error: {this.Id} {errorEventArgs.Message} {errorEventArgs.Exception}");
            
            this.OnError(ErrorCore.ERR_WebsocketError);
        }
        
        private void OnError(int error)
        {
            Log.Info($"WChannel error: {this.Id} {error}");
            
            long channelId = this.Id;
			
            this.Service.Remove(channelId);
			
            this.Service.ErrorCallback(channelId, error);
        }
    }
}
#endif