using System;
//using System.Net.WebSockets;
using WebSocketSharp;

namespace EEUniverse.Library
{
    /// <summary>
    /// Provides data for the disconnect event of a client.
    /// </summary>
    public class CloseEventArgs : EventArgs
    {
        /// <summary>
        /// Gets a value indicating wether the disconnect was clean or not.
        /// </summary>
        public bool WasClean { get; internal set; }

        /// <summary>
        /// The error of the disconnect event.
        /// </summary>
        public CloseStatusCode WebSocketError { get; internal set; }
        //public WebSocketError WebSocketError { get; internal set; }

        /// <summary>
        /// The reason of the close event.
        /// </summary>
        public string Reason { get; internal set; }

        public override string ToString()
        {
            return $"{nameof(WasClean)}: {WasClean}" + Environment.NewLine +
                   $"{nameof(Reason)}: {Reason}" + Environment.NewLine +
                   $"{nameof(WebSocketError)}: {(ushort)WebSocketError}: {WebSocketError.ToString()}";
        }
    }
}
