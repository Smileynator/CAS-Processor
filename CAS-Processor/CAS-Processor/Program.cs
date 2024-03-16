using System;
using System.IO;
using System.Text;

namespace CAS_Processor
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ProcessFiles(args);
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: "+e.Message);
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        private static void ProcessFiles(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No arguments provided.");
                return;
            }

            string filePath = args[0];
            string extension = Path.GetExtension(filePath);

            if (string.IsNullOrEmpty(extension))
            {
                Console.WriteLine("Invalid file path.");
                return;
            }

            switch (extension.ToLower())
            {
                case ".cas":
                    UnpackCasFile(filePath);
                    break;
                case ".canm":
                    PackCanmFile(filePath);
                    break;
                default:
                    Console.WriteLine("Unsupported file type.");
                    break;
            }
        }

        
        private static void UnpackCasFile(string filePath)
        {
            Console.WriteLine($"Unpacking {filePath}...");
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs, Encoding.Unicode))
            {
                if (!CheckCasSignature(reader)) 
                    return;
                long canmStartPos = reader.ReadInt32();
                
                //Start seeking the end of the CANM file
                if (!CheckCanmSignature(canmStartPos, reader))
                {
                    return;
                }
                long canmEndPos = GetCamnEndPos(fs, reader, canmStartPos);
                
                //Export CANM File
                long length = canmEndPos - canmStartPos;
                string camnFilePath = Path.ChangeExtension(filePath, ".CANM");
                Console.WriteLine($"Exporting {length} byte CANM file to {camnFilePath}");
                using (FileStream wfs = new FileStream(camnFilePath, FileMode.OpenOrCreate, FileAccess.Write))
                using (BinaryWriter writer = new BinaryWriter(wfs))
                {
                    fs.Position = canmStartPos;
                    byte[] canmFile = reader.ReadBytes((int)length);
                    writer.Write(canmFile);
                }
            }
        }

        private static bool CheckCasSignature(BinaryReader reader)
        {
            reader.BaseStream.Position = 0;
            var fileSignature = reader.ReadBytes(4);
            if (!ByteArraysEqual(fileSignature, new byte[] { 0x43, 0x41, 0x53, 0x00 }))
            {
                Console.WriteLine("CAS Invalid file signature!");
                return false;
            }

            int version = reader.ReadInt32();
            if (version != 515)
            {
                Console.WriteLine("Unsupported game version!");
                return false;
            }
            return true;
        }
        
        private static bool CheckCanmSignature(long canmPos, BinaryReader reader)
        {
            reader.BaseStream.Position = canmPos;
            var fileSignature = reader.ReadBytes(4);
            if (!ByteArraysEqual(fileSignature, new byte[] {0x43, 0x41, 0x4E, 0x4D}))
            {
                Console.WriteLine("CAMN Invalid file signature!");
                return false;
            }
            return true;
        }

        private static long GetCamnEndPos(FileStream fs, BinaryReader reader, long camnStartPos)
        {
            long canmEndPos = camnStartPos;
            fs.Position = camnStartPos + 0x08;
            int anmCount = reader.ReadInt32();
            int anmOffset = reader.ReadInt32();
            for (int i = 0; i < anmCount; i++)
            {
                fs.Position = camnStartPos + anmOffset + (0x1C * i);
                long basePos = fs.Position;
                fs.Position += 0x04;
                long stringOffset = basePos + reader.ReadInt32();
                fs.Position = stringOffset;
                string name = ReadString(reader);
                if (fs.Position > canmEndPos)
                {
                    canmEndPos = fs.Position;
                }
            }

            fs.Position = camnStartPos + 0x18;
            int boneCount = reader.ReadInt32();
            int boneOffset = reader.ReadInt32();
            fs.Position = camnStartPos + boneOffset;
            for (int i = 0; i < boneCount; i++)
            {
                fs.Position = camnStartPos + boneOffset + (0x04 * i);
                long basePos = fs.Position;
                long stringOffset = basePos + reader.ReadInt32();
                fs.Position = stringOffset;
                string name = ReadString(reader);
                if (fs.Position > canmEndPos)
                {
                    canmEndPos = fs.Position;
                }
            }

            return canmEndPos;
        }

        private static void PackCanmFile(string canmFilePath)
        {
            Console.WriteLine($"Packing {canmFilePath}...");
            string casFilePath = Path.ChangeExtension(canmFilePath, ".CAS");
            Console.WriteLine($"Opening CAS file for wrapping {casFilePath}");
            using (FileStream casFs = new FileStream(casFilePath, FileMode.Open, FileAccess.ReadWrite))
            using (BinaryReader casReader = new BinaryReader(casFs, Encoding.Unicode))
            using (BinaryWriter casWriter = new BinaryWriter(casFs, Encoding.Unicode))
            using (FileStream canmFs = new FileStream(canmFilePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader canmReader = new BinaryReader(canmFs, Encoding.Unicode))
            {
                //Find start and end of CANM point in CAS file
                if (!CheckCasSignature(casReader)) 
                    return;
                long canmStartPos = casReader.ReadInt32();
                if (!CheckCanmSignature(canmStartPos, casReader))
                {
                    return;
                }
                long canmEndPos = GetCamnEndPos(casFs, casReader, canmStartPos);
                long oldCanmLength = canmEndPos - canmStartPos;
                
                //Get the new CANM File length
                if (!CheckCanmSignature(0, canmReader))
                {
                    return;
                }
                long newCanmLength = canmFs.Length;
                int canmLengthDiff = (int)(newCanmLength - oldCanmLength);
                
                //Get the last bytes from the end of the CAS file past CANM
                long endOfFile = casFs.Length;
                long lastBytesSize = endOfFile - canmEndPos;
                casFs.Position = canmEndPos;
                byte[] lastBytes = casReader.ReadBytes((int)lastBytesSize);
                
                Console.WriteLine($"Cropping CAS file and adjusting for new length by {canmLengthDiff}");
                //Crop CAS
                casFs.Position = canmStartPos;
                casFs.SetLength(canmStartPos);
                canmFs.Position = 0;
                // Write new CANM content
                Console.WriteLine($"Write new CANM data");
                byte[] canmFile = canmReader.ReadBytes((int)newCanmLength);
                casWriter.Write(canmFile);
                //Append last bytes
                Console.WriteLine($"Append old CAS data end");
                casWriter.Write(lastBytes);
                
                //Update CAS file offsets to match new CAS length
                Console.WriteLine($"Updating CAS string pointers by {canmLengthDiff}");
                casFs.Position = 0x0C;
                int tControlCount = casReader.ReadInt32();
                int tControlOffset = casReader.ReadInt32();
                int vControlCount = casReader.ReadInt32();
                int vControlOffset = casReader.ReadInt32();
                int anmGroupCount = casReader.ReadInt32();
                int anmGroupOffset = casReader.ReadInt32();
                int boneCount = casReader.ReadInt32();
                int boneOffset = casReader.ReadInt32();
                Console.WriteLine($"TControl {tControlCount} - {tControlOffset}");
                ProcessPointer(casReader, casWriter, tControlCount, tControlOffset, canmLengthDiff, 0x0C);
                Console.WriteLine($"VControl {vControlCount} - {vControlOffset}");
                ProcessPointer(casReader, casWriter, vControlCount, vControlOffset, canmLengthDiff, 0x14);
                Console.WriteLine($"AnmGroup {anmGroupCount} - {anmGroupOffset}");
                ProcessPointer(casReader, casWriter, anmGroupCount, anmGroupOffset, canmLengthDiff, 0x0C);
                //AnmGroup has sub object with a string in it
                for (int i = 0; i < anmGroupCount; i++)
                {
                    long anmPos = (anmGroupOffset + (0x0C * i));
                    casReader.BaseStream.Position = anmPos+4;
                    int count = casReader.ReadInt32();
                    uint offset = casReader.ReadUInt32();
                    Console.WriteLine($"Group {i} - MCAnm {count} - {offset}");
                    ProcessPointer(casReader, casWriter, count, anmPos + offset, canmLengthDiff, 0x24);
                }
                Console.WriteLine($"Bone {boneCount} - {boneOffset}");
                ProcessPointer(casReader, casWriter, boneCount, boneOffset, canmLengthDiff, 0x04);
                Console.WriteLine($"Updating CAS Complete!");
            }
        }

        private static void ProcessPointer(BinaryReader reader, BinaryWriter writer, int amount, long startOffset, int adjustment, int objSize)
        {
            for (int i = 0; i < amount; i++)
            {
                long stringRefPos = startOffset + (objSize * i);
                reader.BaseStream.Position = stringRefPos;
                uint stringRef = reader.ReadUInt32();
                uint absAdjustment = (uint)Math.Abs(adjustment);
                if (adjustment > 0)
                {
                    stringRef += absAdjustment;
                }
                else
                {
                    stringRef -= absAdjustment;
                }

                writer.BaseStream.Position = stringRefPos;
                writer.Write(stringRef);
            }
        }

        private static string ReadString(BinaryReader reader)
        {
            // Read bytes until encountering two consecutive null bytes
            byte[] bytes = new byte[2];
            MemoryStream stringStream = new MemoryStream();
            while (reader.Read(bytes, 0, 2) == 2)
            {
                // Check if the bytes are null
                if (bytes[0] == 0 && bytes[1] == 0)
                {
                    break; // End of string reached
                }

                // Write non-null bytes to the MemoryStream
                stringStream.Write(bytes, 0, 2);
            }

            // Convert the MemoryStream to a string using UTF-16 encoding
            return Encoding.Unicode.GetString(stringStream.ToArray());
        }
        
        private static bool ByteArraysEqual(byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length)
                return false;

            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i])
                    return false;
            }

            return true;
        }
    }
}