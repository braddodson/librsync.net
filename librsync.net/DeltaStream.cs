using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace librsync.net
{
    internal class DeltaStream : Stream
    {
        private IEnumerator<OutputCommand> commandsToOutput;
        private MemoryStream currentCommandStream;
        
        public DeltaStream(Stream signatureStream, Stream inputStream)
        {
            var signature = SignatureHelpers.ParseSignatureFile(signatureStream);
            var inputReader = new BinaryReader(inputStream);
            var commands = DeltaCalculator.ComputeCommands(inputReader, signature);
            this.commandsToOutput = commands.GetEnumerator();

            this.currentCommandStream = new MemoryStream();
            var writer = new BinaryWriter(this.currentCommandStream);
            StreamHelpers.WriteBigEndian(writer, (uint)MagicNumber.Delta);
            writer.Flush();
            this.currentCommandStream.Seek(0, SeekOrigin.Begin);
        }


        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (currentCommandStream.Position == currentCommandStream.Length)
            {
                if (!commandsToOutput.MoveNext())
                {
                    return 0;
                }

                this.currentCommandStream = new MemoryStream();
                var writer = new BinaryWriter(this.currentCommandStream);
                DeltaCalculator.WriteCommand(writer, commandsToOutput.Current);
                writer.Flush();
                this.currentCommandStream.Seek(0, SeekOrigin.Begin);
            }

            return await this.currentCommandStream.ReadAsync(buffer, offset, count);
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

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
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
