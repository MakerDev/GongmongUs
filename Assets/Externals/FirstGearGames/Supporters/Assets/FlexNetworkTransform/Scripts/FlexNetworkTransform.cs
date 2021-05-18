using FirstGearGames.Utilities.Networks;
using FirstGearGames.Utilities.Objects;
#if MIRROR
using Mirror;
#elif MIRAGE
using Mirage;
#endif
using System;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms
{

    [DisallowMultipleComponent]
    public class FlexNetworkTransform : FlexNetworkTransformBase
    {
        #region Public.
        /// <summary>
        /// Transform to synchronize.
        /// </summary>
        public override Transform TargetTransform => base.transform;
        #endregion

        protected override void OnEnable()
        {
            base.OnEnable();
        }
        protected override void OnDisable()
        {
            base.OnDisable();
        }

        //TODO delete this if checks pass.
        //public override bool OnSerialize(NetworkWriter writer, bool initialState)
        //{
        //    if (initialState)
        //    {
        //        /* If root then no need to send transform data as that's already
        //        * handled in the spawn message. */
        //        if (transform.root == null)
        //            return base.OnSerialize(writer, initialState);

        //        writer.WriteVector3(TargetTransform.GetPosition(base.UseLocalSpace));
        //        writer.WriteUInt32(Quaternions.CompressQuaternion(TargetTransform.GetRotation(base.UseLocalSpace)));
        //        writer.WriteVector3(TargetTransform.GetScale());
        //    }
        //    return base.OnSerialize(writer, initialState);
        //}

        //public override void OnDeserialize(NetworkReader reader, bool initialState)
        //{
        //    if (initialState)
        //    {
        //        /* If root then no need to read transform data as that's already
        //        * handled in the spawn message. */
        //        if (transform.root == null)
        //        {
        //            base.OnDeserialize(reader, initialState);
        //            return;
        //        }

        //        TargetTransform.SetPosition(base.UseLocalSpace, reader.ReadVector3());
        //        TargetTransform.SetRotation(base.UseLocalSpace, Quaternions.DecompressQuaternion(reader.ReadUInt32()));
        //        TargetTransform.SetScale(reader.ReadVector3());
        //    }
        //    base.OnDeserialize(reader, initialState);
        //}

        /// <summary>
        /// Sets which object this transform is on.
        /// </summary>
        /// <param name="attachedIdentity">NetworkIdentity of the object this transform is on.</param>
        /// <param name="componentIndex">ComponentIndex of the NetworkBehaviour for the attachedIdentity to use. Used if you wish to attach to child NetworkBehaviours.</param>
        public void SetAttached(NetworkIdentity attachedIdentity, sbyte componentIndex)
        {
            if (componentIndex > sbyte.MaxValue)
            {
                Debug.LogError("ComponentIndex must be less than " + sbyte.MaxValue.ToString() + ".");
                return;
            }

            base.SetAttachedInternal(attachedIdentity, componentIndex);
        }

        /// <summary>
        /// Sets which object this transform is on. 
        /// </summary>
        /// <param name="attachedIdentity">NetworkIdentity of the object this transform is on.</param>
        public void SetAttached(NetworkIdentity attachedIdentity)
        {
            base.SetAttachedInternal(attachedIdentity, -1);
        }

        /// <summary>
        /// Sets which platform this transform is on.
        /// </summary>
        /// <param name="attachedIdentity"></param>
        [Obsolete("SetPlatform is being replaced with SetAttached to support child objects. Please use SetAttached.")]
        public void SetPlatform(NetworkIdentity platformIdentity)
        {
            base.SetAttachedInternal(platformIdentity, -1);
        }

    }
}

