﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhotoLib.Tiff;
using System.IO;
using System.Linq;

namespace PhotoTests.Prototypes
{
    [TestClass]
    public class GpsData
    {
        [TestMethod]
        public void DumpGpsData()
        {
            const string fileName = @"C:..\..\Photos\5DIIIhigh.CR2";
            DumpGpsInfo(fileName);
        }

        private static void DumpGpsInfo(string fileName)
        {
            using (var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read))
            using (var binaryReader = new BinaryReader(fileStream))
            {
                var rawImage = new RawImage(binaryReader);
                var image = rawImage.Directories.First();
                var gps = image.Entries.Single(e => e.TagId == 0x8825 && e.TagType == 4).ValuePointer;
                // Assert.AreEqual(70028u, gps);

                DumpGpsInfo(binaryReader, gps);
            }
        }

        private static void DumpGpsInfo(BinaryReader binaryReader, uint offset)
        {
            binaryReader.BaseStream.Seek(offset, SeekOrigin.Begin);

            var tags = new ImageFileDirectory(binaryReader);
            // tags.DumpDirectory(binaryReader);

            Assert.AreEqual(1, tags.Entries.Length);
            CollectionAssert.AreEqual(
                new ushort[] { 0x0000 }, tags.Entries.Select(e => e.TagId).ToArray());

            var imageFileEntry = tags.Entries.Single(e => e.TagId == 0x0000 && e.TagType == 1);
            Assert.AreEqual(4u, imageFileEntry.NumberOfValue);
            // Assert.AreEqual(0x302u, imageFileEntry.ValuePointer);
            var xmpData = RawImage.ReadBytes(binaryReader, imageFileEntry);
            CollectionAssert.AreEqual(new byte[] {0x01, 0x00, 0x0F, 0xA2}, xmpData);
        }
    }
}
