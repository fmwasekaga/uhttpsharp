/*
 * Copyright (C) 2011 uhttpsharp project - http://github.com/raistlinthewiz/uhttpsharp
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.

 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.

 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System.Text;
using System.Net;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using uhttpsharp.Clients;
using uhttpsharp.Headers;
using uhttpsharp.RequestProviders;
using Logging.io;
using uhttpsharp.Events;

namespace uhttpsharp
{
    public class HttpClientHandler
    {
        private const string CrLf = "\r\n";
        private static readonly byte[] CrLfBuffer = Encoding.UTF8.GetBytes(CrLf);

        //private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private Task _task;
        private /*readonly*/ Stream _stream;
        private /*readonly*/ IClient _client;
        private /*readonly*/ Func<IHttpContext, Task> _requestHandler;
        private /*readonly*/ IHttpRequestProvider _requestProvider;
        private /*readonly*/ EndPoint _remoteEndPoint;
        private DateTime _lastOperationTime;

        public EventHandler ClientEvent { get; set; }
        public EventHandler<LogEventArgs> LogEvent { get; set; }

        public HttpClientHandler(IClient client, Func<IHttpContext, Task> requestHandler, IHttpRequestProvider requestProvider)
        {
            _remoteEndPoint = client.RemoteEndPoint;
            _client = client;
            _requestHandler = requestHandler;
            _requestProvider = requestProvider;

            _stream = new BufferedStream(_client.Stream, 8192);
            _task = Task.Factory.StartNew(() => { Process(); });
            UpdateLastOperationTime();
        }

        private async void Process()
        {
            try
            {
                while (_client.Connected)
                {
                    // TODO : Configuration.
                    var limitedStream = new NotFlushingStream(new LimitedStream(_stream, readLimit: 1024*1024, writeLimit: 1024*1024));
                    var streamReader = new StreamReader(limitedStream);
                    
                    var request = await _requestProvider.Provide(streamReader).ConfigureAwait(false);

                    if (request != null)
                    {
                        UpdateLastOperationTime();

                        var context = new HttpContext(request, _client.RemoteEndPoint);

                        //Logger.InfoFormat("{1} : Got request {0}", request.Uri, _client.RemoteEndPoint);

                        await _requestHandler(context).ConfigureAwait(false);

                        if (context.Response != null)
                        {
                            var streamWriter = new StreamWriter(limitedStream) { AutoFlush = false };
                            
                            await WriteResponse(context, streamWriter).ConfigureAwait(false);
                            await limitedStream.ExplicitFlushAsync().ConfigureAwait(false);

                            if (!request.Headers.KeepAliveConnection() || context.Response.CloseConnection)
                            {
                                _client.Close();
                            }
                        }

                        UpdateLastOperationTime();
                    }
                    else
                    {
                        _client.Close();
                    }
                }
            }
            catch (Exception e)
            {
                // Hate people who make bad calls.
                LogEvent?.Invoke(this, new LogEventArgs(string.Format("Error while serving : {0}. ", _remoteEndPoint) + e.Message + Environment.NewLine + e.StackTrace));
                //Logger.WarnException(string.Format("Error while serving : {0}", _remoteEndPoint), e);
                _client.Close();
            }

            LogEvent?.Invoke(this, new LogEventArgs(string.Format("Lost Web Client {0}", _remoteEndPoint)));
            //Logger.InfoFormat("Lost Client {0}", _remoteEndPoint);
            ClientEvent?.Invoke(this, new EventArgs());
        }
        private async Task WriteResponse(HttpContext context, StreamWriter writer)
        {
            IHttpResponse response = context.Response;
            IHttpRequest request = context.Request;
    
            // Headers
            await writer.WriteLineAsync(string.Format("HTTP/1.1 {0} {1}",
                (int)response.ResponseCode,
                response.ResponseCode))
                .ConfigureAwait(false);
            
            foreach (var header in response.Headers)
            {
                await writer.WriteLineAsync(string.Format("{0}: {1}", header.Key, header.Value)).ConfigureAwait(false);
            }

            // Cookies
            if (context.Cookies.Touched)
            {
                await writer.WriteAsync(context.Cookies.ToCookieData())
                    .ConfigureAwait(false);
            }

            // Empty Line
            await writer.WriteLineAsync().ConfigureAwait(false);

            // Body
            await response.WriteBody(writer).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
            
        }

        public IClient Client
        {
            get { return _client; }
        }

        public void ForceClose()
        {
            _client.Close();
        }

        public DateTime LastOperationTime
        {
            get
            {
                return _lastOperationTime;
            }
        }

        private void UpdateLastOperationTime()
        {
            // _lastOperationTime = DateTime.Now;
        }

        public void Dispose()
        {
            _remoteEndPoint = null;
            _client = null;
            _requestHandler = null;
            _requestProvider = null;

            _stream = null;
            _task.Dispose();
            _task = null;
        }

    }

    internal class NotFlushingStream : Stream
    {
        private readonly Stream _child;
        public NotFlushingStream(Stream child)
        {
            _child = child;
        }


        public void ExplicitFlush()
        {
            _child.Flush();
        }

        public Task ExplicitFlushAsync()
        {
            return _child.FlushAsync();
        }

        public override void Flush()
        {
            // _child.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _child.Seek(offset, origin);
        }
        public override void SetLength(long value)
        {
            _child.SetLength(value);
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _child.Read(buffer, offset, count);
        }

        public override int ReadByte()
        {
            return _child.ReadByte();
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            _child.Write(buffer, offset, count);
        }
        public override void WriteByte(byte value)
        {
            _child.WriteByte(value);
        }
        public override bool CanRead
        {
            get { return _child.CanRead; }
        }
        public override bool CanSeek
        {
            get { return _child.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _child.CanWrite; }
        }
        public override long Length
        {
            get { return _child.Length; }
        }
        public override long Position
        {
            get { return _child.Position; }
            set { _child.Position = value; }
        }
        public override int ReadTimeout
        {
            get { return _child.ReadTimeout; }
            set { _child.ReadTimeout = value; }
        }
        public override int WriteTimeout
        {
            get { return _child.WriteTimeout; }
            set { _child.WriteTimeout = value; }
        }
    }

    public static class RequestHandlersAggregateExtensions
    {

        public static Func<IHttpContext, Task> Aggregate(this IList<IHttpRequestHandler> handlers)
        {
            return handlers.Aggregate(0);
        }

        private static Func<IHttpContext, Task> Aggregate(this IList<IHttpRequestHandler> handlers, int index)
        {
            if (index == handlers.Count)
            {
                return null;
            }

            var currentHandler = handlers[index];
            var nextHandler = handlers.Aggregate(index + 1);
            
            return context => currentHandler.Handle(context, () => nextHandler(context));
        }


    }
}