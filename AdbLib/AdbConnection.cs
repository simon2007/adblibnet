using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using utils;

namespace AdbLib
{
    public class AdbConnection
    {
        /** The underlying socket that this class uses to
 * communicate with the target device.
 */
        private TcpClient socket;

        /** The last allocated local stream ID. The ID
         * chosen for the next stream will be this value + 1.
         */
        private uint lastLocalId;

        /**
         * The input stream that this class uses to read from
         * the socket.
         */
        private BinaryStream stream;



        /**
         * The backend thread that handles responding to ADB packets.
         */
        private Thread connectionThread;

        /**
         * Specifies whether a connect has been attempted
         */
        private bool connectAttempted;

        /**
         * Specifies whether a CNXN packet has been received from the peer.
         */
        private bool connected;

        /**
         * Specifies the maximum amount data that can be sent to the remote peer.
         * This is only valid after connect() returns successfully.
         */
        private uint maxData;

        /**
         * An initialized ADB crypto object that contains a key pair.
         */
        private AdbCrypto crypto;

        /**
         * Specifies whether this connection has already sent a signed token.
         */
        private bool sentSignature;

        /** 
         * A hash map of our open streams indexed by local ID.
         **/
        private Dictionary<uint, AdbSessionBase> openStreams;

        /**
         * Internal constructor to initialize some internal state
         */
        private AdbConnection(TcpClient socket, AdbCrypto crypto)
        {
            openStreams = new Dictionary<uint, AdbSessionBase>();
            lastLocalId = 0;
            this.crypto = crypto;

            this.socket = socket;
            this.stream = new BinaryStream(socket.GetStream());




            this.connectionThread = new Thread(Run);
            this.connectionThread.IsBackground = true;
            this.connectionThread.Start();
        }

        internal void Send(AdbMessage packet)
        {
            packet.WriteTo(stream);
        }

        /**
         * Creates a AdbConnection object associated with the socket and
         * crypto object specified.
         * @param socket The socket that the connection will use for communcation.
         * @param crypto The crypto object that stores the key pair for authentication.
         * @return A new AdbConnection object.
         * @throws IOException If there is a socket error
         */
        public static AdbConnection Create(TcpClient socket, AdbCrypto crypto)
        {
            /* Disable Nagle because we're sending tiny packets */
            socket.NoDelay = true;
            AdbConnection newConn = new AdbConnection(socket, crypto);


            return newConn;
        }



        public void Run()
        {
            while (true)
            {
                try
                {
                    /* Read and parse a message off the socket's input stream */
                    AdbMessage msg = AdbMessage.ReadAdbMessage(stream);



                    switch (msg.command)
                    {
                        /* Stream-oriented commands */
                        case AdbProtocol.CMD_OKAY:
                        case AdbProtocol.CMD_WRTE:
                        case AdbProtocol.CMD_CLSE:
                            /* We must ignore all packets when not connected */
                            if (!connected)
                                continue;

                            /* Get the stream object corresponding to the packet */
                            AdbSessionBase waitingStream;
                            if (!openStreams.TryGetValue(msg.arg1, out waitingStream))
                                continue;



                            lock (waitingStream)
                            {
                                if (msg.command == AdbProtocol.CMD_OKAY)
                                {
                                    /* We're ready for writes */
                                    waitingStream.UpdateRemoteId(msg.arg0);
                                    waitingStream.ReadyForWrite();

                                    /* Unwait an open/write */
                                    Monitor.Pulse(waitingStream);
                                }
                                else if (msg.command == AdbProtocol.CMD_WRTE)
                                {
                                    /* Got some data from our partner */
                                    waitingStream.AddPayload(msg.payload);

                                    /* Tell it we're ready for more */
                                    waitingStream.SendReady();
                                }
                                else if (msg.command == AdbProtocol.CMD_CLSE)
                                {
                                    /* He doesn't like us anymore :-( */
                                    openStreams.Remove(msg.arg1);

                                    /* Notify readers and writers */
                                    waitingStream.NotifyClose();
                                }
                            }

                            break;

                        case AdbProtocol.CMD_AUTH:

                            AdbMessage packet;

                            if (msg.arg0 == AdbProtocol.AUTH_TYPE_TOKEN)
                            {
                                /* This is an authentication challenge */
                                if (sentSignature)
                                {
                                    /* We've already tried our signature, so send our public key */
                                    packet = AdbProtocol.generateAuth(AdbProtocol.AUTH_TYPE_RSA_PUBLIC,
                                            crypto.getAdbPublicKeyPayload());
                                }
                                else
                                {
                                    /* We'll sign the token */
                                    packet = AdbProtocol.generateAuth(AdbProtocol.AUTH_TYPE_SIGNATURE,
                                            crypto.signAdbTokenPayload(msg.payload));
                                    sentSignature = true;
                                }

                                /* Write the AUTH reply */
                                packet.WriteTo(stream);
                            }
                            break;

                        case AdbProtocol.CMD_CNXN:
                            lock (this)
                            {
                                /* We need to store the max data size */
                                maxData = msg.arg1;

                                /* Mark us as connected and unwait anyone waiting on the connection */
                                connected = true;
                                //conn.notifyAll();
                                Monitor.PulseAll(this);
                            }
                            break;

                        default:
                            /* Unrecognized packet, just drop it */
                            break;
                    }
                }
                catch (Exception e)
                {
                    /* The cleanup is taken care of by a combination of this thread
                     * and close() */
                    break;
                }
            }

            /* This thread takes care of cleaning up pending streams */
            lock (this)
            {
                cleanupStreams();
                //conn.notifyAll();
                Monitor.PulseAll(this);
                connectAttempted = false;
            }
        }

        internal void Flush()
        {
            stream.Flush();
        }


        /**
         * Gets the max data size that the remote client supports.
         * A connection must have been attempted before calling this routine.
         * This routine will block if a connection is in progress.
         * @return The maximum data size indicated in the connect packet.
         * @throws InterruptedException If a connection cannot be waited on.
         * @throws IOException if the connection fails
         */
        public uint getMaxData()
        {
            if (!connectAttempted)
                throw new IOException("connect() must be called first");


            lock (this)
            {
                /* Block if a connection is pending, but not yet complete */
                if (!connected)
                    Monitor.Wait(this);

                if (!connected)
                {
                    throw new IOException("Connection failed");
                }
            }

            return maxData;
        }

        /**
         * Connects to the remote device. This routine will block until the connection
         * completes.
         * @throws IOException If the socket fails while connecting
         * @throws InterruptedException If we are unable to wait for the connection to finish
         */
        public void connect()
        {
            if (connected)
                throw new IOException("Already connected");

            /* Write the CONNECT packet */
            AdbProtocol.generateConnect().WriteTo(stream);


            /* Start the connection thread to respond to the peer */
            connectAttempted = true;
            connectionThread.Start();

            /* Wait for the connection to go live */
            lock (this)
            {
                if (!connected)
                    Monitor.Wait(this);

                if (!connected)
                {
                    throw new IOException("Connection failed");
                }
            }
        }

        /**
         * Opens an AdbStream object corresponding to the specified destination.
         * This routine will block until the connection completes.
         * @param destination The destination to open on the target
         * @return AdbStream object corresponding to the specified destination
         * @throws UnsupportedEncodingException If the destination cannot be encoded to UTF-8
         * @throws IOException If the stream fails while sending the packet
         * @throws InterruptedException If we are unable to wait for the connection to finish
         */
        public AdbSessionBase open(String destination)
        {
            uint localId = ++lastLocalId;

            if (!connectAttempted)
                throw new IOException("connect() must be called first");

            /* Wait for the connect response */
            lock (this)
            {
                if (!connected)
                    Monitor.Wait(this);

                if (!connected)
                {
                    throw new IOException("Connection failed");
                }
            }

            /* Add this stream to this list of half-open streams */
            AdbSessionBase stream = new ShellSession(this, localId);
            openStreams.Add(localId, stream);

            /* Send the open */
            AdbProtocol.generateOpen(localId, destination).WriteTo(this.stream);



            /* Wait for the connection thread to receive the OKAY */
            lock (stream)
            {
                Monitor.Wait(stream);
            }

            /* Check if the open was rejected */
            if (stream.IsClosed)
                throw new IOException("Stream open actively rejected by remote peer");

            /* We're fully setup now */
            return stream;
        }

        /**
         * This function terminates all I/O on streams associated with this ADB connection
         */
        private void cleanupStreams()
        {
            /* Close all streams on this connection */
            foreach (AdbSessionBase s in openStreams.Values)
            {
                /* We handle exceptions for each close() call to avoid
                 * terminating cleanup for one failed close(). */
                try
                {
                    s.Close();
                }
                catch (IOException e) { }
            }

            /* No open streams anymore */
            openStreams.Clear();
        }

        /** This routine closes the Adb connection and underlying socket
         * @throws IOException if the socket fails to close
         */

        public void close()
        {
            /* If the connection thread hasn't spawned yet, there's nothing to do */
            if (connectionThread == null)
                return;

            /* Closing the socket will kick the connection thread */
            socket.Close();

            /* Wait for the connection thread to die */
            connectionThread.Interrupt();

            connectionThread.Join();

        }
    }
}
