using Assets.Scripts;
using Assets.Scripts.GamePlay.PlayerActions;
using Mirror;
using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    [RequireComponent(typeof(WeaponManager))]
    public class PlayerShoot : NetworkBehaviour
    {
        private const string PLAYER_TAG = "Player";

        [SerializeField]
        private Camera _camera;
        [SerializeField]
        private LayerMask _layerMask;
        [SerializeField]
        private LayerMask _remotePlayerLayer;

        private WeaponManager _weaponManager;
        private PlayerWeapon _currentWeapon;
        private PlayerController _playerController;

        [SerializeField]
        private Transform _raycastTransform;
        private IPlayerAction _mainFireAction;

        private Player _player;

        private void Start()
        {
            if (_camera == null)
            {
                Debug.LogError("PlayerShoot : No camera reference");
                this.enabled = false;
            }
            _player = GetComponent<Player>();
            _weaponManager = GetComponent<WeaponManager>();
            _playerController = GetComponent<PlayerController>();
        }

        public void NotifyStateChanged(PlayerState state)
        {
            if (state == PlayerState.Professor)
            {
                //ChatHub.Instance.PrintMessage("I'm professor", Player.LocalPlayer.PlayerName, ChatType.Player);
                _mainFireAction = new CatchAction(_raycastTransform, _remotePlayerLayer, _playerController);
            }
            else if (state == PlayerState.Student)
            {
                //ChatHub.Instance.PrintMessage("I'm Student", Player.LocalPlayer.PlayerName, ChatType.Player);
                _mainFireAction = new StartMiniGameAction();
            }
            else
            {
                _mainFireAction = new StunByAssignmentAction();
                GameManager.Instance.PrintMessage($"{_player.PlayerName} is now assistant.", "SYSTEM", ChatType.Info);
            }
        }

        private void Update()
        {
            if (GameManager.DisableControl || _mainFireAction == null)
            {
                return;
            }

            if (!isLocalPlayer || GameManager.Instance.IsMenuOpen || MiniGame.IsPlaying)
            {
                return;
            }

            var canExecute = _mainFireAction.CanExecute();
            PlayerSetup.PlayerUI.SetCrossHair(canExecute);

            if (Input.GetButtonDown("Fire1"))
            {
                _mainFireAction.TryExecute();
            }
        }

        [Command]
        private void CmdOnHit(Vector3 position, Vector3 normal)
        {
            RpcInstantiateHitEffect(position, normal);
        }

        [ClientRpc]
        private void RpcInstantiateHitEffect(Vector3 position, Vector3 normal)
        {
            GameObject hitEffect = Instantiate(_weaponManager.GetCurrentWeaponGraphics().HitEffectPrefab, position, Quaternion.LookRotation(normal));
            Destroy(hitEffect, 1.5f);
        }

        [Command]
        private void CmdOnShoot()
        {
            RpcPlayShootEffect();
        }

        [ClientRpc]
        private void RpcPlayShootEffect()
        {
            _weaponManager.GetCurrentWeaponGraphics().MuzzleFlash.Play();
        }

        [Client]
        private void Shoot()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            CmdOnShoot();

            if (Physics.Raycast(_camera.transform.position, _camera.transform.forward, out RaycastHit hit, _currentWeapon.Range, _layerMask))
            {
                //We hit something
                //Debug.Log($"Hit : {hit.collider.name}");

                if (hit.collider.CompareTag(PLAYER_TAG))
                {
                    CmdPlayerGotShot(hit.collider.name, _currentWeapon.Damage, Player.LocalPlayer.PlayerName);
                }

                CmdOnHit(hit.point, hit.normal);
            }
        }

        [Command]
        void CmdPlayerGotShot(string playerId, float damage, string shooter)
        {
            //Debug.Log(playerId + " has been shot by damage " + damage + $" by {shooter}");
            Player player = GameManager.Instance.GetPlayer(playerId);

            //player.RpcTakeDamage(player.CurrentHealth, shooter);

            //Does this increase too much server load..? Looks fine.
            //Server side respawn
            //if (player.CurrentHealth <= 0)
            //{
            //    player.CurrentHealth = 100;
            //}
        }
    }
}