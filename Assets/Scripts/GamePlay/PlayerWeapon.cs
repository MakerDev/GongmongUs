using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    [Serializable]
    public class PlayerWeapon
    {
        public string Name = "Glock";

        public float Damage = 10f;
        public float Range = 100f;

        public float FireRate = 0f;

        public GameObject Graphics;
    }
}