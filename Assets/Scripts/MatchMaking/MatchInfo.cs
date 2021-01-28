using Mirror;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    [Serializable]
    public class MatchInfo : NetworkBehaviour
    {
        [NonSerialized]
        public const int MAX_PLAYERS = 8;

        public string MatchID;
        public string Name;

        [NonSerialized]
        private Guid _matchGuid = Guid.Empty;
        public Guid MatchGuid
        {
            get
            {
                if (_matchGuid == Guid.Empty)
                {
                    _matchGuid = MatchID.ToGuid();
                }
                return _matchGuid;
            }
        }
    }
}