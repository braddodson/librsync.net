using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using librsync.net;

namespace librsyncTest
{
    [TestClass]
    public class PatchingTests
    {
        [TestMethod]
        public void NullDeltaTest()
        {
            NullDeltaTest(
                new byte[] { 0x72, 0x73, 0x02, 0x36, 0x00 },
                new byte[] { });

            NullDeltaTest(
                new byte[] { 0x72, 0x73, 0x02, 0x36, 0x05, 0x68, 0x65, 0x6C, 0x6C, 0x6F, 0x00 },
                new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F });

            NullDeltaTest(
                new byte[] { 0x72, 0x73, 0x02, 0x36, 0x05, 0x68, 0x65, 0x6C, 0x6C, 0x6F, 0x07, 0x20, 0x77, 0x6F, 0x72, 0x6C, 0x64, 0x0A, 0x00 },
                new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x77, 0x6F, 0x72, 0x6C, 0x64, 0x0A });
        }

        private void NullDeltaTest(byte[] deltas, byte[] expected)
        {
            var nullStream = new MemoryStream();
            using (var deltaStream = new MemoryStream(deltas))
            {
                var result = Librsync.ApplyDelta(nullStream, deltaStream);
                var resultMem = new MemoryStream();
                result.CopyTo(resultMem);

                Assert.IsTrue(expected.SequenceEqual(resultMem.ToArray()));
            }
        }
    }
}
