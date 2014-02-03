
namespace Lab.IO
{
    /// <summary>
    /// Defines single-byte control signals used by the NetServerRequestResponse and NetClientRequestResponse
    /// </summary>
    public enum NetServerControl : byte
    {
        MessageFollows = 1,
        KeepAliveNoRead = 2,
        Close = 4,
        TerminateServer = 8,

        HandshakeFromClient = 16,
        HandshakeFromServer = 17
    }

}
