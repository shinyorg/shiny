using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Foundation;

namespace Shiny.Net.Http
{
    public class BodyStream : NSInputStream
    {
        readonly FileInfo file;
        FileStream? stream;


        public BodyStream(FileInfo file) => this.file = file;

        public override void Open()
            => this.stream = this.file.OpenRead();

        public override NSStreamStatus Status
        {
            get
            {
                if (this.stream == null)
                    return NSStreamStatus.Closed;

                return NSStreamStatus.Open;
            }
        }

        public override bool HasBytesAvailable() => this.stream.Position < this.stream.Length;


        public override nint Read(IntPtr buffer, nuint len)
        {
            // TODO: has cleared preamble below?
            //var boundary = Guid.NewGuid().ToString();
            //if (!request.PostData.IsEmpty())
            //{
            //    outputStream.WriteString("--" + boundary);
            //    outputStream.WriteString("Content-Type: text/plain; charset=utf-8");
            //    outputStream.WriteString("Content-Disposition: form-data;");
            //    outputStream.WriteLine();
            //    outputStream.WriteString(request.PostData!);
            //}
            // TODO: if starting next stream, write preamble for that first - new boundary and disposition
            var nbuffer = new byte[len];
            var read = this.stream.Read(nbuffer, 0, (int)len);
            Marshal.Copy(nbuffer, 0, buffer, read);
            return read;
        }
    }
}
