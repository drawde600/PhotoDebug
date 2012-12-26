﻿namespace PhotoLib
{
    using System;
    using System.IO;

    using PhotoLib.Utilities;

    public class ImageFileDirectory
    {
        #region Fields

        private readonly ImageFileEntry[] entries;

        private readonly uint nextEntry;

        #endregion

        #region Constructors and Destructors

        public ImageFileDirectory(ushort length)
        {
            this.entries = new ImageFileEntry[length];
        }

        public ImageFileDirectory(BinaryReader binaryReader, uint start)
        {
            if (binaryReader.BaseStream.Position != start)
            {
                binaryReader.BaseStream.Seek(start, SeekOrigin.Begin);
            }

            var length = binaryReader.ReadUInt16();
            this.entries = new ImageFileEntry[length];
            for (var i = 0; i < this.Length; i++)
            {
                this.entries[i] = new ImageFileEntry(binaryReader);
            }
            var next = binaryReader.ReadUInt32();
            this.nextEntry = next;
            Console.WriteLine("### Directory [0x{0}], {1}, [0x{2}]", start.ToString("X6"), length, next.ToString("X6"));

            var x = -1;
            foreach (var entry in this.Entries)
            {
                x++;

                if (entry.TagId == 0x8769)
                {
                    Console.WriteLine("{0:2})  [0x{1}] Image File Directory ({2}):", x, entry.ValuePointer.ToString("X6"), entry.NumberOfValue);
                    var tags = new ImageFileDirectory(binaryReader, entry.ValuePointer);
                }
                else
                {
                    const string BlockHeader = "{0:2})  0x{1} {2}: ";
                    const string SingleItem = "[0x{0}] ({1}): ";
                    switch (entry.TagType)
                    {
                        case 0x01:
                            Console.Write(BlockHeader, x, entry.TagId.ToString("X4"), "Byte");
                            Console.WriteLine(SingleItem, entry.ValuePointer.ToString("X6"), entry.NumberOfValue);
                            break;

                        case 0x02:
                            Console.Write(BlockHeader, x, entry.TagId.ToString("X4"), "Ascii");

                            Console.Write(SingleItem, entry.ValuePointer.ToString("X6"), entry.NumberOfValue);
                            if (binaryReader.BaseStream.Position != entry.ValuePointer)
                            {
                                binaryReader.BaseStream.Seek(entry.ValuePointer, SeekOrigin.Begin);
                            }

                            for (var j = 0; j < entry.NumberOfValue - 1; j++)
                            {
                                var us = binaryReader.ReadByte();
                                Console.Write("{0}", (char)us);
                            }
                            binaryReader.ReadByte();
                            Console.WriteLine();
                            break;

                        case 0x03:
                            Console.Write(BlockHeader, x, entry.TagId.ToString("X4"), "Short");
                            if (entry.NumberOfValue == 1)
                            {
                                Console.Write("{0}", entry.ValuePointer);
                            }
                            else
                            {
                                Console.Write(SingleItem, entry.ValuePointer.ToString("X6"), entry.NumberOfValue);
                                if (binaryReader.BaseStream.Position != entry.ValuePointer)
                                {
                                    binaryReader.BaseStream.Seek(entry.ValuePointer, SeekOrigin.Begin);
                                }

                                for (var j = 0; j < entry.NumberOfValue; j++)
                                {
                                    var us = binaryReader.ReadUInt16();
                                    Console.Write("{0}, ", us);
                                }
                            }
                            Console.WriteLine();
                            break;

                        case 0x04:
                            Console.Write(BlockHeader, x, entry.TagId.ToString("X4"), "Long");
                            Console.Write(SingleItem, entry.ValuePointer.ToString("X6"), entry.NumberOfValue);
                            if (entry.NumberOfValue == 1)
                            {
                                Console.WriteLine("{0}", entry.ValuePointer);
                            }
                            else
                            {
                                if (binaryReader.BaseStream.Position != entry.ValuePointer)
                                {
                                    binaryReader.BaseStream.Seek(entry.ValuePointer, SeekOrigin.Begin);
                                }

                                for (var j = 0; j < entry.NumberOfValue; j++)
                                {
                                    var long1 = binaryReader.ReadUInt32();
                                    Console.Write("{0} ", long1.ToString("X4"));
                                }
                                Console.WriteLine();
                            }
                            break;

                        case 0x05:
                            Console.Write(BlockHeader, x, entry.TagId.ToString("X4"), "Rational");
                            Console.Write("[0x{0}] (2):", entry.ValuePointer.ToString("X6"));
                            if (binaryReader.BaseStream.Position != entry.ValuePointer)
                            {
                                binaryReader.BaseStream.Seek(entry.ValuePointer, SeekOrigin.Begin);
                            }

                            var us1 = binaryReader.ReadUInt32();
                            var us2 = binaryReader.ReadUInt32();
                            Console.WriteLine("{0}/{1} = {2}", us1, us2, us1 / (double)us2);
                            break;

                        case 0x07:
                            Console.Write(BlockHeader, x, entry.TagId.ToString("X4"), "Byte[]");
                            if (entry.NumberOfValue <= 4)
                            {
                                Console.Write("{0}, ", entry.ValuePointer >> 0 & 0xFF);
                                Console.Write("{0}, ", entry.ValuePointer >> 8 & 0xFF);
                                Console.Write("{0}, ", entry.ValuePointer >> 16 & 0xFF);
                                Console.WriteLine("{0}", entry.ValuePointer >> 24 & 0xFF);
                            }
                            else
                            {
                                Console.WriteLine(SingleItem, entry.ValuePointer.ToString("X6"), entry.NumberOfValue);
                            }
                            break;

                        case 0x0A:
                            Console.Write(BlockHeader, x, entry.TagId.ToString("X4"), "SRational");
                            Console.Write(SingleItem, entry.ValuePointer.ToString("X6"), entry.NumberOfValue);
                            if (binaryReader.BaseStream.Position != entry.ValuePointer)
                            {
                                binaryReader.BaseStream.Seek(entry.ValuePointer, SeekOrigin.Begin);
                            }

                            var s1 = binaryReader.ReadInt32();
                            var s2 = binaryReader.ReadUInt32();
                            Console.WriteLine("{0}/{1} = {2}", s1, s2, s1 / (double)s2);
                            break;

                        default:
                            Console.Write(BlockHeader, x, entry.TagId.ToString("X4"), "Undefined");
                            throw new NotImplementedException("Undfined message {0}".FormatWith(entry.TagType));
                    }
                }
            }

            // TODO move file chekcer to another class
            //Console.Write("EOB [0x{0:x}]: ", binaryReader.BaseStream.Position);
            //var readBytes = binaryReader.ReadBytes(20);
            //foreach (var t in readBytes)
            //{
            //    Console.Write("0x{0:x} ", t);
            //}
            //Console.WriteLine();
        }

        #endregion

        #region Public Properties

        public ImageFileEntry[] Entries
        {
            get
            {
                return this.entries;
            }
        }

        public ushort Length
        {
            get
            {
                return (ushort)this.Entries.Length;
            }
        }

        public uint NextEntry
        {
            get
            {
                return this.nextEntry;
            }
        }

        #endregion
    }
}