##############################
IMPORTANT: Mirage USERS ONLY

With the recent improvements you must now add the
FlexNetworkTransformManager script to your scene and
assign your NetworkManager reference within it.
Additionally, add MIRAGE to your defines under player settings.

Mirror users, do NOT perform this step.
##############################

FlexNetworkTransform

NOTES
=====================================   
    Using 'Compress Small' may cause inaccurate replication when working with fine decimals.
    For example, if you need accuracy beyond the hundredth decimal place you should not use 'Compress Small'.

API
=====================================   
    public void SetAttached(NetworkIdentity target)
        Sets your transform as attached to an object.
            While using client or server authoritative you may set the attached target from the client
            to enable perfect synchronization on moving platforms. To detach from the object
            you will call SetAttached(null). See Platforms demo for an example.

    public void SetAttached(NetworkIdentity target, sbyte targetIndex)
        Sets your transform as attached to a child object.
            To attach to a child object on a target add the `FlexAttachTargets` script to the
            object you wish to attach onto. Add all objects which can be attached to under `Targets`.
            Use `ReturnTargetIndex(GameObject go)` on the `FlexAttachTargets` component for target to
            get the targetIndex. You may also use `GameObject ReturnTarget(sbyte index)` on the receiving end,
            such as from a RPC. See PickupObjects demo for an example.

    OnClientDataReceived
        Dispatched when server receives data from a client while using client authoritative.
            To reject data on the server you only have to call IgnoreData() on
            the ReceivedClientData instance. For example, obj.IgnoreData();

            You may also modify the data instead.
            For example: obj.Data.Position = Vector3.zero;

            Be aware that data may arrive as LocalSpace or WorldSpace
            depending on your FNT settings. When modifying data be sure to
            convert when necessary.

            You could even implement your own way of snapping the client
            authoritative player back after rejecting the data. In my example
            I send the current coordinates of the transform back to the client
            in which they teleport to these values.

            Be aware that the space may change to world space if the client is on a platform.
            You can access the platform information within FlexNetworkTransform.Platform. If the
            Identity field is null, the player is not on a platform.

            See ClientDataRejector in demos for an example.
	