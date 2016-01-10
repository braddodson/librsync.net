using System;
using System.Security.Cryptography;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using rdiff.net.exe;

namespace rDiff.NetTest
{
    [TestClass]
    public class rDiffDotNetTest
    {
        string originalFilePath = "./TestFiles/Original.txt";
        string newFilePath = "./TestFiles/Modified.txt";
        string patchedFilePath = "./TestFiles/Patched.txt";
        string deltaPath = "./TestFiles/Original.txt.delta";
        string signaturePath = "./TestFiles/Original.txt.signature";

        [TestMethod]
        public void E2ETest()
        {            
            rDiffDotNet.ComputeSignature(originalFilePath, signaturePath);
            rDiffDotNet.ComputeDelta(newFilePath, signaturePath, deltaPath);
            rDiffDotNet.ApplyDelta(originalFilePath, deltaPath, patchedFilePath);
            Assert.IsTrue(VerifyFilesAreIdentical(newFilePath, patchedFilePath));
        }

        [TestMethod]
        public void DiffTest()
        {
            //makes sure that both methods of generating deltas (file+file and file+signature) 
            //create the same delta file.
            rDiffDotNet.ComputeSignature(originalFilePath, signaturePath);
            rDiffDotNet.ComputeDelta(newFilePath, signaturePath, deltaPath);

            string deltaPath2 = deltaPath + "2";
            rDiffDotNet.ComputeDeltaFromFiles(originalFilePath, newFilePath, deltaPath2);            

            Assert.IsTrue(VerifyFilesAreIdentical(deltaPath, deltaPath2));
        }

        [TestMethod]
        public void DiffTestE2E()
        {
            rDiffDotNet.ComputeDeltaFromFiles(originalFilePath, newFilePath, deltaPath);
            rDiffDotNet.ApplyDelta(originalFilePath, deltaPath, patchedFilePath);
            Assert.IsTrue(VerifyFilesAreIdentical(newFilePath, patchedFilePath));
        }

        private static bool VerifyFilesAreIdentical(string newFilePath, string patchedFilePath)
        {
            MD5 md5 = MD5.Create();
            using (var newFile = new FileStream(newFilePath, FileMode.Open))
            {
                using (var patchedFile = new FileStream(patchedFilePath, FileMode.Open))
                {
                    byte[] originalHash = md5.ComputeHash(newFile);
                    byte[] patchedHash = md5.ComputeHash(patchedFile);
                    return originalHash.SequenceEqual(patchedHash);
                }
            }
        }
    }
}
