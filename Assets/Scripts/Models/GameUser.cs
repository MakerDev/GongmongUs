using System;
using System.Collections.Generic;
using System.Text;

namespace BattleCampusMatchServer.Models
{
    /// <summary>
    /// This player represents game player.
    /// </summary>
    [Serializable]
    public class GameUser
    {
        public Guid ID;
        public string StudentID;
        public string Name;
        //Joining match ID
        public string MatchID;
        public bool IsHost = false;
        public int ConnectionID = -1;
    }
}
