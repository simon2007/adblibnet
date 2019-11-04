using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdbLib
{
    public class SyncSession : AdbSessionBase
    {
        private const String CMD_QUIT = "QUIT";
        private const String CMD_SEND = "SEND";
        private const String CMD_DATA = "DATA";
        private const String CMD_DONE = "DONE";
        private const String CMD_OKAY = "OKAY";



        public SyncSession(AdbConnection adbConn, uint localId) : base(adbConn, localId)
        {
        }


        byte[] htoll(long x)
        {
            byte[] buffer = new byte[4];
            htoll(x, buffer, 0);
            return buffer;
        }

        void htoll(long x, byte[] buffer, int offset)
        {
            buffer[offset + 3] = (byte)(((x) & 0xFF000000) >> 24);
            buffer[offset + 2] = (byte)(((x) & 0x00FF0000) >> 16);
            buffer[offset + 1] = (byte)(((x) & 0x0000FF00) >> 8);
            buffer[offset + 0] = (byte)((x) & 0x000000FF);

        }

        int ltohl(byte[] buffer, int offset)
        {
            int ret = (byte)(((buffer[offset + 3]) & 0xFF000000) >> 24)
                    | (byte)(((buffer[offset + 2]) & 0x00FF0000) >> 16)
                    | (byte)(((buffer[offset + 1]) & 0x0000FF00) >> 8)
                    | (byte)((buffer[offset + 0]) & 0x000000FF);

            return ret;
        }

        const int S_IFREG = 0100000;//普通文件
        const int S_IRUSR = 0400;    /* Read by owner.  */
        const int S_IWUSR = 0200;    /* Write by owner.  */
        const int S_IXUSR = 0100;    /* Execute by owner.  */
        const int S_IRGRP = (S_IRUSR >> 3);    /* Read by group.  */
        const int S_IWGRP = (S_IWUSR >> 3);    /* Write by group.  */
        const int S_IXGRP = (S_IXUSR >> 3);    /* Execute by group.  */

        const int S_IROTH = (S_IRGRP >> 3);    /* Read by others.  */
        const int S_IWOTH = (S_IWGRP >> 3);/* Write by others.  */
        const int S_IXOTH = (S_IXGRP >> 3);    /* Execute by others.  */


        private void quit()
        {
            byte[] command = new byte[8];
            Encoding.ASCII.GetBytes(CMD_QUIT, 0, CMD_QUIT.Length, command, 0);

            Write(command);
        }


        public void Push(String remotePath, String fileName)
        {
            FileInfo file = new FileInfo(fileName);
            if (!file.Exists)
                return;
            Push(remotePath, file);
        }

        public void Push(String remotePath, FileInfo file)
        {
            using (FileStream fileStream = file.OpenRead())
                Push(remotePath, fileStream);
        }

        public void Push(String remotePath, Stream inputStream)
        {

            try
            {
                String tmp = remotePath + "," + (S_IFREG | S_IRUSR | S_IWUSR);


                byte[] buffer = new byte[8];
                Encoding.ASCII.GetBytes(CMD_SEND, 0, CMD_SEND.Length, buffer, 0);

                htoll(tmp.Length, buffer, 4);

                Write(buffer);


                Write(Encoding.UTF8.GetBytes(tmp));

                int len = 21;
                buffer = new byte[adbConn.MaxData];

                Encoding.ASCII.GetBytes(CMD_DATA, 0, CMD_DATA.Length, buffer, 0);

                while ((len = inputStream.Read(buffer, 8, buffer.Length - 8)) > 0)
                {
                    htoll(len, buffer, 4);
                    Write(buffer, 0, len + 8);
                }

                buffer = new byte[8];
                Encoding.ASCII.GetBytes(CMD_DONE, 0, CMD_DONE.Length, buffer, 0);

                long lastModifyTime = 0;
                htoll(lastModifyTime, buffer, 4);
                Write(buffer);

                buffer = Read();

                String head = Encoding.UTF8.GetString(buffer, 0, 4);
                if (head != CMD_OKAY)
                {

                    int ret = ltohl(buffer, 4);
                    Debug.WriteLine("error " + ret);
                    if (ret > 0 && buffer.Length >= ret + 8)
                        throw new IOException(Encoding.UTF8.GetString(buffer, 8, ret));
                    else
                    {
                        byte[] payload = ReadIfExists();
                        if (payload != null)
                            throw new IOException(Encoding.UTF8.GetString(payload, 0, payload.Length));

                        throw new IOException();
                    }
                }
                else
                    Debug.WriteLine("ok");
            }
            catch (IOException e)
            {
                if (!IsClosed)
                    quit();
                throw;
            }
        }
    }
}
