using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace AdbLib
{
    public abstract class AdbSessionBase
    {
        public bool IsClosed { get; internal set; }

        /** The AdbConnection object that the stream communicates over */
        protected AdbConnection adbConn;

        /** The local ID of the stream */
        private uint localId;

        /** The remote ID of the stream */
        private uint remoteId;

        /** Indicates whether a write is currently allowed */
        private Boolean writeReady;

        /** A queue of data from the target's write packets */
        private PipeStream readStream;

        /** Indicates whether the connection is closed already */
        private bool isClosed;



        /**
         * Creates a new AdbStream object on the specified AdbConnection
         * with the given local ID.
         * @param adbConn AdbConnection that this stream is running on
         * @param localId Local ID of the stream
         */
        public AdbSessionBase(AdbConnection adbConn, uint localId)
        {
            this.adbConn = adbConn;
            this.localId = localId;
            this.readStream = new PipeStream();
            this.writeReady = false;
            this.isClosed = false;
        }

        /**
         * Called by the connection thread to indicate newly received data.
         * @param payload Data inside the write message
         */
        internal void AddPayload(byte[] payload)
        {
            readStream.Write(payload);

        }

        /**
         * Called by the connection thread to send an OKAY packet, allowing the
         * other side to continue transmission.
         * @throws IOException If the connection fails while sending the packet
         */
        internal void SendReady()
        {
            /* Generate and send a READY packet */
            AdbMessage packet = AdbProtocol.generateReady(localId, remoteId);
            adbConn.Send(packet);
        }

        /**
         * Called by the connection thread to update the remote ID for this stream
         * @param remoteId New remote ID
         */
        internal void UpdateRemoteId(uint remoteId)
        {
            this.remoteId = remoteId;
        }

        /**
         * Called by the connection thread to indicate the stream is okay to send data.
         */
        internal void ReadyForWrite()
        {
            writeReady = true;

        }

        /**
         * Called by the connection thread to notify that the stream was closed by the peer.
         */
        internal void NotifyClose()
        {
            /* We don't call close() because it sends another CLOSE */
            isClosed = true;

            /* Unwait readers and writers */
            lock (this)
            {
                Monitor.PulseAll(this);
            }
            lock (readStream)
            {
                Monitor.PulseAll(readStream);
            }
        }

        /**
         * Reads a pending write payload from the other side.
         * @return Byte array containing the payload of the write
         */
        protected int Read(byte[] buffer)
        {
            return Read(buffer, 0, buffer.Length);
        }



        protected int Read(byte[] buffer,int offset,int count)
        {
            return readStream.Read(buffer, offset, count);
        }

        protected int Fill(byte[] buffer)
        {
            return Fill(buffer, 0, buffer.Length);
        }

        protected int Fill(byte[] buffer, int offset, int count)
        {
            return readStream.Fill(buffer, offset, count);
        }


        /**
         * Sends a write packet with a given String payload.
         * @param payload Payload in the form of a String
         * @throws IOException If the stream fails while sending data
         * @throws InterruptedException If we are unable to wait to send data
         */
        protected void Write(String payload)
        {
            /* ADB needs null-terminated strings */
            Write(Encoding.UTF8.GetBytes(payload), false);

            Write(new byte[] { 0 }, true);
        }

        /**
         * Sends a write packet with a given byte array payload.
         * @param payload Payload in the form of a byte array
         * @throws IOException If the stream fails while sending data
         * @throws InterruptedException If we are unable to wait to send data
         */
        protected void Write(byte[] payload)
        {
            Write(payload, true);
        }

        protected void Write(byte[] payload, int offset, int count)
        {

            Write(payload, offset, count, true);
        }

        protected void Write(byte[] payload, int offset, int count, bool flush)
        {

            lock (this)
            {
                /* Make sure we're ready for a write */
                while (!isClosed && !writeReady)
                    Monitor.Wait(this);
                Thread.MemoryBarrier();
                writeReady = false;

                if (isClosed)
                {
                    throw new IOException("Stream closed");
                }
            }

            /* Generate a WRITE packet and send it */
            AdbMessage packet = AdbProtocol.generateWrite(localId, remoteId, payload, offset, count);
            adbConn.Send(packet);

            if (flush)
                adbConn.Flush();
        }

        /**
         * Queues a write packet and optionally sends it immediately.
         * @param payload Payload in the form of a byte array
         * @param flush Specifies whether to send the packet immediately
         * @throws IOException If the stream fails while sending data
         * @throws InterruptedException If we are unable to wait to send data
         */
        protected void Write(byte[] payload, bool flush)
        {

            Write(payload, 0, payload.Length, false);
        }

        /**
         * Closes the stream. This sends a close message to the peer.
         * @throws IOException If the stream fails while sending the close message.
         */

        public void Close()
        {
            lock (this)
            {
                /* This may already be closed by the remote host */
                if (isClosed)
                    return;

                /* Notify readers/writers that we've closed */
                NotifyClose();
            }

            AdbMessage packet = AdbProtocol.generateClose(localId, remoteId);
            adbConn.Send(packet);
            adbConn.Flush();
        }

    }
}
