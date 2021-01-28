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
        public Guid ID { get; set; }
        public string StudentID { get; set; }
        public string Name { get; set; }
        //Joining match ID
        public string MatchID { get; set; }
        public bool IsHost { get; set; } = false;
        public int ConnectionID { get; set; } = -1;
    }
}
