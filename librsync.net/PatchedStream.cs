using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace librsync.net
{
    /// <summary>
    /// Implements a streaming patching of data
    /// The librsync delta format basically has two types of operations:
    ///  * Literal - these basically say "output these bytes"
    ///  * Copy - these provide a source offset and length from the input file, which should be copied to the output
    /// 
    /// This implements a readable stream interface to apply the delta to the input as you read the data
    /// This basically buffers out the data from each command.
    /// </summary>
    internal class PatchedStream : Stream
    {
        private Stream input;
        private Stream delta;
        private BinaryReader deltaReader;
        /// <summary>
        /// This represents the state of the current command.
        /// Basically, we are always copying from one of the input streams.
        /// </summary>
        private StreamCopyHelper currentCopyHelper;

        public PatchedStream(Stream input, Stream delta)
        {
            this.input = input;
            this.delta = delta;
            this.deltaReader = new BinaryReader(this.delta);
            // read and check the header
            PatchedStream.ReadHeader(this.deltaReader);

            // starting the current helper with 0 bytes left will force us to immediately read a new command
            this.currentCopyHelper = new StreamCopyHelper(0, input);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // if the current command can't supply any more data, then read a new command
            if (!this.currentCopyHelper.MoreData)
            {
                // read the next command from the delta stream
                var command = ReadCommand(this.deltaReader);
                // construct a copy helper to run that command
                this.currentCopyHelper = ConstructCopyHelperForCommand(command, this.input, this.delta);
                if (this.currentCopyHelper == null)
                {
                    // if it's null, that means we reached the end token
                    return 0;
                }
            }

            // now read the data based on the copy helper 
            return await this.currentCopyHelper.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).Result;
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

        // TODO: it should be possible to compute the length of the patched stream without generating it
        // based on adding up the length of every command. It should be possible to seek by this method too.
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

        private static void ReadHeader(BinaryReader s)
        {
            uint magic = StreamHelpers.ReadBigEndianUint32(s);
            if (magic != (uint)MagicNumber.Delta)
            {
                throw new InvalidDataException(string.Format("Got magic number {0:x} instead of expected {1:x}", magic, (uint)MagicNumber.Delta));
            }
        }

        private class StreamCopyHelper
        {
            private long bytesLeft;
            private Stream source;

            public StreamCopyHelper(long bytes, Stream source)
            {
                this.bytesLeft = bytes;
                this.source = source;
            }

            public bool MoreData
            {
                get { return bytesLeft > 0; }
            }

            public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                int bytesToRead = (int)Math.Min(count, bytesLeft);
                int bytesActuallyRead = await this.source.ReadAsync(buffer, offset, bytesToRead, cancellationToken);
                bytesLeft -= bytesActuallyRead;
                return bytesActuallyRead;
            }
        }

        private static Command ReadCommand(BinaryReader s)
        {
            // the first byte indicates which kind and format it is
            var commandFormat = GetCommandFormat(s.ReadByte());

            // then we might have to read the parameters in
            Command result = new Command();
            result.Kind = commandFormat.Kind;
            if (commandFormat.Length1 > 0)
            {
                result.Parameter1 = StreamHelpers.ConvertFromBigEndian(s.ReadBytes(commandFormat.Length1));
            }
            else
            {
                // some of the commands encode the parameter directly into the command byte
                result.Parameter1 = commandFormat.ImmediateValue;
            }

            if (commandFormat.Length2 > 0)
            {
                result.Parameter2 = StreamHelpers.ConvertFromBigEndian(s.ReadBytes(commandFormat.Length2));
            }

            return result;
        }

        private static CommandFormat GetCommandFormat(byte commandCode)
        {
            if (commandCode == 0)
            {
                return new CommandFormat(CommandKind.End, 0);
            }
            else if (commandCode <= 64)
            {
                return new CommandFormat(CommandKind.Literal, commandCode);
            }
            else if (commandCode < 85)
            {
                return CommandFormatTable[commandCode - 65];
            }

            return new CommandFormat(CommandKind.Reserved, commandCode);
        }

        private static StreamCopyHelper ConstructCopyHelperForCommand(Command command, Stream input, Stream delta)
        {
            switch (command.Kind)
            {
                case CommandKind.Literal:
                    // for a literal command, we copy bytes from the delta stream
                    return new StreamCopyHelper(command.Parameter1, delta);
                case CommandKind.Copy:
                    // for a copy command, we seek to the specified point and copy from the input stream
                    input.Seek(command.Parameter1, SeekOrigin.Begin);
                    return new StreamCopyHelper(command.Parameter2, input);
                case CommandKind.End:
                    return null;
                default:
                    throw new InvalidDataException(string.Format("Unknown command {0}", command.Parameter1));
            }
        }

        /// <summary>
        /// Table of commands with ids from 65 to 84
        /// (unlike librsync, I didn't expand the immediate literals)
        /// </summary>
        private static CommandFormat[] CommandFormatTable = new CommandFormat[]
        {
            new CommandFormat(CommandKind.Literal, 0, 1, 0),
            new CommandFormat(CommandKind.Literal, 0, 2, 0),
            new CommandFormat(CommandKind.Literal, 0, 4, 0),
            new CommandFormat(CommandKind.Literal, 0, 8, 0),

            new CommandFormat(CommandKind.Copy, 0, 1, 1),
            new CommandFormat(CommandKind.Copy, 0, 1, 2),
            new CommandFormat(CommandKind.Copy, 0, 1, 4),
            new CommandFormat(CommandKind.Copy, 0, 1, 8),
            new CommandFormat(CommandKind.Copy, 0, 2, 1),
            new CommandFormat(CommandKind.Copy, 0, 2, 2),
            new CommandFormat(CommandKind.Copy, 0, 2, 4),
            new CommandFormat(CommandKind.Copy, 0, 2, 8),
            new CommandFormat(CommandKind.Copy, 0, 4, 1),
            new CommandFormat(CommandKind.Copy, 0, 4, 2),
            new CommandFormat(CommandKind.Copy, 0, 4, 4),
            new CommandFormat(CommandKind.Copy, 0, 4, 8),
            new CommandFormat(CommandKind.Copy, 0, 8, 1),
            new CommandFormat(CommandKind.Copy, 0, 8, 2),
            new CommandFormat(CommandKind.Copy, 0, 8, 4),
            new CommandFormat(CommandKind.Copy, 0, 8, 8),
        };

        public struct CommandFormat
        {
            public CommandFormat(CommandKind kind, int immediate, int length1 = 0, int length2 = 0)
            {
                this.Kind = kind;
                this.ImmediateValue = immediate;
                this.Length1 = length1;
                this.Length2 = length2;
            }
            
            public CommandKind Kind;
            public int ImmediateValue;
            public int Length1;
            public int Length2;
        }
    }

    public enum CommandKind
    {
        End,
        Literal,
        Copy,
        Reserved
    }

    public struct Command
    {
        public CommandKind Kind;
        public long Parameter1;
        public long Parameter2;
    }
}
