using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Assets.Scripts
{
    public static class Utils
    {
        public static Guid ToGuid(this string str)
        {
            MD5 md5Hasher = MD5.Create();
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(str));

            return new Guid(data);
        }

        public static void SetLayerRecursive(GameObject gameObject, int newLayer)
        {
            if (gameObject == null)
            {
                return;
            }

            //if (gameObject.layer != LayerMask.NameToLayer("DontDraw"))
            //{
            //    gameObject.layer = newLayer;
            //}
            gameObject.layer = newLayer;

            foreach (Transform transform in gameObject.transform)
            {
                if (transform == null)
                {
                    continue;
                }

                SetLayerRecursive(transform.gameObject, newLayer);
            }
        }
    }
}