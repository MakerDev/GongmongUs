##############################
IMPORTANT: MirrorNG USERS ONLY

With the recent improvements you must now add the
FlexNetworkAnimatorManager script to your scene and
assign your NetworkManager reference within it.

Mirror users, do NOT perform this step.
##############################

FlexNetworkAnimator

API
=====================================   
    public void SetAnimator(Animator animator)
        Sets which animator to use. You must call this with the appropriate animator on all clients and server. This change is not automatically synchronized.
    public void SetController(RuntimeAnimatorController controller)
        Sets which controller to use. You must call this with the appropriate controller on all clients and server. This change is not automatically synchronized.
