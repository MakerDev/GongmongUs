﻿using System.Collections.Generic;

namespace Assets.Scripts
{
    public class ServerMatchInfo
    {
        public bool Started { get; set; } = false;
        public List<Player> Players { get; set; } = new List<Player>();
    }
}