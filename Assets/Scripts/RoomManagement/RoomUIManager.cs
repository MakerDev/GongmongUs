using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.RoomManagement
{
    public class RoomUIManager : MonoBehaviour
    {
        public static RoomUIManager Instance { get; private set; }

        [SerializeField]
        private Text _localPlayerName;
        [SerializeField]
        private GameObject _roomPlayerUIPrefab;
        [SerializeField]
        private GameObject _roomPlayerUIListPanel;

        private List<GameObject> _roomPlayerUIList = new List<GameObject>();

        public void RefreshList(IEnumerable<Player> players)
        {
            foreach (var playerUIObject in _roomPlayerUIList)
            {
                Destroy(playerUIObject);
            }

            _roomPlayerUIList.Clear();

            foreach (var player in players)
            {
                var roomPlayerItem = Instantiate(_roomPlayerUIPrefab, _roomPlayerUIListPanel.transform);
                _roomPlayerUIList.Add(roomPlayerItem);
                var roomPlayerUI = roomPlayerItem.GetComponent<RoomPlayerUI>();
                roomPlayerUI.SetPlayer(player);
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Update()
        {
            if (Player.LocalPlayer != null)
            {
                _localPlayerName.text = Player.LocalPlayer.PlayerName;
            }
        }
    }
}
