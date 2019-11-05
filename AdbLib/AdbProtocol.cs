using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdbLib
{
    class AdbProtocol
    {
        /** The length of the ADB message header */
        public const uint ADB_HEADER_LENGTH = 24;

        public const uint CMD_SYNC = 0x434e5953;

        /** CNXN is the connect message. No messages (except AUTH) 
         * are valid before this message is received. */
        public const uint CMD_CNXN = 0x4e584e43;

        /** The current version of the ADB protocol */
        public const uint CONNECT_VERSION = 0x01000000;

        /** The maximum data payload supported by the ADB implementation */
        public const uint CONNECT_MAXDATA = 4096;

        /** The payload sent with the connect message */
        public static byte[] CONNECT_PAYLOAD = Encoding.UTF8.GetBytes("host::\0");


        /** AUTH is the authentication message. It is part of the
         * RSA public key authentication added in Android 4.2.2. */
        public const uint CMD_AUTH = 0x48545541;

        /** This authentication type represents a SHA1 hash to sign */
        public const uint AUTH_TYPE_TOKEN = 1;

        /** This authentication type represents the signed SHA1 hash */
        public const uint AUTH_TYPE_SIGNATURE = 2;

        /** This authentication type represents a RSA public key */
        public const uint AUTH_TYPE_RSA_PUBLIC = 3;

        /** OPEN is the open stream message. It is sent to open
         * a new stream on the target device. */
        public const uint CMD_OPEN = 0x4e45504f;

        /** OKAY is a success message. It is sent when a write is
         * processed successfully. */
        public const uint CMD_OKAY = 0x59414b4f;

        /** CLSE is the close stream message. It it sent to close an
         * existing stream on the target device. */
        public const uint CMD_CLSE = 0x45534c43;

        /** WRTE is the write stream message. It is sent with a payload
         * that is the data to write to the stream. */
        public const uint CMD_WRTE = 0x45545257;


        /**
         * This function generates an ADB message given the fields.
         * @param cmd Command identifier
         * @param arg0 First argument
         * @param arg1 Second argument
         * @param payload Data payload
         * @return Byte array containing the message
         */

        public static AdbMessage GenerateMessage(uint cmd, uint arg0, uint arg1, byte[] payload, int offset, int count)
        {

            AdbMessage message = new AdbMessage()
            {
                command = cmd,
                arg0 = arg0,
                arg1 = arg1
            };
            if (count > 0 && payload != null)
            {
                message.payload = new byte[count];
                Array.Copy(payload, 0, message.payload, offset, count);
            }
            else
                message.payload = payload;

            return message;
        }

        public static AdbMessage GenerateMessage(uint cmd, uint arg0, uint arg1, byte[] payload)
        {
            if (payload != null)
                return GenerateMessage(cmd, arg0, arg1, payload, 0, payload.Length);
            return GenerateMessage(cmd, arg0, arg1, null, 0, 0);
        }

        /**
         * Generates a connect message with default parameters.
         * @return Byte array containing the message
         */
        public static AdbMessage GenerateConnect()
        {
            return GenerateMessage(CMD_CNXN, CONNECT_VERSION, CONNECT_MAXDATA, CONNECT_PAYLOAD);
        }

        /**
         * Generates an auth message with the specified type and payload.
         * @param type Authentication type (see AUTH_TYPE_* constants)
         * @param data The payload for the message
         * @return Byte array containing the message
         */
        public static AdbMessage GenerateAuth(uint type, byte[] data)
        {
            return GenerateMessage(CMD_AUTH, type, 0, data);
        }

        /**
         * Generates an open stream message with the specified local ID and destination.
         * @param localId A unique local ID identifying the stream
         * @param dest The destination of the stream on the target
         * @return Byte array containing the message
         */
        public static AdbMessage GenerateOpen(uint localId, String dest)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(dest);
            byte[] buf = new byte[buffer.Length + 1];
            Array.Copy(buffer, buf, buffer.Length);


            return GenerateMessage(CMD_OPEN, localId, 0, buf);
        }

        /**
         * Generates a write stream message with the specified IDs and payload. 
         * @param localId The unique local ID of the stream
         * @param remoteId The unique remote ID of the stream
         * @param data The data to provide as the write payload
         * @return Byte array containing the message
         */
        public static AdbMessage GenerateWrite(uint localId, uint remoteId, byte[] data)
        {
            return GenerateMessage(CMD_WRTE, localId, remoteId, data);
        }

        public static AdbMessage GenerateWrite(uint localId, uint remoteId, byte[] data, int offset, int count)
        {
            return GenerateMessage(CMD_WRTE, localId, remoteId, data, offset, count);
        }


        /**
         * Generates a close stream message with the specified IDs.
         * @param localId The unique local ID of the stream
         * @param remoteId The unique remote ID of the stream
         * @return Byte array containing the message
         */
        public static AdbMessage GenerateClose(uint localId, uint remoteId)
        {
            return GenerateMessage(CMD_CLSE, localId, remoteId, null);
        }

        /**
         * Generates an okay message with the specified IDs.
         * @param localId The unique local ID of the stream
         * @param remoteId The unique remote ID of the stream
         * @return Byte array containing the message
         */
        public static AdbMessage GenerateReady(uint localId, uint remoteId)
        {
            return GenerateMessage(CMD_OKAY, localId, remoteId, null);
        }

    }
}
