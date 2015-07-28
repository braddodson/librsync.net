using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace librsync.net
{
    internal class SignatureStream : Stream
    {
        private const int BlocksToBuffer = 100;
        private BinaryReader inputReader;
        private SignatureJobSettings settings;
        private MemoryStream bufferStream;

        public SignatureStream(Stream inputStream, SignatureJobSettings settings)
        {
            this.inputReader = new BinaryReader(inputStream);
            this.settings = settings;

            // initialize the buffer with the header
            this.bufferStream = new MemoryStream();
            var writer = new BinaryWriter(this.bufferStream);
            SignatureHelpers.WriteHeader(writer, settings);
            writer.Flush();
            this.bufferStream.Seek(0, SeekOrigin.Begin);
        }


        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (this.bufferStream.Position == this.bufferStream.Length)
            {
                this.FillBuffer();
            }

            return await this.bufferStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.ReadAsync(buffer, offset, count).Result;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return this.ReadAsync(buffer, offset, count).ToApm(callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return ((Task<int>)asyncResult).Result;
        }

        private void FillBuffer()
        {
            this.bufferStream = new MemoryStream();
            var writer = new BinaryWriter(this.bufferStream);
            for (int i = 0; i < BlocksToBuffer; i++)
            {
                var block = this.inputReader.ReadBytes(this.settings.BlockLength);
                if (block.Length != 0)
                {
                    SignatureHelpers.WriteBlock(writer, block, this.settings);
                }
            }

            writer.Flush();
            this.bufferStream.Seek(0, SeekOrigin.Begin);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
