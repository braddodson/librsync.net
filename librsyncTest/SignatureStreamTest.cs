using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace librsyncTest
{
    [TestClass]
    public class SignatureStreamTest
    {
        private static Random r = new Random();

        [TestMethod]
        public void TestLengthAndSeeking()
        {
            var inputStream = new MemoryStream(RoundTripTest.MakeRandomFile());
            var signatureStream = librsync.net.Librsync.ComputeSignature(inputStream);

            var resultMem = new MemoryStream();
            signatureStream.CopyTo(resultMem);

            Assert.AreEqual(resultMem.Length, signatureStream.Length);

            for (int i = 0; i < 1000; i++)
            {
                resultMem.Position = i;
                signatureStream.Position = i;

                Assert.AreEqual(resultMem.ReadByte(), signatureStream.ReadByte());
            }
        }
    }
}
