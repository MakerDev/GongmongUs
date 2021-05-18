using Mirror;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
#if MIRAGE
using NetworkConnection = Mirror.INetworkConnection;
#endif


namespace FirstGearGames.Utilities.Networks
{

    public static class Platforms
    {
        #region Network specific Support.
        /// <summary>
        /// Returns the NetworkId for a NetworkIdentity.
        /// </summary>
        /// <param name="ni"></param>
        /// <returns></returns>
        public static uint ReturnNetworkId(this NetworkIdentity ni)
        {
#if MIRROR
            return ni.netId;
#elif MIRAGE
            return ni.NetId;
#endif
        }
        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nm"></param>
        /// <param name="msg"></param>
        /// <param name="channel"></param>
#if MIRROR
        public static void ClientSend<T>(NetworkManager nm, T msg, int channel) where T : struct, NetworkMessage
#elif MIRAGE
        public static void ClientSend<T>(NetworkManager nm, T msg, int channel)
#endif
        {
#if MIRROR
            NetworkClient.Send(msg, channel);
#elif MIRAGE
            nm.Client.Send(msg, channel);
#endif
        }

        /// <summary>
        /// Sends a message to all clients.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nm"></param>
        /// <param name="msg"></param>
        /// <param name="channel"></param>
#if MIRROR
        public static void ServerSendToAll<T>(NetworkManager nm, T msg, int channel) where T : struct, NetworkMessage
#elif MIRAGE
        public static void ServerSendToAll<T>(NetworkManager nm, T msg, int channel)
#endif
        {
#if MIRROR
            NetworkServer.SendToAll(msg, channel, true);
#elif MIRAGE
            nm.Server.SendToAll(msg, channel, true);
#endif
        }

        /// <summary>
        /// Returns true if object has an owner.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReturnHasOwner(this NetworkBehaviour nb)
        {
#if MIRROR
            return (nb.connectionToClient != null);
#elif MIRAGE
            return (nb.ConnectionToClient != null);
#endif
        }

        /// <summary>
        /// Returns the networkId for the networkIdentity.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReturnNetId(this NetworkBehaviour nb)
        {
#if MIRROR
            return nb.netIdentity.netId;
#elif MIRAGE
            return nb.NetIdentity.NetId;
#endif
        }
        /// <summary>
        /// Returns current owner of this object.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkConnection ReturnOwner(this NetworkBehaviour nb)
        {
#if MIRROR
            return nb.connectionToClient;
#elif MIRAGE
            return nb.ConnectionToClient;
#endif
        }

        /// <summary>
        /// Returns if current client has authority.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReturnHasAuthority(this NetworkBehaviour nb)
        {
#if MIRROR
            return nb.hasAuthority;
#elif MIRAGE
            return nb.HasAuthority;
#endif
        }
        /// <summary>
        /// Returns if is server.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReturnIsServer(this NetworkBehaviour nb)
        {
#if MIRROR
            return nb.isServer;
#elif MIRAGE
            return nb.IsServer;
#endif
        }
        /// <summary>
        /// Returns if is server only.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReturnIsServerOnly(this NetworkBehaviour nb)
        {
#if MIRROR
            return nb.isServerOnly;
#elif MIRAGE
            return nb.IsServerOnly;
#endif
        }
        /// <summary>
        /// Returns if is client.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReturnIsClient(this NetworkBehaviour nb)
        {
#if MIRROR
            return nb.isClient;
#elif MIRAGE
            return nb.IsClient;
#endif
        }

        /// <summary>
        /// Returns if client is active.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReturnServerActive(NetworkManager nm)
        {
#if MIRROR
            return NetworkServer.active;
#elif MIRAGE
            if (nm != null && nm.Server != null)
                return (nm.Server.Active);
            else
                return false;
#endif
        }
        /// <summary>
        /// Returns if client is active.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReturnClientActive(NetworkManager nm)
        {
#if MIRROR
            return NetworkClient.active;
#elif MIRAGE
            if (nm != null && nm.Client != null)
                return (nm.Client.Connection != null);
            else
                return false;
#endif
        }

        /// <summary>
        /// Returns if a connection is ready.
        /// </summary>
        /// <param name="nc"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsReady(this NetworkConnection nc)
        {
#if MIRROR
            return nc.isReady;
#elif MIRAGE
            return nc.IsReady;
#endif
        }

        /// <summary>
        /// Returns currently spawned NetworkIdentities.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<uint, NetworkIdentity> ReturnSpawned(NetworkManager nm)
        {
#if MIRROR
            return NetworkIdentity.spawned;
#elif MIRAGE
            if (ReturnServerActive(nm))
                return nm.Server.Spawned;
            else if (ReturnClientActive(nm))
                return nm.Client.Spawned;
            else
                return new Dictionary<uint, NetworkIdentity>();
#endif
        }

        /// <summary>
        /// Sets MTU values.
        /// </summary>
        /// <param name="reliable"></param>
        /// <param name="unreliable"></param>
        /// <returns></returns>
        public static void SetMTU(ref int reliable, ref int unreliable, int maxPacketSize)
        {
            //Only Mirror can check transport MTU.
#if MIRROR
            if (Transport.activeTransport != null)
            {
                reliable = Mathf.Min(maxPacketSize, Transport.activeTransport.GetMaxPacketSize(0));
                unreliable = Mathf.Min(maxPacketSize, Transport.activeTransport.GetMaxPacketSize(1));
            }
#endif
            //If packet sizes are not calculated then use max.
            if (reliable == -1)
                reliable = maxPacketSize;
            if (unreliable == -1)
                unreliable = maxPacketSize;
        }  
#endregion

    }

}