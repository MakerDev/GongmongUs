using Assets.Scripts;
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

        private void Start()
        {
            if (_camera == null)
            {
                Debug.LogError("PlayerShoot : No camera reference");
                this.enabled = false;
            }
            _weaponManager = GetComponent<WeaponManager>();
        }

        private void Update()
        {
            if (GameManager.DisableControl)
            {
                return;
            }

            if (!isLocalPlayer || GameManager.Instance.IsMenuOpen || MiniGame.IsPlaying)
            {
                return;
            }

            _currentWeapon = _weaponManager.GetCurrentWeapon();

            var isHit = Physics.Raycast(_camera.transform.position,
                                        _camera.transform.forward,
                                        out RaycastHit hit,
                                        _currentWeapon.Range,
                                        _remotePlayerLayer);
            PlayerSetup.PlayerUI.SetCrossHair(isHit);

            if (_currentWeapon.FireRate <= 0f)
            {
                if (Input.GetButtonDown("Fire1"))
                {
                    Shoot();
                }
            }
            else
            {
                if (Input.GetButtonDown("Fire1"))
                {
                    InvokeRepeating(nameof(Shoot), 0f, 1f / _currentWeapon.FireRate);
                }
                else if (Input.GetButtonUp("Fire1"))
                {
                    CancelInvoke("Shoot");
                }
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