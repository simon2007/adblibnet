using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdbLib
{
    public class ShellSession : AdbSessionBase
    {


        public ShellSession(AdbConnection adbConnection, uint localId)
            :base(adbConnection,localId)
        {
            Thread thread = new Thread(Run);
            thread.IsBackground = true;
            thread.Start();
        }



        private void Run()
        {
            while(!IsClosed)
            {
                byte[] payload = Read();
                Debug.WriteLine(Encoding.UTF8.GetString(payload));
            }
        }



        public void Execute(string cmd)
        {
            Write(cmd);
        }


    }
}
