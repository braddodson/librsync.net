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
        private const long HeaderLength = 12;
        private Stream inputStream;
        private BinaryReader inputReader;
        private SignatureJobSettings settings;
        private MemoryStream bufferStream;
        private long currentPosition;

        public SignatureStream(Stream inputStream, SignatureJobSettings settings)
        {
            this.inputStream = inputStream;
            this.inputReader = new BinaryReader(inputStream);
            this.settings = settings;

            // initialize the buffer with the header
            this.InitializeHeader();
            this.currentPosition = 0;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (this.bufferStream.Position == this.bufferStream.Length)
            {
                this.FillBuffer();
            }

            var length =  await this.bufferStream.ReadAsync(buffer, offset, count, cancellationToken);
            this.currentPosition += length;
            return length;
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
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get
            {
                long blockCount = (this.inputStream.Length + this.settings.BlockLength - 1) / this.settings.BlockLength;
                return SignatureStream.HeaderLength + blockCount * (4 + this.settings.StrongSumLength);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition = StreamHelpers.ComputeNewPosition(offset, origin, this.Length, this.Position);

            if (newPosition < SignatureStream.HeaderLength)
            {
                this.InitializeHeader();
                this.bufferStream.Seek(newPosition, SeekOrigin.Begin);
            }
            else
            {
                // if we are in the main section of the file, we calculate which block we are in
                // then we seek to the point at the start of that block in the source file
                // we refill the buffer and seek into it the remainder bytes

                long adjustedPosition = newPosition - SignatureStream.HeaderLength;
                int blockSize = (4 + this.settings.StrongSumLength);
                long remainderBytes;
                long blockNumber = Math.DivRem(adjustedPosition, blockSize, out remainderBytes);

                this.inputStream.Seek(blockNumber * this.settings.BlockLength, SeekOrigin.Begin);
                this.FillBuffer(); // this reads the next block and computes it's hash
                this.bufferStream.Seek(remainderBytes, SeekOrigin.Begin);
            }

            this.currentPosition = newPosition;
            return this.currentPosition;
        }

        public override long Position
        {
            get
            {
                return this.currentPosition;
            }

            set
            {
                this.Seek(value, SeekOrigin.Begin);
            }
        }

        public override void Flush()
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

        private void InitializeHeader()
        {
            this.bufferStream = new MemoryStream();
            var writer = new BinaryWriter(this.bufferStream);
            SignatureHelpers.WriteHeader(writer, this.settings);
            writer.Flush();
            this.bufferStream.Seek(0, SeekOrigin.Begin);
        }
    }
}
