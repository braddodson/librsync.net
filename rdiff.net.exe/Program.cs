using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using librsync.net;


namespace rdiff.net.exe
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    throw new ArgumentException("No verb provided.");
                }

                string verb = args[0].ToLower();

                switch (verb)
                {
                    case "help":
                    case "-help":
                    case "--help":
                    case "-?":
                    case "--?":
                        {
                            Console.WriteLine(Usage);
                        }
                        break;
                    case "signature":
                        {
                            String filePath = null;
                            String signatureOutputPath = null;
                            for (int i = 1; i < args.Length; i += 2)
                            {
                                switch (args[i].ToLower())
                                {
                                    case "--file":
                                    case "-f":
                                        filePath = args[i + 1];
                                        break;
                                    case "--output":
                                    case "-o":
                                        signatureOutputPath = args[i + 1];
                                        break;
                                    default:
                                        throw new ArgumentException(String.Format("{0} is not a recognized argument", args[i]));
                                }
                            }

                            if (filePath == null)
                            {
                                throw new ArgumentException("No <filepath> provided to --file");
                            }

                            if (signatureOutputPath == null)
                            {
                                signatureOutputPath = filePath + ".signature";
                            }

                            rDiffDotNet.ComputeSignature(filePath, signatureOutputPath);
                        }
                        break;
                    case "delta":
                        {
                            String newFilePath = null, signaturePath = null, deltaOutputPath = null;

                            for (int i = 1; i < args.Length; i += 2)
                            {
                                switch (args[i].ToLower())
                                {
                                    case "--file":
                                    case "-f":
                                        newFilePath = args[i + 1];
                                        break;
                                    case "--signature":
                                    case "-s":
                                        signaturePath = args[i + 1];
                                        break;
                                    case "--output":
                                    case "-o":
                                        deltaOutputPath = args[i + 1];
                                        break;
                                    default:
                                        throw new ArgumentException(String.Format("{0} is not a recognized argument", args[i]));
                                }
                            }

                            if (newFilePath == null)
                            {
                                throw new ArgumentException("No <filepath> provided to --file");
                            }

                            if (signaturePath == null)
                            {
                                throw new ArgumentException("No <signaturepath> provided to --signature");
                            }

                            if (deltaOutputPath == null)
                            {
                                deltaOutputPath = signaturePath.Replace(".signature", ".delta");
                            }

                            rDiffDotNet.ComputeDelta(newFilePath, signaturePath, deltaOutputPath);
                        }
                        break;
                    case "applydelta":
                    case "patch":
                        {
                            String originalFilePath = null, deltaFilePath = null, newFileOutputPath = null;

                            for (int i = 1; i < args.Length; i += 2)
                            {
                                switch (args[i].ToLower())
                                {
                                    case "--file":
                                    case "-f":
                                        originalFilePath = args[i + 1];
                                        break;
                                    case "--delta":
                                    case "-d":
                                        deltaFilePath = args[i + 1];
                                        break;
                                    case "--output":
                                    case "-o":
                                        newFileOutputPath = args[i + 1];
                                        break;
                                    default:
                                        throw new ArgumentException(String.Format("{0} is not a recognized argument", args[i]));
                                }
                            }

                            if (originalFilePath == null)
                            {
                                throw new ArgumentException("No <filepath> provided to --file");
                            }

                            if (deltaFilePath == null)
                            {
                                throw new ArgumentException("No <deltapath> provided to --delta");
                            }

                            if (newFileOutputPath == null)
                            {
                                newFileOutputPath = originalFilePath;
                            }

                            rDiffDotNet.ApplyDelta(originalFilePath, deltaFilePath, newFileOutputPath);
                        }
                        break;
                    case "diff":
                        {
                            String originalFilePath = null, newFilePath = null, deltaOutputFilePath = null;

                            for (int i = 1; i < args.Length; i += 2)
                            {
                                switch (args[i].ToLower())
                                {
                                    case "--original":
                                    case "-v1":
                                        originalFilePath = args[i + 1];
                                        break;
                                    case "--new":
                                    case "-v2":
                                        newFilePath = args[i + 1];
                                        break;
                                    case "--output":
                                    case "-o":
                                        deltaOutputFilePath = args[i + 1];
                                        break;
                                    default:
                                        throw new ArgumentException(String.Format("{0} is not a recognized argument", args[i]));
                                }
                            }

                            if (originalFilePath == null)
                            {
                                throw new ArgumentException("No <filepath> provided to --original");
                            }

                            if (newFilePath == null)
                            {
                                throw new ArgumentException("No <filepath> provided to --new");
                            }

                            if (deltaOutputFilePath == null)
                            {
                                deltaOutputFilePath = originalFilePath + ".delta";
                            }

                            rDiffDotNet.ComputeDeltaFromFiles(originalFilePath, newFilePath, deltaOutputFilePath);
                        }
                        break;
                    default:
                        throw new ArgumentException(String.Format("{0} is not a recognized argument", verb));
                }
            } catch (ArgumentException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Please see help for more information. \n");
                Console.WriteLine(Usage);
            }
        }
        
        private static string Usage =
@"SYNONPSIS:
    Uses the librsync.net library to generate rsync signatures, create file deltas from signatures, and patch files with diffs.

USAGE: 
    Generate a signature from a given file and save it to disk:
        rdiff.net.exe signature --file <filepath> [--output <outputpath>]
    
    Generate a delta from a given file and a signature and save it to disk:
        rdiff.net.exe delta --file <filepath> --signature <signaturepath> [--output <outputpath>]

    Generate a delta between two files and save it to disk:
        rdiff.net.exe diff --original <filepath> --new <filepath> [--output <outputpath>]

    Generate a new file by applying a delta and save it to disk:
        rdiff.net.exe applydelta --file <filepath> --delta <deltapath> [--output <outputpath>]
       
    Options:
        --file, -f      - path to the input file
        --delta, -d     - path to a delta file
        --signature, -s - path to a signature file
        --output, -o    - path where output will be saved
        --original, -v1 - path to the original file
        --new, v2       - path to the changed file";

    }
}
