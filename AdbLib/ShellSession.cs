using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdbLib
{
    public class ShellSession : AdbSessionBase
    {


        public ShellSession(AdbConnection adbConnection, uint localId)
            :base(adbConnection,localId)
        {

        }
    }
}
