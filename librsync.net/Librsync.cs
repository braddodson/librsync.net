using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace librsync.net
{
    public static class Librsync
    {
        public static Stream ComputeSignature(Stream inputFile)
        {
            return new SignatureStream(inputFile,
                new SignatureJobSettings
                {
                    MagicNumber = MagicNumber.Blake2Signature,
                    BlockLength = SignatureHelpers.DefaultBlockLength,
                    StrongSumLength = SignatureHelpers.DefaultStrongSumLength
                });
        }

        public static Stream ComputeDelta(Stream signature, Stream newFile)
        {
            return new DeltaStream(signature, newFile);
        }

        public static Stream ApplyDelta(Stream originalFile, Stream delta)
        {
            return new PatchedStream(originalFile, delta);
        }
    }
}
