﻿namespace PhotoTests.Jpeg
{
    using System;
    using System.IO;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using PhotoLib.Jpeg;

    [TestClass]
    public class StartOfImageTests
    {
        #region Public Methods and Operators

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void BadMark()
        {
            var data = new byte[] { 0x00, 0x00 };
            using (var memory = new MemoryStream(data))
            {
                var reader = new BinaryReader(memory);
                var startOfImage = new StartOfImage(reader, 0x0000, (uint)data.Length);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void BadTag()
        {
            var data = new byte[] { 0xFF, 0x00 };
            using (var memory = new MemoryStream(data))
            {
                var reader = new BinaryReader(memory);
                var startOfImage = new StartOfImage(reader, 0x0000, (uint)data.Length);
            }
        }

        [TestMethod]
        public void Mark()
        {
            var data = new byte[] { 0xFF, 0xD8 };
            using (var memory = new MemoryStream(data))
            {
                var reader = new BinaryReader(memory);
                var startOfImage = new StartOfImage(reader, 0x0000, (uint)data.Length);
                Assert.AreEqual(0xFF, startOfImage.Mark);
            }
        }

        [TestMethod]
        public void Tag()
        {
            var data = new byte[] { 0xFF, 0xD8 };
            using (var memory = new MemoryStream(data))
            {
                var reader = new BinaryReader(memory);
                var startOfImage = new StartOfImage(reader, 0x0000, (uint)data.Length);
                Assert.AreEqual(0xD8, startOfImage.Tag);
            }
        }

        #endregion
    }
}