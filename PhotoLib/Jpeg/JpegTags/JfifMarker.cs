﻿// Project Photo Library 0.1
// Copyright © 2013-2016. All Rights Reserved.
// 
// SUBSYSTEM:	PhotoLib
// FILE:		JfifMarker.cs
// AUTHOR:		Greg Eakin

using System;
using System.IO;
using System.Text;

namespace PhotoLib.Jpeg.JpegTags
{
    /// <summary>
    /// APP0 0xFFEO
    /// </summary>
    public class JfifMarker : JpegTag
    {
        #region Fields

        private readonly ushort length;

        #endregion

        #region Constructors and Destructors

        public JfifMarker(BinaryReader binaryReader)
            : base(binaryReader)
        {
            if (Mark != 0xFF || Tag != 0xE0)
            {
                throw new ArgumentException();
            }

            length = (ushort)(binaryReader.ReadByte() << 8 | binaryReader.ReadByte());

            var identifer = binaryReader.ReadBytes(5);
            if (Encoding.ASCII.GetString(identifer) != "JFIF\0")
                throw new ArgumentException();

            var version = (ushort)(binaryReader.ReadByte() << 8 | binaryReader.ReadByte());
            var units = binaryReader.ReadByte();
            var xDensity = (ushort)(binaryReader.ReadByte() << 8 | binaryReader.ReadByte());
            var yDensity = (ushort)(binaryReader.ReadByte() << 8 | binaryReader.ReadByte());
            var xThumb = binaryReader.ReadByte();
            var yThumb = binaryReader.ReadByte();

            var thumbLen = 3 * xThumb * yThumb;
            var thumb = binaryReader.ReadBytes(thumbLen);

            var size = 16 + thumbLen;

            if (size != length)
            {
                throw new ArgumentException();
            }
        }

        #endregion

        #region Public Properties

        public ushort Length
        {
            get
            {
                return length;
            }
        }

        #endregion
    }
}