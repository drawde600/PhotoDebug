﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhotoLib.Jpeg;
using PhotoLib.Tiff;
using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace PhotoTests.Prototypes
{
    [TestClass]
    public class SRawII
    {
        struct DataBuf
        {
            public ushort Y;
            public short Cb;
            public short Cr;
        }

        struct DiffBuf
        {
            public short Y1;
            public short Y2;
            public short Cb;
            public short Cr;
        }

        static int cc = 0;

        [TestMethod]
        public void DumpImage3SRawTest()
        {
            // 2592 x 1728, Canon EOS 7D, 1/160 sec. f/1.8 85mm, SRAW   
            // const string Folder = @"D:\Users\Greg\Pictures\2013_10_14\";
            // DumpImage3SRaw(Folder, "IMG_4194.CR2");
            const string Folder = @"D:\Users\Greg\Pictures\2016-02-26\";
            DumpImage3SRaw(Folder, "003.CR2");
        }

        private static void DumpImage3SRaw(string folder, string file)
        {
            var fileName2 = folder + file;
            using (var fileStream = File.Open(fileName2, FileMode.Open, FileAccess.Read))
            {
                var binaryReader = new BinaryReader(fileStream);
                var rawImage = new RawImage(binaryReader);

                // Image #3 is a raw image compressed in ITU-T81 lossless JPEG
                {
                    var image = rawImage.Directories.Skip(3).First();
                    var compression = image.Entries.Single(e => e.TagId == 0x0103 && e.TagType == 3).ValuePointer;
                    Assert.AreEqual(6u, compression);
                    var offset = image.Entries.Single(e => e.TagId == 0x0111 && e.TagType == 4).ValuePointer;
                    // Assert.AreEqual(0x2D42DCu, offset);
                    var count = image.Entries.Single(e => e.TagId == 0x0117 && e.TagType == 4).ValuePointer;
                    // Assert.AreEqual(0x1501476u, count);

                    // 0xC640 UShort 16-bit: [0x000119BE] (3): 1, 2960, 2960, 
                    var imageFileEntry = image.Entries.Single(e => e.TagId == 0xC640 && e.TagType == 3);
                    var slices = imageFileEntry.ValuePointer;
                    // Assert.AreEqual(0x000119BEu, slices);
                    var number = imageFileEntry.NumberOfValue;
                    Assert.AreEqual(3u, number);
                    var sizes = RawImage.ReadUInts16(binaryReader, imageFileEntry);
                    CollectionAssert.AreEqual(new[] { (ushort)5, (ushort)864, (ushort)864 }, sizes);

                    binaryReader.BaseStream.Seek(offset, SeekOrigin.Begin);
                    var startOfImage = new StartOfImage(binaryReader, offset, count); // { ImageData = new ImageData(binaryReader, count) };

                    var startOfFrame = startOfImage.StartOfFrame;
                    Assert.AreEqual(1728u, startOfFrame.ScanLines);
                    Assert.AreEqual(2592u, startOfFrame.SamplesPerLine);
                    Assert.AreEqual(7776, startOfFrame.Width);

                    // var rowBuf0 = new short[startOfFrame.SamplesPerLine * startOfFrame.Components.Length];
                    // var rowBuf1 = new short[startOfFrame.SamplesPerLine * startOfFrame.Components.Length];
                    // var predictor = new[] { (short)(1 << (startOfFrame.Precision - 1)), (short)(1 << (startOfFrame.Precision - 1)) };
                    Assert.AreEqual(2, startOfImage.HuffmanTable.Tables.Count);
                    var table0 = startOfImage.HuffmanTable.Tables[0x00];
                    var table1 = startOfImage.HuffmanTable.Tables[0x01];

                    // Console.WriteLine(table0.ToString());
                    // Console.WriteLine(table1.ToString());

                    Assert.AreEqual(15, startOfFrame.Precision); // sraw/sraw2

                    // chrominance subsampling factors
                    Assert.AreEqual(3, startOfFrame.Components.Length); // sraw/sraw2

                    // J:a:b = 4:2:2, h/v = 2/1
                    Assert.AreEqual(1, startOfFrame.Components[0].ComponentId);
                    Assert.AreEqual(2, startOfFrame.Components[0].HFactor);
                    Assert.AreEqual(1, startOfFrame.Components[0].VFactor);
                    Assert.AreEqual(0, startOfFrame.Components[0].TableId);

                    Assert.AreEqual(2, startOfFrame.Components[1].ComponentId);
                    Assert.AreEqual(1, startOfFrame.Components[1].HFactor);
                    Assert.AreEqual(1, startOfFrame.Components[1].VFactor);
                    Assert.AreEqual(0, startOfFrame.Components[1].TableId);

                    Assert.AreEqual(3, startOfFrame.Components[2].ComponentId);
                    Assert.AreEqual(1, startOfFrame.Components[2].HFactor);
                    Assert.AreEqual(1, startOfFrame.Components[2].VFactor);
                    Assert.AreEqual(0, startOfFrame.Components[2].TableId);

                    // sraw/sraw2
                    // Y1 Y2 Cb Cr ...
                    // Y1 Cb Cr Y2 x x
                    // Y1 Cb Cr Y2 x x

                    var startOfScan = startOfImage.StartOfScan;
                    // DumpStartOfScan(startOfScan);

                    Assert.AreEqual(1, startOfScan.Bb1);    // Start of spectral or predictor selection
                    Assert.AreEqual(0, startOfScan.Bb2);    // end of spectral selection
                    Assert.AreEqual(0, startOfScan.Bb3);    // successive approximation bit positions
                    Assert.AreEqual(3, startOfScan.Components.Length);   // sraw/sraw2

                    Assert.AreEqual(1, startOfScan.Components[0].Id);
                    Assert.AreEqual(0, startOfScan.Components[0].Dc);
                    Assert.AreEqual(0, startOfScan.Components[0].Ac);

                    Assert.AreEqual(2, startOfScan.Components[1].Id);
                    Assert.AreEqual(1, startOfScan.Components[1].Dc);
                    Assert.AreEqual(0, startOfScan.Components[1].Ac);

                    Assert.AreEqual(3, startOfScan.Components[2].Id);
                    Assert.AreEqual(1, startOfScan.Components[2].Dc);
                    Assert.AreEqual(0, startOfScan.Components[2].Ac);

                    // DumpCompressedData(startOfImage);

                    // horz sampling == 1
                    startOfImage.ImageData.Reset();

                    var memory = new DataBuf[startOfFrame.ScanLines][];          // [1728][]
                    for (var line = 0; line < startOfFrame.ScanLines; line++)   // 0 .. 1728
                    {
                        var pp = (line == 0)
                                ? new DataBuf { Y = 0x8000 - 3720, Cb = 0, Cr = 0 }
                                : new DataBuf { Y = 0x4000 - 3720, Cb = 0, Cr = 0 };
                        memory[line] = ProcessLine15321B(line, startOfFrame.SamplesPerLine, startOfImage, pp, table0, table1);  //2592
                    }
                    Assert.AreEqual(8957952, cc);
                    Assert.AreEqual(3, startOfImage.ImageData.DistFromEnd);
                    MakeBitmap(memory, folder, sizes);
                }
            }
        }

        private static DataBuf[] ProcessLine15321B(int line, int samplesPerLine, StartOfImage startOfImage, DataBuf pp, HuffmanTable table0, HuffmanTable table1)
        {
            var diff = new DiffBuf[samplesPerLine / 2];         // 1296
            for (var x = 0; x < samplesPerLine / 2; x++)        // 1296
            {
                diff[x].Y1 = ProcessColor(startOfImage, table0);
                diff[x].Y2 = ProcessColor(startOfImage, table0);
                diff[x].Cb = ProcessColor(startOfImage, table1);
                diff[x].Cr = ProcessColor(startOfImage, table1);
                cc += 4;

                if (line < 4 && x < 4)
                {
                    Console.WriteLine("{0}, {1}: {2}, {3}, {4}, {5}", line, x, diff[x].Y1, diff[x].Y2, diff[x].Cb, diff[x].Cr);
                }
            }

            var memory = new DataBuf[samplesPerLine];
            for (var x = 0; x < samplesPerLine / 2; x++)
            {
                if (x == 0)
                {
                    var predY = pp.Y;
                    pp.Y += (ushort)diff[x].Y1;
                    pp.Y += (ushort)diff[x].Y2;
                    memory[2 * x + 0].Y = (ushort)(predY + diff[x].Y1);
                    memory[2 * x + 1].Y = (ushort)(predY + diff[x].Y1 + diff[x].Y2);

                    var predCr = pp.Cr;
                    pp.Cr += diff[x].Cr;
                    memory[2 * x + 0].Cr = (short)(predCr + diff[x].Cr);
                    memory[2 * x + 1].Cr = (short)(predCr + diff[x].Cr);

                    var predCb = pp.Cb;
                    pp.Cb += diff[x].Cb;
                    memory[2 * x + 0].Cb = (short)(predCb + diff[x].Cb);
                    memory[2 * x + 1].Cb = (short)(predCb + diff[x].Cb);
                }
                else
                {
                    memory[2 * x + 0].Y = (ushort)(memory[2 * x - 2].Y + diff[x].Y1);
                    memory[2 * x + 1].Y = (ushort)(memory[2 * x - 1].Y + diff[x].Y2);
                    memory[2 * x + 0].Cr = (short)(memory[2 * x - 2].Cr + diff[x].Cr);
                    memory[2 * x + 1].Cr = (short)(memory[2 * x - 1].Cr + diff[x].Cr);
                    memory[2 * x + 0].Cb = (short)(memory[2 * x - 2].Cr + diff[x].Cb);
                    memory[2 * x + 1].Cb = (short)(memory[2 * x - 1].Cr + diff[x].Cb);
                }
            }

            return memory;
        }

        private static short ProcessColor(StartOfImage startOfImage, HuffmanTable table)
        {
            var hufBits = startOfImage.ImageData.GetValue(table);
            var difCode = startOfImage.ImageData.GetValue(hufBits);
            var difValue = HuffmanTable.DecodeDifBits(hufBits, difCode);
            return difValue;
        }

        private static void MakeBitmap(DataBuf[][] memory, string folder, ushort[] sizes)
        {
            var y = memory.GetLength(0);
            var x = memory[0].GetLength(0);

            //using (var bitmap = new Bitmap(x, y, PixelFormat.Format24bppRgb))
            //{
            //    var size = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            //    var data = bitmap.LockBits(size, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            //    try
            //    {
            //        for (var row = 0; row < y; row++)
            //        {
            //            var scan0 = data.Scan0 + data.Stride * row;
            //            for (var col = 0; col < x; col++)
            //            {
            //                var pt = memory[row, col];
            //                var r = pt.Y + 1.40200 * pt.Cr;
            //                var g = pt.Y - 0.34414 * pt.Cb - 0.71414 * pt.Cr;
            //                var b = pt.Y + 1.77200 * pt.Cb;
            //                Marshal.WriteInt16(scan0, 3 * col + 0, check(b));
            //                Marshal.WriteInt16(scan0, 3 * col + 1, check(g));
            //                Marshal.WriteInt16(scan0, 3 * col + 2, check(r));
            //            }
            //        }
            //    }
            //    finally
            //    {
            //        bitmap.UnlockBits(data);
            //    }

            //    bitmap.Save(folder + "0L2A8897-3.bmp");
            //}

            using (var bitmap = new Bitmap(x, y))
            {
                for (var mrow = 0; mrow < y; mrow++)
                {
                    var rdata = memory[mrow];
                    for (var mcol = 0; mcol < x; mcol++)
                    {
                        var index = mrow * x + mcol;
                        var slice = index / (sizes[1] * y);
                        if (slice >= sizes[0])
                            slice = sizes[0];
                        index -= slice * (sizes[1] * y);
                        var brow = index / sizes[slice < sizes[0] ? 1 : 2];
                        var bcol = index % sizes[slice < sizes[0] ? 1 : 2] + slice * sizes[1];

                        var val = rdata[mcol];
                        PixelSet(bitmap, brow, bcol, val);
                    }
                }

                bitmap.Save(folder + "0L2A8897-3.bmp");
            }
        }

        private static void PixelSet(Bitmap bitmap, int row, int col, DataBuf val)
        {
            //var r = memory[row, col].Y + 1.40200 * memory[row, col].Cr;
            //var g = memory[row, col].Y - 0.34414 * memory[row, col].Cb - 0.71414 * memory[col, row].Cr;
            //var b = memory[row, col].Y + 1.77200 * memory[row, col].Cb;
            var c = (val.Y) >> 7;
            var color = Color.FromArgb((byte)c, (byte)c, (byte)c);
            bitmap.SetPixel(col, row, color);
        }
    }
}