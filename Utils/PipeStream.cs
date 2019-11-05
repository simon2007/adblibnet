using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class PipeStream
    {
        private readonly BlockQueue<byte[]> queue;


        public PipeStream()
        {
            queue = new BlockQueue<byte[]>(64);
        }

        public void Write(byte[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            byte[] buf = new byte[count];
            Array.Copy(buffer, offset, buf, 0, count);
            queue.Enqueue(buf);
        }

        private byte[] currentReadBlock;
        private int currentReadOffset;

        public int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer, offset, count, (readCount,c) =>readCount>0 );
        }
        public int ReadWithNoBlock(byte[] buffer, int offset, int count)
        {
            return Read(buffer, offset, count, (readCount, c)=>false);
        }

        private int Read(byte[] buffer, int offset, int count,Func<int,int,bool> onNoBufferedAction)
        {
            lock (queue)
            {
                byte[] buf = currentReadBlock;
                int bufOffset = currentReadOffset;
                if (buf == null)
                    bufOffset = 0;

                int readCount = 0;

                while (count > 0)
                {
                    if (buf == null)
                    {
                        if (queue.Count <= 0)
                        {
                            if (!onNoBufferedAction(readCount, count))
                                break;
                        }
                        buf = queue.Dequeue();
                    }

                    int minCount = Math.Min(count, buf.Length - bufOffset);
                    Array.Copy(buf, bufOffset, buffer, offset, minCount);

                    offset += minCount;
                    count -= minCount;

                    if (minCount + bufOffset == buf.Length)
                    {
                        buf = null;
                        bufOffset = 0;
                    }
                    else
                        bufOffset += minCount;

                    readCount += minCount;
                }

                currentReadBlock = buf;
                currentReadOffset = bufOffset;
                return readCount;
            }
        }

        public int Read(byte[] buffer)
        {
            return Read(buffer, 0, buffer.Length);
        }

        public int Fill(byte[] buffer)
        {
            return Fill(buffer, 0, buffer.Length);
        }

        public int Fill(byte[] buffer, int offset, int count)
        {
            return Read(buffer, offset, count, (readCount, c) => c>0);
        }


        public void Close()
        {

        }
    }
}
