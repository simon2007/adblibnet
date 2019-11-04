using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using utils;

namespace AdbLib
{

    /**
     * This class provides an abstraction for the ADB message format.
     * @author Cameron Gutman
     */
    class AdbMessage
    {
        /** The command field of the message */
        public uint command;
        /** The arg0 field of the message */
        public uint arg0;
        /** The arg1 field of the message */
        public uint arg1;
        /** The payload of the message */
        public byte[] payload;

        /**
         * Read and parse an ADB message from the supplied input stream.
         * This message is NOT validated.
         * @param in InputStream object to read data from
         * @return An AdbMessage object represented the message read
         * @throws IOException If the stream fails while reading
         */
        public static AdbMessage ReadAdbMessage(BinaryStream inStream) 
        {
            AdbMessage msg = new AdbMessage();
            inStream.IsLittleEndian = true;

			/* Pull out header fields */
			msg.command = inStream.ReadUInt32();
			msg.arg0 = inStream.ReadUInt32();
			msg.arg1 = inStream.ReadUInt32();
			uint payloadLength = inStream.ReadUInt32();
			uint checksum = inStream.ReadUInt32();
			uint magic = inStream.ReadUInt32();

            if (msg.command != (magic ^ 0xFFFFFFFF))
                throw new IOException("message error");

            /* If there's a payload supplied, read that too */
            if (payloadLength > 0)
			{
				msg.payload = new byte[payloadLength];
                inStream.Fill(msg.payload);

                if (getPayloadChecksum(msg.payload) != checksum)
                    throw new IOException("checksum error");
            }
			
			return msg;
		}

        public void WriteTo(BinaryStream inStream)
        {
            /* struct message {
            * 		unsigned command;       // command identifier constant
              * 		unsigned arg0;          // first argument
              * 		unsigned arg1;          // second argument
              * 		unsigned data_length;   // length of payload (0 is allowed)
              * 		unsigned data_check;    // checksum of data payload
              * 		unsigned magic;         // command ^ 0xffffffff
              * };
              */

            inStream.Write(command);
            inStream.Write(arg0);
            inStream.Write(arg1);

            if (payload != null)
            {
                inStream.Write(payload.Length);
                inStream.Write(getPayloadChecksum(payload));
            }
            else
            {
                inStream.Write(0u);
                inStream.Write(0u);
            }

            inStream.Write(command ^ 0xFFFFFFFF);

            if (payload != null)
                inStream.Write(payload, 0, payload.Length);

        }


        /**
         * This function performs a checksum on the ADB payload data.
         * @param payload Payload to checksum
         * @return The checksum of the payload
         */
        private static uint getPayloadChecksum(byte[] payload)
        {
            return getPayloadChecksum(payload, 0, payload.Length);
        }

        private static uint getPayloadChecksum(byte[] payload, int offset, int count)
        {
            uint checksum = 0;
            int end = offset + count;

            for (int i = offset; i < end; i++)
            {
                byte b = payload[i];
                /* We have to manually "unsign" these bytes because Java sucks */
                if (b >= 0)
                    checksum += b;
                else
                    checksum += b + 256u;
            }

            return checksum;
        }


    }
}
