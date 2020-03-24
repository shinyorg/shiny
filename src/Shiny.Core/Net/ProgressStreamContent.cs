﻿using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;


namespace Shiny.Net
{
    public class ProgressStreamContent : HttpContent
    {
        readonly Stream content;
        readonly int bufferSize;
        readonly Action<int> packetSent;
        bool contentConsumed;


        public ProgressStreamContent(Stream content, Action<int> packetSent) : this(content, 8192, packetSent)
        {
        }


        public ProgressStreamContent(Stream content, int bufferSize, Action<int> packetSent)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            this.content = content;
            this.bufferSize = bufferSize;
            this.packetSent = packetSent;
        }


        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            Contract.Assert(stream != null);
            this.PrepareContent();

            return Task.Run(() =>
            {
                var buffer = new byte[this.bufferSize];

                using (this.content)
                {
                    var read = this.content.Read(buffer, 0, buffer.Length);

                    while (read > 0)
                    {
                        stream.Write(buffer, 0, read);
                        this.packetSent.Invoke(read);
                        read = this.content.Read(buffer, 0, buffer.Length);
                    }
                }
            });
        }


        protected override bool TryComputeLength(out long length)
        {
            length = this.content.Length;
            return true;
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
                this.content.Dispose();

            base.Dispose(disposing);
        }


        void PrepareContent()
        {
            if (this.contentConsumed)
            {
                // If the content needs to be written to a target stream a 2nd time, then the stream must support
                // seeking (e.g. a FileStream), otherwise the stream can't be copied a second time to a target
                // stream (e.g. a NetworkStream).
                if (!this.content.CanSeek)
                    throw new InvalidOperationException("SR.net_http_content_stream_already_read");

                this.content.Position = 0;
            }

            this.contentConsumed = true;
        }
    }
}