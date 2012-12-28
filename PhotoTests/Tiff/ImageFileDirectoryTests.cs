﻿namespace PhotoTests.Tiff
{
    using System.IO;
    using System.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using PhotoLib.Tiff;

    [TestClass]
    public class ImageFileDirectoryTests
    {
        #region Static Fields

        private static readonly byte[] Data = { 0x01, 0x00, 0x12, 0x00, 0x00, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x40, 0x14, 0x00, 0x00, 0x00, 0x00 };

        #endregion

        #region Public Methods and Operators

        [TestMethod]
        public void EntriesCount()
        {
            using (var memory = new MemoryStream(Data))
            {
                var reader = new BinaryReader(memory);
                var imageFileDirectory = new ImageFileDirectory(reader);
                Assert.AreEqual(1, imageFileDirectory.Entries.Length);
            }
        }

        [TestMethod]
        public void Entry()
        {
            using (var memory = new MemoryStream(Data))
            {
                var reader = new BinaryReader(memory);
                var imageFileDirectory = new ImageFileDirectory(reader);
                var entry = imageFileDirectory.Entries.First();
                Assert.AreEqual(0x0012, entry.TagId);
            }
        }

        [TestMethod]
        public void NextEntry()
        {
            using (var memory = new MemoryStream(Data))
            {
                var reader = new BinaryReader(memory);
                var imageFileDirectory = new ImageFileDirectory(reader);
                Assert.AreEqual(0x00000000u, imageFileDirectory.NextEntry);
            }
        }

        #endregion
    }
}