using Mirror;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    public class WeaponManager : NetworkBehaviour
    {
        private const string WEAPON_LAYER_NAME = "Weapon";

        [SerializeField]
        private PlayerWeapon _primaryWeapon;
        [SerializeField]
        private Transform _weaponHolder;

        private PlayerWeapon _currentWeapon;
        private WeaponGraphics _currentGraphics;

        private void Start()
        {
            EquipWeapon(_primaryWeapon);
        }

        public PlayerWeapon GetCurrentWeapon()
        {
            return _currentWeapon;
        }

        public WeaponGraphics GetCurrentWeaponGraphics()
        {
            return _currentGraphics;
        }

        private void EquipWeapon(PlayerWeapon weapon)
        {
            _currentWeapon = weapon;

            GameObject weaponInstance = Instantiate(weapon.Graphics, _weaponHolder.position, _weaponHolder.rotation);
            weaponInstance.transform.SetParent(_weaponHolder);

            _currentGraphics = weaponInstance.GetComponent<WeaponGraphics>();
            if (_currentGraphics == null)
            {
                Debug.LogError("No WeaponGraphics component on the weapon ojbject : " + weaponInstance.name);
            }

            if (isLocalPlayer)
            {
                Utils.SetLayerRecursive(weaponInstance, LayerMask.NameToLayer(WEAPON_LAYER_NAME));
            }
        }
    }
}