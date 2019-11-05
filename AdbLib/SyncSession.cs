using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace AdbLib
{
    public class SyncSession : AdbSessionBase
    {
        private const String CMD_QUIT = "QUIT";
        private const String CMD_SEND = "SEND";
        private const String CMD_DATA = "DATA";
        private const String CMD_DONE = "DONE";
        private const String CMD_OKAY = "OKAY";

        private const String CMD_RECV = "RECV";
        private const String CMD_FAIL = "FAIL";

        #region Head
        private String GetCommand(byte[] buffer)
            {
                return Encoding.UTF8.GetString(buffer, 0, 4);
                
            }

            private void SetCommand(byte[] buffer,String cmd)
            {
                Encoding.ASCII.GetBytes(cmd, 0, cmd.Length, buffer, 0);
            }
            byte[] Htoll(long x)
            {
                byte[] buffer = new byte[4];
                Htoll(x, buffer, 0);
                return buffer;
            }

            void Htoll(long x, byte[] buffer, int offset)
            {
                buffer[offset + 3] = (byte)(((x) & 0xFF000000) >> 24);
                buffer[offset + 2] = (byte)(((x) & 0x00FF0000) >> 16);
                buffer[offset + 1] = (byte)(((x) & 0x0000FF00) >> 8);
                buffer[offset + 0] = (byte)((x) & 0x000000FF);

            }

            int Ltohl(byte[] buffer, int offset)
            {
                int ret = (((buffer[offset + 3]) ) << 24)
                        | (((buffer[offset + 2]) ) << 16)
                        | (((buffer[offset + 1]) ) << 8)
                        | ((buffer[offset + 0]) );

                return ret;
            }

            private int GetLength(byte[] buffer)
            { 
                return Ltohl(buffer, 4);                 
            }

            private void  SetLength(byte[] buffer,int length)
            {
                Htoll(length, buffer, 4);
            }

#endregion



        public SyncSession(AdbConnection adbConn, uint localId) : base(adbConn, localId)
        {
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


        private void Quit()
        {
            byte[] command = new byte[8];
            Encoding.ASCII.GetBytes(CMD_QUIT, 0, CMD_QUIT.Length, command, 0);

            Write(command);
        }


        public void Push(String remotePath, String fileName)
        {
            FileInfo file = new FileInfo(fileName);
            Push(remotePath, file);
        }

        public void Push(String remotePath, FileInfo file)
        {
            if (!file.Exists)
                return;
            
            using (FileStream fileStream = file.OpenRead())
                Push(remotePath, fileStream);
        }

        public void Push(String remotePath, Stream inputStream)
        {
            Push(remotePath, inputStream, S_IFREG | S_IRUSR | S_IWUSR, (int)DateTime.Now.Ticks);
        }

        public void Push(String remotePath, Stream inputStream,int mode,int time)
        {

            try
            {
                String tmp = remotePath + "," + mode;


                byte[] buffer = new byte[8];
                SetCommand(buffer,CMD_SEND);
                SetLength( buffer, tmp.Length);
                Write(buffer);
                Write(Encoding.UTF8.GetBytes(tmp));

                int len = 21;
                buffer = new byte[adbConn.MaxData];

                SetCommand(buffer, CMD_DATA);

                while ((len = inputStream.Read(buffer, 8, buffer.Length - 8)) > 0)
                {
                    SetLength( buffer, len);
                    Write(buffer, 0, len + 8);
                }

                buffer = new byte[8];
                SetCommand(buffer,CMD_DONE);

                SetLength(buffer, time);
                Write(buffer);

                buffer = new byte[8];
                Fill(buffer);

                String head = GetCommand(buffer);
                if (head != CMD_OKAY)
                {

                    int ret = GetLength(buffer);
                    Debug.WriteLine("error " + ret);

                    if (ret > 0)
                    {
                        byte[] payload = new byte[ret];
                        Fill(payload);
                            throw new IOException(Encoding.UTF8.GetString(payload, 0, payload.Length));
                    }
                        throw new IOException();
                    
                }
                else
                    Debug.WriteLine("ok");
            }
            catch (IOException)
            {
                if (!IsClosed)
                    Quit();
                throw;
            }
        }



        public void Pull(String remotePath, String path)
        {
            FileInfo localFile = new FileInfo(path);
            Pull(remotePath, localFile);
        }

        public void Pull(String remotePath, FileInfo localFile)
        {
            using (FileStream fs = localFile.OpenWrite())
                Pull(remotePath, fs);
        }

        public void Pull(String remotePath,Stream stream)
        {
            byte[] head = new byte[8];
            SetCommand(head, CMD_RECV);
            SetLength(head, remotePath.Length);
            Write(head);
            Write(Encoding.UTF8.GetBytes(remotePath));

            //head
            Fill(head);
            String command = GetCommand(head);
            int len = GetLength(head);

            if (command == CMD_FAIL)
            {
                byte[] buffer = new byte[len];
                Fill(buffer);
                throw new IOException(Encoding.UTF8.GetString(buffer));
            }



            if(command == CMD_DATA || command == CMD_DONE)
            {
                while(command != CMD_DONE)
                {

                    byte[] buffer = new byte[len];
                    Fill(buffer);

                    stream.Write(buffer, 0, buffer.Length);

                    Fill(head);
                    command = GetCommand(head);
                    len = GetLength(head);
                }
            }

        }
    }
}
