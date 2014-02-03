using System;
using System.Diagnostics;
using System.IO;
using Lab.IO;
using ProtoBuf.Meta;

namespace Lab
{
    public class RpcBase : Disposable
    {
        protected readonly Stream stream;
        protected readonly TypeModel customSerializer;
        protected readonly LengthPrefixMessenger messenger;
        protected readonly ProtoBufByteConverter byteConverter;
        protected readonly string NameForTrace;

        protected RpcBase(Stream stream, TypeModel customSerializer)
        {
            if (stream == null)
                throw new ArgumentNullException();
            if (!stream.CanWrite)
                throw new ArgumentException("stream isn't writable!");
            this.stream = stream;
            this.customSerializer = customSerializer;
            this.messenger = new LengthPrefixMessenger(stream, customSerializer);
            this.byteConverter = new ProtoBufByteConverter(customSerializer);
            NameForTrace = this.GetType().FriendlyNameForGenerics();
            Trace.TraceInformation("Instantiated " + NameForTrace);
        }

        /// <summary>
        /// If true, when disposed this object will attempt to send a TerminateServer message
        /// to the remote endpoint requesting that it shutdown permanently.
        /// If false, the remote endpoint may choose to continue running and wait for future connections.
        /// </summary>
        public bool RequestRemoteTerminationOnDispose { get; set; }

        protected override void RunOnceDisposer()
        {
            try
            {
                if ((messenger != null) && (stream.CanWrite))
                {
                    if (RequestRemoteTerminationOnDispose)
                        messenger.SendControlByte(NetServerControl.TerminateServer);
                    else
                        messenger.SendControlByte(NetServerControl.Close);
                }
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }

            stream.TryDispose();

            Trace.TraceInformation("Disposed " + this.GetType().FriendlyNameForGenerics());
        }


    }

}
