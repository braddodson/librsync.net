 using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using librsync.net;

namespace librsyncTest
{
    [TestClass]
    public class RoundTripTest
    {
        private static Random r = new Random();

        [TestMethod]
        public void RoundTrips()
        {
            for (int i = 0; i < 100; i++)
            {
                RunIteration();
            }
        }

        public void RunIteration()
        {
            var file = MakeRandomFile();
            var newFile = file;
            for (int i = 0; i < 10; i++)
            {
                newFile = ApplyChange(newFile);

                var signature = Librsync.ComputeSignature(new MemoryStream(file));
                var delta = Librsync.ComputeDelta(signature, new MemoryStream(newFile));
                var deltaMem = new MemoryStream();
                delta.CopyTo(deltaMem);
                deltaMem.Seek(0, SeekOrigin.Begin);
                var regenerated = Librsync.ApplyDelta(new MemoryStream(file), deltaMem);

                var regeneratedMem = new MemoryStream();
                regenerated.CopyTo(regeneratedMem);
                Assert.IsTrue(newFile.SequenceEqual(regeneratedMem.ToArray()));
            }
        }

        [TestMethod]
        public void PatchSeekTest()
        {
            var file = MakeRandomFile();
            var newFile = ApplyChange(file);
            var signature = Librsync.ComputeSignature(new MemoryStream(file));
            var delta = Librsync.ComputeDelta(signature, new MemoryStream(newFile));
            var deltaMem = new MemoryStream();
            delta.CopyTo(deltaMem);
            deltaMem.Seek(0, SeekOrigin.Begin);
            var regenerated = Librsync.ApplyDelta(new MemoryStream(file), deltaMem);

            var regeneratedMem = new MemoryStream();
            regenerated.CopyTo(regeneratedMem);

            Assert.AreEqual(regeneratedMem.Length, regenerated.Length);
            for(int i = 0; i < regeneratedMem.Length; i++)
            {
                regeneratedMem.Seek(i, SeekOrigin.Begin);
                regenerated.Seek(i, SeekOrigin.Begin);
                Assert.AreEqual(regeneratedMem.ReadByte(), regenerated.ReadByte());
            }
        }

        public static byte[] MakeRandomFile()
        {
            int len = r.Next(1000000);
            byte[] result = new byte[len];
            r.NextBytes(result);
            return result;
        }

        public byte[] ApplyChange(byte[] inputFile)
        {
            List<byte> result = new List<byte>(inputFile);
            int type = r.Next(3);
            switch (type)
            {
                case 0:
                    // just change a byte
                    result[r.Next(result.Count)] = (byte)r.Next(256);
                    break;
                case 1:
                    // insert some bytes
                    var randBytes = new byte[r.Next(1000)];
                    r.NextBytes(randBytes);
                    result.InsertRange(r.Next(result.Count), randBytes);
                    break;
                case 2:
                    // delete some bytes
                    result.RemoveRange(r.Next(result.Count), r.Next(100));
                    break;
            }
            return result.ToArray();
        }
    }
}
