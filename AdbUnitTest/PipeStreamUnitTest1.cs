using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utils;

namespace AdbListTest
{
    [TestClass]
    public class PipeStreamUnitTest1
    {
        [TestMethod]
        public void PipeStreamTest()
        {
            //写入的大小和读取的大小一样
            PipeStream pipStream = new PipeStream();
            byte[] buf1 = new byte[255];
            for (int i = 0; i < buf1.Length; i++)
                buf1[i] =(byte) i;
            pipStream.Write(buf1);

            byte[] buf2 = new byte[255];
            for (int i = 0; i < buf2.Length; i++)
                buf2[i] = (byte)(Byte.MaxValue- i);
            pipStream.Write(buf2);

            byte[] tmpBuf = new byte[buf1.Length];
            pipStream.Read(tmpBuf);
            for (int i = 0; i < buf1.Length; i++)
                Assert.AreEqual(buf1[i], tmpBuf[i], "buf1[" + i + "]");


            tmpBuf = new byte[buf2.Length];
            pipStream.Read(tmpBuf);
            for (int i = 0; i < buf2.Length; i++)
                Assert.AreEqual(buf2[i], tmpBuf[i], "buf2[" + i + "]");
        }

        [TestMethod]
        public void PipeStreamTest2()
        {
            //读取的大小=写入大小*n
            PipeStream pipStream = new PipeStream();
            byte[] buf1 = new byte[255];
            for (int i = 0; i < buf1.Length; i++)
                buf1[i] = (byte)i;
            pipStream.Write(buf1);

            byte[] buf2 = new byte[255];
            for (int i = 0; i < buf2.Length; i++)
                buf2[i] = (byte)(Byte.MaxValue - i);
            pipStream.Write(buf2);

            byte[] tmpBuf = new byte[buf1.Length + buf2.Length];
            pipStream.Read(tmpBuf);
            for (int i = 0; i < buf1.Length; i++)
                Assert.AreEqual(buf1[i], tmpBuf[i], "buf1[" + i + "]");

            for (int i = buf1.Length; i < tmpBuf.Length; i++)
                Assert.AreEqual(buf2[i - buf1.Length], tmpBuf[i], "buf2[" + (i-buf1.Length) + "]");
        }


        [TestMethod]
        public void PipeStreamTest3()
        {
            
            PipeStream pipStream = new PipeStream();
            byte[] buf1 = new byte[255];
            for (int i = 0; i < buf1.Length; i++)
                buf1[i] = (byte)i;
            pipStream.Write(buf1);

            byte[] buf2 = new byte[255];
            for (int i = 0; i < buf2.Length; i++)
                buf2[i] = (byte)(Byte.MaxValue - i);
            pipStream.Write(buf2);

            byte[] tmpBuf = new byte[buf1.Length + 10];

            pipStream.Read(tmpBuf);
            for (int i = 0; i < buf1.Length; i++)
                Assert.AreEqual(buf1[i], tmpBuf[i], "buf1[" + i + "]");

            for (int i = buf1.Length; i < tmpBuf.Length; i++)
                Assert.AreEqual(buf2[i - buf1.Length], tmpBuf[i], "buf2[" + i + "]");


            tmpBuf = new byte[buf2.Length - 10];
            pipStream.Read(tmpBuf);
            for (int i = 0; i < tmpBuf.Length; i++)
                Assert.AreEqual(buf2[i+10], tmpBuf[i], "buf2[" + (i+10) + "]");
        }



        [TestMethod]
        public void PipeStreamTest4()
        {

            PipeStream pipStream = new PipeStream();
            byte[] buf1 = new byte[255];
            for (int i = 0; i < buf1.Length; i++)
                buf1[i] = (byte)i;
            pipStream.Write(buf1);

            byte[] buf2 = new byte[255];
            for (int i = 0; i < buf2.Length; i++)
                buf2[i] = (byte)(Byte.MaxValue - i);
            pipStream.Write(buf2);

            byte[] tmpBuf = new byte[buf1.Length - 10];
            pipStream.Read(tmpBuf);
            for (int i = 0; i < tmpBuf.Length; i++)
                Assert.AreEqual(buf1[i], tmpBuf[i], "buf1[" + i + "]");


            tmpBuf = new byte[buf2.Length + 10];
            pipStream.Read(tmpBuf);

            for (int i = 0; i < 10; i++)
                Assert.AreEqual(buf1[i +buf1.Length-10], tmpBuf[i], "buf1[" + (i + buf1.Length - 10) + "]");



            for (int i = 10; i < tmpBuf.Length; i++)
                Assert.AreEqual(buf2[i - 10], tmpBuf[i], "buf2[" + (i - 10) + "]");
        }
    }
}
