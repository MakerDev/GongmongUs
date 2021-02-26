using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public enum ChatType
    {
        Info,
        Player,
        KillInfo,
        None,
    }

    [Serializable]
    public class ChatMessage
    {
        public string Message;
        public string Sender;
        public TextMeshProUGUI TextObject;

        public ChatType ChatType = ChatType.Info;
    }
}