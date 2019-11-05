using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AdbLib;
using System.IO;
using System.Net.Sockets;

namespace AdbListTest
{
    [TestClass]
    public class AdbSyncUnitTest
    {

        public AdbConnection CreateAdbConnection()
        {
           
            AdbConnection connect = AdbConnection.Create("192.168.1.178", 5555);
            connect.Connect();
            return connect;
        }


        [TestMethod]
        public void PushTest()
        {
            using (AdbConnection connect = CreateAdbConnection())
            {
                SyncSession syncSession = connect.OpenSync();
                File.WriteAllText("test.log", "aaaa");
                syncSession.Push("/data/test.log", new FileInfo("test.log"));
                syncSession.Close();

                connect.Close();
            }
        }

        [TestMethod]
        public void PullTest()
        {
            AdbConnection connect = CreateAdbConnection();
            SyncSession syncSession = connect.OpenSync();
            syncSession.Pull("/data/test.log", new FileInfo("test.log"));
            syncSession.Close();

            connect.Close();
        }
    }
}
