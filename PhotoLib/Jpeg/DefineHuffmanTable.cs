﻿namespace PhotoLib.Jpeg
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class HuffmanTable
    {
        #region Fields

        private readonly byte[] data1;

        private readonly byte[] data2;

        private readonly Dictionary<int, HCode> dictionary;

        private readonly byte index;

        #endregion

        #region Constructors and Destructors

        public HuffmanTable(byte index, byte[] data1, byte[] data2)
        {
            this.index = index;
            this.data1 = data1;
            this.data2 = data2;
            this.dictionary = BuildTree();
            //for (var i = 0; i < data1.Length; i++)
            //{
            //    Console.WriteLine("Add {0}, {1}", data1[i], data2[i]);
            //    dictionary.Add(data1[i], data2[i]);
            //}
        }

        #endregion

        #region Public Properties

        public byte[] Data1
        {
            get
            {
                return data1;
            }
        }

        public byte[] Data2
        {
            get
            {
                return data2;
            }
        }

        public Dictionary<int, HCode> Dictionary
        {
            get
            {
                return dictionary;
            }
        }

        public byte Index
        {
            get
            {
                return index;
            }
        }

        #endregion

        #region Public Methods and Operators

        public Dictionary<int, HCode> BuildTree()
        {
            var retval = new Dictionary<int, HCode>();

            var offset = 0;
            var bits = 0;
            for (var i = 0; i < 16; i++)
            {
                bits = bits << 1;
                for (var j = 0; j < Data1[i]; j++)
                {
                    var value = new HCode { Length = (byte)(i + 1), Code = Data2[offset] };
                    retval.Add(bits, value);
                    bits++;
                    offset++;
                }
            }
            return retval;
        }

        #endregion

        public struct HCode
        {
            #region Fields

            public byte Code;

            public byte Length;

            #endregion
        }
    }

    public class DefineHuffmanTable : JpegTag
    {
        #region Fields

        private readonly ushort length;

        private readonly Dictionary<byte, HuffmanTable> tables = new Dictionary<byte, HuffmanTable>();

        #endregion

        // DHT: Define Huffman HuffmanTable

        #region Constructors and Destructors

        public DefineHuffmanTable(BinaryReader binaryReader)
            : base(binaryReader)
        {
            if (Mark != 0xFF || Tag != 0xC4)
            {
                throw new ArgumentException();
            }

            length = (ushort)(binaryReader.ReadByte() << 8 | binaryReader.ReadByte());

            var size = 2;
            while (size + 17 <= length)
            {
                // HT Info, bits 0..3 is number, bits 4 is 0 = DC, 1 = AC, bits 5..7 must be zero
                var index = binaryReader.ReadByte();
                var data1 = binaryReader.ReadBytes(16);
                var sum = data1.Aggregate(0, (current, b) => current + b);
                // sum must be <= 256
                var data2 = binaryReader.ReadBytes(sum);
                this.tables.Add(index, new HuffmanTable(index, data1, data2));
                size += 1 + data1.Length + data2.Length;
            }

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

        public Dictionary<byte, HuffmanTable> Tables
        {
            get
            {
                return tables;
            }
        }

        #endregion

        #region Public Methods and Operators

        public static string[] BuildTree(HuffmanTable huffmanTable)
        {
            var retval = new string[huffmanTable.Data2.Length];
            var index = 0;
            var bits = 0;
            for (var i = 0; i < 16; i++)
            {
                bits = bits << 1;
                for (var j = 0; j < huffmanTable.Data1[i]; j++)
                {
                    retval[index] = PrintBits(bits, i);
                    bits++;
                    index++;
                }
            }
            return retval;
        }

        //public static Dictionary<int, HCode> BuildTree2(HuffmanTable huffmanTable)
        //{
        //    var retval = new Dictionary<int, HCode>();

        //    var index = 0;
        //    var bits = 0;
        //    for (var i = 0; i < 16; i++)
        //    {
        //        bits = bits << 1;
        //        for (var j = 0; j < huffmanTable.Data1[i]; j++)
        //        {
        //            var value = new HCode { Length = (byte)(i + 1), Code = huffmanTable.Data2[index] };
        //            retval.Add(bits, value);
        //            bits++;
        //            index++;
        //        }
        //    }
        //    return retval;
        //}

        public static int DcValueEncoding(int dcCode, int bits)
        {
            int retval;
            if (dcCode > 0)
            {
                var sign = bits & (1u << (dcCode - 1));
                var num = bits & ((1u << dcCode) - 1);
                retval = sign == 0 ? (int)num - (int)((1u << dcCode) - 1) : (int)num;
            }
            else
            {
                retval = 0;
            }
            return retval;
        }

        public static int Luminance()
        {
            return 0;
        }

        public static string PrintBits(int value, int number)
        {
            var retval = new StringBuilder();
            for (var i = number; i >= 0; i--)
            {
                var mask = 0x01 << i;
                retval.Append((value & mask) != 0 ? '1' : '0');
            }
            return retval.ToString();
        }

        public void DumpTable()
        {
            foreach (var table in tables.Values)
            {
                // HT Info, bits 0..3 is number, bits 4 is 0 = DC, 1 = AC, bits 5..7 must be zero
                var tableNumber = table.Index & 0x0F;
                var tableType = (table.Index & 0x10) == 0 ? "DC" : "AC";
                Console.WriteLine("HuffmanTable {0} {1}", tableType, tableNumber);
                var bits = BuildTree(table);

                var index = 0;
                for (byte i = 0; i < 16; i++)
                {
                    if (table.Data1[i] <= 0)
                    {
                        continue;
                    }

                    Console.Write("{0} : ", i + 1);
                    for (var j = 0; j < table.Data1[i]; j++)
                    {
                        Console.Write("{0:x} ({1}) ", table.Data2[index], bits[index]);
                        index++;
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();
            }
        }

        #endregion

        //public struct HCode
        //{
        //    #region Fields

        //    public byte Code;

        //    public byte Length;

        //    #endregion
        //}
    }
}