using ProtoBuf.Meta;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Lab.IO
{
    public class LengthPrefixMessenger
    {
        readonly Stream messageStream;
        public Stream MessageStream { get { return messageStream; } }

        public TypeModel CustomSerializer { get; set; }

        public LengthPrefixMessenger(Stream stream, TypeModel customSerializer)
        {
            if (stream == null)
                throw new ArgumentNullException();
            this.messageStream = stream;
            this.CustomSerializer = customSerializer;
        }

        public LengthPrefixMessenger(Stream stream) : this(stream, null) { }

        /// <summary>
        /// Attempts to read a message from the stream.  Blocks when data is unavailable
        /// and will throw IOException if stream ends/closes/etc.
        /// </summary>
        /// <exception cref="System.IOException">Thrown if stream ends or closes etc.</exception>
        public T ReceiveMessage<T>()
        {
            T msg;
            var status = TryReceiveMessage<T>(out msg);
            if (status != NetServerControl.MessageFollows)
                throw new IOException("Expecting message with payload but instead got " + status.ToString() + "  Check trace log for details.");
            return msg;
        }

        public NetServerControl TryReceiveMessage<T>(out T message)
        {
            return TryReceiveMessage<T>(messageStream, CustomSerializer, out message);
        }

        internal static NetServerControl TryReceiveMessage<T>(Stream stream, TypeModel customDeserializer, out T message)
        {
            if (stream == null)
                throw new ArgumentNullException();
            message = default(T);
            while (true)
            {
                int b = stream.ReadByte();
                if (b < 0)
                {
                    Trace.TraceInformation("Reached end of stream");
                    return NetServerControl.Close;
                }

                NetServerControl ca = (NetServerControl)b;
                if (ca == NetServerControl.KeepAliveNoRead)
                    continue;
                else if (ca == NetServerControl.MessageFollows)
                {
                    message = customDeserializer.NullSafe_Deserialize_Int32Prefix<T>(stream);
                    return ca;
                }
                else if (ca == NetServerControl.Close)
                {
                    Trace.TraceInformation("Other end requested connection be closed gracefully");
                    return ca;
                }
                else if (ca == NetServerControl.TerminateServer)
                {
                    Trace.TraceInformation("Other end requested server to terminate.");
                    return ca;
                }
                else
                    throw new IOException("Received invalid control byte recieved from other end");
            }

        }

        internal static void SendControlByte(Stream s, NetServerControl controlByte)
        {
            if (s == null)
                throw new ArgumentNullException();
            s.WriteByte((byte)controlByte);
            Trace.TraceInformation("Sent control byte: " + controlByte.ToString());
        }

        public void SendControlByte(NetServerControl controlByte)
        {
            SendControlByte(messageStream, controlByte);
        }

        internal static bool ServerHandshake(Stream stream)
        {
            int r = stream.ReadByte();
            bool ok = (r == (int)NetServerControl.HandshakeFromClient);
            if (ok)
                SendControlByte(stream, NetServerControl.HandshakeFromServer);
            return ok;
        }

        internal static bool ClientHandshake(Stream stream)
        {
            SendControlByte(stream, NetServerControl.HandshakeFromClient);
            int r = stream.ReadByte();
            return (r == (int)NetServerControl.HandshakeFromServer);
        }

        public void SendMessage<T>(T message)
        {
            SendMessage(messageStream, CustomSerializer, message);
        }

        internal static void SendMessage<T>(Stream stream, TypeModel customSerializer, T message)
        {
            if (stream == null)
                throw new ArgumentNullException();
            if (object.ReferenceEquals(message, null))
                throw new ArgumentNullException();

            stream.WriteByte((byte)NetServerControl.MessageFollows);
            customSerializer.NullSafe_Serialize_Int32Prefix<T>(stream, message);
        }


    }


}
