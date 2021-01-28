using System;
using System.Collections.Generic;
using System.Text;

namespace BattleCampusMatchServer.Models
{
    public class GuestUser : GameUser
    {
        public GuestUser()
        {
            Name = "Guest";
            StudentID = null;
            ID = Guid.NewGuid();
        }
    }
}
