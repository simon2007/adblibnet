using System;
using System.IO;

namespace utils
{
    public class BinaryStream:IDisposable
    {
        private Stream baseStream;
        public BinaryStream(Stream stream)
        {
            baseStream = stream;
        }

        public bool IsLittleEndian { get; set; } = BitConverter.IsLittleEndian;

        public void Fill(byte[] buffer)
        {
            Fill(buffer, 0, buffer.Length);
        }

        /**
         * 
         */
        public void Fill(byte[] buffer,int offset,int count)
        {
            while (count>0)
            {
                int len= baseStream.Read(buffer, offset, count );
                if (len <= 0)
                    throw new IOException("Read error(" + len+")");
                
                offset += len;
                count -= len;
            }
        }


        public int Read(byte[] buffer,int offset,int count)
        {
            return baseStream.Read(buffer, offset, count);
        }


        public void Write(byte[] buffer,int offset,int count)
        {
            baseStream.Write(buffer, offset, count);
        }


        private byte[] PrivateRead(int count)
        {
            byte[] buffer = new byte[count];
            Fill(buffer,0, count);
            if (BitConverter.IsLittleEndian != IsLittleEndian)
                Array.Reverse(buffer);
            return buffer;
        }

        public Int16 ReadInt16()
        {
            byte[] buffer = PrivateRead(2);
            return BitConverter.ToInt16(buffer, 0);
        }

        public Int32 ReadInt32()
        {
            byte[] buffer = PrivateRead(4);
            return BitConverter.ToInt32(buffer, 0);
        }


        public Int64 ReadInt64()
        {
            byte[] buffer = PrivateRead(8);
            return BitConverter.ToInt64(buffer, 0);
        }


        public UInt16 ReadUInt16()
        {
            byte[] buffer = PrivateRead(2);
            return BitConverter.ToUInt16(buffer, 0);
        }

        public UInt32 ReadUInt32()
        {
            byte[] buffer = PrivateRead(4);
            return BitConverter.ToUInt32(buffer, 0);
        }


        public UInt64 ReadUInt64()
        {
            byte[] buffer = PrivateRead(8);
            return BitConverter.ToUInt64(buffer, 0);
        }


        private void PrivateWrite(byte[] buffer)
        {
            if (BitConverter.IsLittleEndian != IsLittleEndian)
                Array.Reverse(buffer);

            baseStream.Write(buffer, 0, buffer.Length);
        }

        public void Write(Int16 value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            PrivateWrite(buffer);
        }
        public void Write(Int32 value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            PrivateWrite(buffer);
        }

        public void Write(Int64 value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            PrivateWrite(buffer);
        }

        public void Write(UInt32 value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            PrivateWrite(buffer);
        }

        public void Write(UInt16 value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            PrivateWrite(buffer);
        }

        public void Write(UInt64 value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            PrivateWrite(buffer);
        }

        public void Flush()
        {
            baseStream.Flush();
        }

        public void Dispose()
        {
            baseStream.Close();
            baseStream.Dispose(); 
        }
    }
}
