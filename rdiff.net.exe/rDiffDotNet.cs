using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using librsync.net;
using System.Security.Cryptography;

namespace rdiff.net.exe
{
    public class rDiffDotNet
    {

        public static void ComputeSignature(string FilePath, string SignatureOutputPath)
        {
            using (FileStream file = new FileStream(FilePath, FileMode.Open))
            {
                using (var signature = Librsync.ComputeSignature(file))
                {
                    using (var signatureFile = File.Create(SignatureOutputPath))
                    {
                        signature.Seek(0, SeekOrigin.Begin);
                        signature.CopyTo(signatureFile);
                    }
                }
            }
        }

        public static void ComputeDelta(string NewFilePath, string SignaturePath, string DeltaOutputPath)
        {
            using (var file = new FileStream(NewFilePath, FileMode.Open))
            {
                using (var signature = new FileStream(SignaturePath, FileMode.Open))
                {
                    using (var delta = Librsync.ComputeDelta(signature, file))
                    {
                        using (var deltaFile = File.Create(DeltaOutputPath))
                        {
                            //delta.Seek(0, SeekOrigin.Begin);
                            delta.CopyTo(deltaFile);
                        }
                    }
                }
            }
        }

        public static void ComputeDeltaFromFiles(string OriginalFilePath, string NewFilePath, string DeltaOutputPath)
        {
            using (var fileA = new FileStream(OriginalFilePath, FileMode.Open))
            {
                var signature = Librsync.ComputeSignature(fileA);

                using (var fileB = new FileStream(NewFilePath, FileMode.Open))
                {
                    using (var delta = Librsync.ComputeDelta(signature, fileB))
                    {
                        using (var deltaFile = File.Create(DeltaOutputPath))
                        {
                            //delta.Seek(0, SeekOrigin.Begin);
                            delta.CopyTo(deltaFile);
                        }
                    }
                }
            }
        }

        public static void ApplyDelta(string OriginalFilePath, string DeltaPath, string NewFileOutputPath)
        {
            using (var originalFile = new FileStream(OriginalFilePath, FileMode.Open))
            {
                using (var delta = new FileStream(DeltaPath, FileMode.Open))
                {
                    using (var resultStream = Librsync.ApplyDelta(originalFile, delta))
                    {
                        using (var newFile = File.Create(NewFileOutputPath))
                        {
                            newFile.Seek(0, SeekOrigin.Begin);
                            resultStream.CopyTo(newFile);
                        }
                    }
                }
            }
        }
    }
}
