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
        AdbConnection connect;
        [ClassInitialize]
        public void Init()
        {
            TcpClient tc = new TcpClient();
            tc.Connect("192.168.1.126", 5555);
            FileInfo privateKeyFile = new FileInfo("private.key");
            FileInfo publicKeyFile = new FileInfo("public.key");
            AdbCrypto adbCryto;
            if (privateKeyFile.Exists)
            {
                adbCryto = AdbCrypto.loadAdbKeyPair(privateKeyFile, publicKeyFile);
            }
            else
            {
                adbCryto = AdbCrypto.generateAdbKeyPair();
                adbCryto.saveAdbKeyPair(privateKeyFile, publicKeyFile);
            }
            connect = AdbConnection.Create(tc, adbCryto);
            connect.connect();
        }


        [TestMethod]
        private void PushTest()
        {
            SyncSession syncSession = connect.OpenSync();
            File.WriteAllText("test.log", "aaaa");
            syncSession.Push("/data/test.log", new FileInfo("test.log"));
            syncSession.Close();
        }

        [TestMethod]
        private void PullTest()
        {
            SyncSession syncSession = connect.OpenSync();
            syncSession.Pull("/data/test.log", new FileInfo("test.log"));
            syncSession.Close();
        }
    }
}
