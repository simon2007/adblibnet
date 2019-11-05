using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils;

namespace AdbListTest
{
    [TestClass]
    public class BlockQueueTest
    {
        [TestMethod]
        public void BlockQueueTest1()
        {
            byte[] buffer = new byte[1024];
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] =(byte)( i % Byte.MaxValue);

            BlockQueue<byte[]> queue = new BlockQueue<byte[]>();

            queue.Enqueue(buffer);

            byte[] tmpBuf = queue.Dequeue();

            Assert.AreNotEqual(tmpBuf, null);

            Assert.AreEqual(tmpBuf.Length , buffer.Length);

            for (int i = 0; i < buffer.Length; i++)
                Assert.AreEqual(tmpBuf[i], buffer[i]);
        }
    }
}
