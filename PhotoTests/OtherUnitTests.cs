﻿// Copyright © 2013-2016. All Rights Reserved.
// 
// SUBSYSTEM:	PhotoTests
// FILE:		OtherUnitTests.cs
// AUTHOR:		Greg Eakin

using PhotoLib.Jpeg.JpegTags;

namespace PhotoTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using PhotoLib.Jpeg;
    using PhotoLib.Tiff;
    using PhotoLib.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;

    [TestClass]
    public class OtherUnitTests
    {
        [Ignore]
        [TestMethod]
        public void DumpHuffmanTable()
        {
            const string directory = @"..\..\..\Samples\";
            const string fileName2 = directory + "huff_simple0.jpg";

            using (var fileStream = File.Open(fileName2, FileMode.Open, FileAccess.Read))
            using (var binaryReader = new BinaryReader(fileStream))
            {
                binaryReader.BaseStream.Seek(0x000000D0u, SeekOrigin.Begin);
                var huffmanTable = new DefineHuffmanTable(binaryReader);
                Assert.AreEqual(0xFF, huffmanTable.Mark);
                Assert.AreEqual(0xC4, huffmanTable.Tag);
                Console.WriteLine(huffmanTable);
            }
        }

        [Ignore]
        [TestMethod]
        public void TestMethod8()
        {
            const string directory = @"..\..\..\Samples\";
            const string fileName2 = directory + "IMAG0086.jpg";

            using (var fileStream = File.Open(fileName2, FileMode.Open, FileAccess.Read))
            using (var binaryReader = new BinaryReader(fileStream))
            {
                var startOfImage = new StartOfImage(binaryReader, 0x00u, (uint)fileStream.Length);
                Assert.AreEqual(0xFF, startOfImage.Mark);
                Assert.AreEqual(0xD8, startOfImage.Tag); // JPG_MARK_SOI

                var huffmanTable = startOfImage.HuffmanTable;
                Assert.AreEqual(0xFF, huffmanTable.Mark);
                Assert.AreEqual(0xC4, huffmanTable.Tag);
                Console.WriteLine(huffmanTable);
            }
        }

        public void TestMethodA()
        {
            // it is the width of a
            // row.	The initial values at the beginning of each row is the RG/GB value of
            // its nearest previous row beginning.  For the first row, the initial row
            // values are 1/2 the bit range defined by the precision.  Thus for 12-bit
            // precision:
            //     Pix[Row, Col] = Val
            //     Pix[0,0] = (1 << (Precision - 1)) + Diff
            //     Pix[0,1] = (1 << (Precision - 1)) + Diff
            // and for n >= 1
            //     Pix[n,0] = Pix[n-2,0] + Diff
            //     Pix[n,1] = Pix[n-2,1] + Diff
            // while for any other Row/Column
            //     Pix[R,C] = Pix[R,C-2] + Diff
        }

        [Ignore]
        [TestMethod]
        public void TestMethodB()
        {
            // const string Directory = @"C:\Users\Greg\Documents\Visual Studio 2012\Projects\PhotoDebug\Samples\";
            // const string FileName2 = Directory + "IMG_0503.CR2";
            const string directory = @"D:\Users\Greg\Pictures\2013-10-06 001\";
            const string fileName2 = directory + "0L2A8892.CR2";

            using (var fileStream = File.Open(fileName2, FileMode.Open, FileAccess.Read))
            using (var binaryReader = new BinaryReader(fileStream))
            {
                var rawImage = new RawImage(binaryReader);
                var imageFileDirectory = rawImage.Directories.Last();

                var strips = imageFileDirectory.Entries.Single(e => e.TagId == 0xC640 && e.TagType == 3).ValuePointer; // TIF_CR2_SLICE
                binaryReader.BaseStream.Seek(strips, SeekOrigin.Begin);
                var x = binaryReader.ReadUInt16();
                var y = binaryReader.ReadUInt16();
                var z = binaryReader.ReadUInt16();

                var address = imageFileDirectory.Entries.Single(e => e.TagId == 0x0111).ValuePointer; // TIF_STRIP_OFFSETS
                var length = imageFileDirectory.Entries.Single(e => e.TagId == 0x0117).ValuePointer; // TIF_STRIP_BYTE_COUNTS
                binaryReader.BaseStream.Seek(address, SeekOrigin.Begin);
                var startOfImage = new StartOfImage(binaryReader, address, length);
                var lossless = startOfImage.StartOfFrame;
                // Assert.AreEqual(4711440, lossless.SamplesPerLine * lossless.ScanLines); // IbSize (IB = new ushort[IbSize])

                var rawSize = address + length - binaryReader.BaseStream.Position - 2;
                // Assert.AreEqual(23852856, rawSize); // RawSize (Raw = new byte[RawSize]
                startOfImage.ImageData = new ImageData(binaryReader, (uint)rawSize);
                // var table0 = startOfImage.HuffmanTable.Tables[0x00];

                var buffer = new byte[rawSize];

                var rp = 0;
                for (var jrow = 0; jrow < lossless.ScanLines; jrow++)
                {
                    for (var jcol = 0; jcol < lossless.SamplesPerLine; jcol++)
                    {
                        var val = startOfImage.ImageData.RawData[rp++];
                        if (val == 0xFF)
                        {
                            var code = startOfImage.ImageData.RawData[rp];
                            if (code == 0)
                            {
                                rp++;
                            }
                            else
                            {
                                Assert.Fail("Invalid code found {0}, {1}", rp, startOfImage.ImageData.RawData[rp]);
                            }
                        }

                        var jidx = jrow * lossless.SamplesPerLine + jcol;
                        var i = jidx / (y * lossless.ScanLines);
                        var j = i >= x;
                        if (j)
                            i = x;
                        jidx -= i * (y * lossless.ScanLines);
                        var row = jidx / (j ? y : z);
                        var col = jidx % (j ? y : z) + i * y;

                        buffer[row * lossless.SamplesPerLine + col] = val;
                    }
                }
            }
        }

        [Ignore]
        [TestMethod]
        public void TestMethodB6()
        {
            // const string Folder = @"C:\Users\Greg\Documents\Visual Studio 2012\Projects\PhotoDebug\Samples\";
            // const string FileName2 = Folder + "IMG_0503.CR2";

            const string folder = @"D:\Users\Greg\Pictures\2013-10-06 001\";
            const string fileName2 = folder + "0L2A8892.CR2";
            const string bitmap = folder + "0L2A8892 B6.BMP";

            //const string Folder = @"C:\Users\Greg\Pictures\2013_06_02\";
            //const string FileName2 = Folder + "IMG_3559.CR2";
            //const string Bitmap = Folder + "IMG_3559.BMP";

            ProcessFile(fileName2, bitmap);
        }

        private static void ProcessFile(string fileName, string bitmap)
        {
            using (var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read))
            using (var binaryReader = new BinaryReader(fileStream))
            {
                var rawImage = new RawImage(binaryReader);
                var imageFileDirectory = rawImage.Directories.Last();

                var strips = imageFileDirectory.Entries.Single(e => e.TagId == 0xC640 && e.TagType == 3).ValuePointer; // TIF_CR2_SLICE
                binaryReader.BaseStream.Seek(strips, SeekOrigin.Begin);
                var x = binaryReader.ReadUInt16();
                var y = binaryReader.ReadUInt16();
                var z = binaryReader.ReadUInt16();

                var address = imageFileDirectory.Entries.Single(e => e.TagId == 0x0111).ValuePointer; // TIF_STRIP_OFFSETS
                var length = imageFileDirectory.Entries.Single(e => e.TagId == 0x0117).ValuePointer; // TIF_STRIP_BYTE_COUNTS
                binaryReader.BaseStream.Seek(address, SeekOrigin.Begin);
                var startOfImage = new StartOfImage(binaryReader, address, length);
                var lossless = startOfImage.StartOfFrame;

                var rawSize = address + length - binaryReader.BaseStream.Position - 2;
                // Assert.AreEqual(23852858, rawSize); // RawSize (Raw = new byte[RawSize]
                startOfImage.ImageData = new ImageData(binaryReader, (uint)rawSize);

                var colors = lossless.Components.Sum(comp => comp.HFactor * comp.VFactor);
                var table0 = startOfImage.HuffmanTable.Tables[0x00];

                // var buffer = new byte[rawSize];
                using (var image1 = new Bitmap(500, 500))
                {
                    for (var jrow = 0; jrow < lossless.ScanLines; jrow++)
                    {
                        var rowBuf = new ushort[lossless.SamplesPerLine * colors];
                        for (var jcol = 0; jcol < lossless.SamplesPerLine; jcol++)
                        {
                            for (var jcolor = 0; jcolor < colors; jcolor++)
                            {
                                //var pred = (ushort)0;
                                //var len = gethuff();
                                //var diff = getbits(len);
                                //var row = pred + diff;

                                var val = GetValue(startOfImage.ImageData, table0);
                                var bits = startOfImage.ImageData.GetSetOfBits(val);
                                rowBuf[jcol * colors + jcolor] = bits;
                            }

                            DumpPixel(jcol, jrow, rowBuf, colors, image1);
                        }
                        // var pp = startOfImage.ImageData.Index;
                    }

                    image1.Save(bitmap);
                }

                // Assert.AreEqual(23852855, startOfImage.ImageData.Index);
                //Console.WriteLine("{0}: ", startOfImage.ImageData.BitsLeft);
                //for (var i = startOfImage.ImageData.Index; i < rawSize - 2; i++)
                //{
                //    Console.WriteLine("{0} ", startOfImage.ImageData.RawData[i].ToString("X2"));
                //}
            }
        }

        private static void DumpPixelDebug(int row, IList<short> rowBuf0, IList<short> rowBuf1)
        {
            const int x = 122; // 2116;
            const int y = 40; // 1416 / 2;

            var q = row - y;
            if (q < 0 || q >= 5)
            {
                return;
            }

            for (var p = 0; p < 5; p++)
            {
                var red = rowBuf0[2 * p + x + 0] - 2047;
                var green = rowBuf0[2 * p + x + 1] - 2047;
                var blue = rowBuf1[2 * p + x + 1] - 2047;
                var green2 = rowBuf1[2 * p + x + 0] - 2047;

                Console.WriteLine("{4}, {5}: {0}, {1}, {2}, {3}", red, green, blue, green2, p + 1, q + 1);
            }
        }

        private static void DumpPixel(int jcol, int jrow, ushort[] rowBuf, int colors, Bitmap image1)
        {
            var p = jcol - 50;
            var q = jrow - 30;
            if (p < 0 || p >= 500 || q < 0 || q >= 500)
            {
                return;
            }

            var bits1 = rowBuf[jcol * colors + 0] >> 2;
            if (bits1 > 0xFF)
            {
                bits1 = 0xFF;
            }

            var bits2 = rowBuf[jcol * colors + 1] >> 2;
            if (bits2 > 0xFF)
            {
                bits2 = 0xFF;
            }

            if (jrow % 2 == 0)
            {
                var red = bits1;
                var green = bits2 >> 1;
                var color = Color.FromArgb(red, green, 0);
                image1.SetPixel(p, q, color);
            }
            else
            {
                var green = bits1 >> 1;
                var blue = bits2;
                var color = Color.FromArgb(0, green, blue);
                image1.SetPixel(p, q, color);
            }
        }

        [TestMethod]
        public void SerialNums()
        {
            const uint cam1 = 3071201378;
            const uint cam1H = cam1 & 0xFFFF0000 >> 8;
            const uint cam1L = cam1 & 0x0000FFFF;
            Assert.AreEqual("ED00053346", "{0}{1}".FormatWith(cam1H.ToString("X4"), cam1L.ToString("D5")));
            // var cam2 = "%04X%05d";
        }

        [TestMethod]
        public void TestMethodD()
        {
            const string fileName = @"..\..\Photos\5DIIIhigh.CR2";

            using (var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read))
            using (var binaryReader = new BinaryReader(fileStream))
            {
                var rawImage = new RawImage(binaryReader);

                var ifid0 = rawImage.Directories.First();
                var make = ifid0[0x010f];
                Assert.AreEqual("Canon", RawImage.ReadChars(binaryReader, make));

                var model = ifid0[0x0110];
                // Assert.AreEqual("Canon EOS 7D", RawImage.ReadChars(binaryReader, model));
                Assert.AreEqual("Canon EOS 5D Mark III", RawImage.ReadChars(binaryReader, model));

                var exif = ifid0[0x8769];
                binaryReader.BaseStream.Seek(exif.ValuePointer, SeekOrigin.Begin);
                var tags = new ImageFileDirectory(binaryReader);
                // tags.DumpDirectory(binaryReader);

                var makerNotes = tags[0x927C];
                binaryReader.BaseStream.Seek(makerNotes.ValuePointer, SeekOrigin.Begin);
                var notes = new ImageFileDirectory(binaryReader);
                // notes.DumpDirectory(binaryReader);

                Assert.AreEqual(0x2A, notes.Entries.Length);
                Assert.AreEqual(0u, notes.NextEntry); // last

                // Camera settings
                //var settings = notes.Entries.Single(e => e.TagId == 0x0001 && e.TagType == 3);
                //Console.WriteLine("Camera Settings id: {0}, type: {1}, count {2}, value {3}", settings.TagId, settings.TagType, settings.NumberOfValue, settings.ValuePointer);
                //binaryReader.BaseStream.Seek(settings.ValuePointer, SeekOrigin.Begin);
                //for (var i = 0; i < settings.NumberOfValue; i++)
                //{
                //    var x = binaryReader.ReadUInt16();
                //    Console.WriteLine("{0} : {1}", i, x);
                //}

                // focus info
                //var focalLength = notes.Entries.Single(e => e.TagId == 0x0001 && e.TagType == 3);
                //Console.WriteLine("Focal Length: {0}, type: {1}, count {2}, value {3}", focalLength.TagId, focalLength.TagType, focalLength.NumberOfValue, focalLength.ValuePointer);
                //binaryReader.BaseStream.Seek(focalLength.ValuePointer, SeekOrigin.Begin);
                //for (var i = 0; i < focalLength.NumberOfValue; i++)
                //{
                //    var x = binaryReader.ReadUInt16();
                //    Console.WriteLine("{0} : {1}", i, x);
                //}

                // Color Balance
                //var colorBalance = notes.Entries.Single(e => e.TagId == 0x4001 && e.TagType == 3);
                //Console.WriteLine("Color Balance: {0}, type: {1}, count {2}, value {3}", colorBalance.TagId, colorBalance.TagType, colorBalance.NumberOfValue, colorBalance.ValuePointer);
                //binaryReader.BaseStream.Seek(colorBalance.ValuePointer, SeekOrigin.Begin);
                //for (var i = 0; i < colorBalance.NumberOfValue; i++)
                //{
                //    var x = binaryReader.ReadUInt16();
                //    if (0x3f <= i && i <= 0x42)
                //        Console.WriteLine("{0} : {1}", i, x);
                //}

                var white = notes[0x4001];
                var whiteData = RawImage.ReadUInts16(binaryReader, white);
                Assert.AreEqual(2032, whiteData[0x003F]);
                Assert.AreEqual(1024, whiteData[0x0040]);
                Assert.AreEqual(1024, whiteData[0x0041]);
                Assert.AreEqual(1702, whiteData[0x0042]);

                var wb = new WhiteBalance(binaryReader, white);
            }
        }

        [TestMethod]
        public void CanonDustDeleteData()
        {
            const string fileName = @"..\..\Photos\5DIIIhigh.CR2";
            using (var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read))
            using (var binaryReader = new BinaryReader(fileStream))
            {
                var rawImage = new RawImage(binaryReader);
                var ifid0 = rawImage.Directories.First();
                var exif = ifid0[0x8769];
                binaryReader.BaseStream.Seek(exif.ValuePointer, SeekOrigin.Begin);
                var tags = new ImageFileDirectory(binaryReader);

                var makerNotes = tags[0x927C];
                binaryReader.BaseStream.Seek(makerNotes.ValuePointer, SeekOrigin.Begin);
                var notes = new ImageFileDirectory(binaryReader);
                // notes.DumpDirectory(binaryReader);

                var data = notes[0x0097];
                binaryReader.BaseStream.Seek(data.ValuePointer, SeekOrigin.Begin);
                for (var i = 0; i < data.NumberOfValue; i++)
                {
                    var x = binaryReader.ReadUInt16();
                    Console.WriteLine("{0} : {1}", i, x);
                }
            }
        }

        [TestMethod]
        public void TestMethodTags()
        {
            const string directory = @"..\..\Photos\";
            const string fileName2 = directory + "5DIIIhigh.CR2";

            using (var fileStream = File.Open(fileName2, FileMode.Open, FileAccess.Read))
            using (var binaryReader = new BinaryReader(fileStream))
            {
                var rawImage = new RawImage(binaryReader);

                var ifd0 = rawImage.Directories.First();
                var make = ifd0[0x010f];
                Assert.AreEqual("Canon", RawImage.ReadChars(binaryReader, make));
                var model = ifd0[0x0110];
                // Assert.AreEqual("Canon EOS 7D", RawImage.ReadChars(binaryReader, model));
                Assert.AreEqual("Canon EOS 5D Mark III", RawImage.ReadChars(binaryReader, model));

                var exif = ifd0[0x8769];
                binaryReader.BaseStream.Seek(exif.ValuePointer, SeekOrigin.Begin);
                var tags = new ImageFileDirectory(binaryReader);

                var makerNotes = tags[0x927C];
                binaryReader.BaseStream.Seek(makerNotes.ValuePointer, SeekOrigin.Begin);
                var notes = new ImageFileDirectory(binaryReader);

                var white = notes[0x4001];
                Console.WriteLine("0x{0:X4}, {1}, {2}, {3}", white.TagId, white.TagType, white.NumberOfValue, white.ValuePointer);
                // var wb = new WhiteBalance(binaryReader, white);
                ReadSomeData(binaryReader, white.ValuePointer);

                var size2 = notes[0x4002];
                Console.WriteLine("0x{0:X4}, {1}, {2}, {3}", size2.TagId, size2.TagType, size2.NumberOfValue, size2.ValuePointer);
                ReadSomeData(binaryReader, size2.ValuePointer);

                var size5 = notes[0x4005];
                Console.WriteLine("0x{0:X4}, {1}, {2}, {3}", size5.TagId, size5.TagType, size5.NumberOfValue, size5.ValuePointer);
                ReadSomeData(binaryReader, size5.ValuePointer);
            }
        }

        private static void ReadSomeData(BinaryReader binaryReader, uint valuePointer)
        {
            if (binaryReader.BaseStream.Position != valuePointer)
            {
                binaryReader.BaseStream.Seek(valuePointer, SeekOrigin.Begin);
            }

            var ar = 0;
            var length = binaryReader.ReadUInt16();
            Console.WriteLine("0x{0:X4} Len = {1} Length", ar, length);
            ar += 2;
        }

        private static short DecodeDifBits(ushort difBits, ushort difCode)
        {
            short dif0;
            if ((difCode & (0x01u << (difBits - 1))) != 0)
            {
                // msb is 1, thus decoded DifCode is positive
                dif0 = (short)difCode;
            }
            else
            {
                // msb is 0, thus DifCode is negative
                var mask = (1 << difBits) - 1;
                var m1 = difCode ^ mask;
                dif0 = (short)(0 - m1);
            }
            return dif0;
        }

        private static ushort GetValue(ImageData imageData, HuffmanTable table)
        {
            var hufIndex = (ushort)0;
            var hufBits = (ushort)0;
            HuffmanTable.HCode hCode;
            do
            {
                hufIndex = imageData.GetNextShort(hufIndex);
                hufBits++;
            }
            while (!table.Dictionary.TryGetValue(hufIndex, out hCode) || hCode.Length != hufBits);

            return hCode.Code;
        }
    }
}
