using BattleCampusMatchServer.Models;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Networking
{
    public struct ClientUserConnectMessage : NetworkMessage
    {
        public uint NetId;
        public GameUser User;
    }
}
