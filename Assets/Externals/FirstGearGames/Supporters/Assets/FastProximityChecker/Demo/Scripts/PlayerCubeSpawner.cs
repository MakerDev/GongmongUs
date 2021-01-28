using Mirror;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.NetworkProximities.Demos
{



    public class PlayerCubeSpawner : NetworkBehaviour
    {
        [SerializeField]
        private GameObject _playerMovingCube;

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            CmdSpawnMovingCube();
        }

        [Command]
        private void CmdSpawnMovingCube()
        {
            GameObject obj = Instantiate(_playerMovingCube, new Vector3(0f, 1f, 0f), Quaternion.identity);
            NetworkServer.Spawn(obj, base.connectionToClient);
        }
    }


}