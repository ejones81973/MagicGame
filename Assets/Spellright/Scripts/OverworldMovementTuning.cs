using UnityEngine;

namespace Spellright
{
    [CreateAssetMenu(menuName = "Spellright/Overworld Test Tuning")]
    public sealed class OverworldMovementTuning : ScriptableObject
    {
        [Header("Movement")]
        [Min(0)] public float walkSpeed = 6.5f;
        [Min(0)] public float sprintSpeed = 9.5f;
        [Min(0)] public float acceleration = 48f;
        [Min(0)] public float deceleration = 65f;
        [Min(0)] public float turnSpeed = 1440f;
        [Min(0)] public float jumpSpeed = 10.2f;
        [Min(0)] public float gravity = 28f;
        [Min(0)] public float jumpCutSpeed = 4.1f;
        [Min(0)] public float coyoteTime = .13f;
        [Min(0)] public float jumpBufferTime = .14f;
        [Header("Party movement")]
        [Min(0)] public float followDistance = 1.7f;
        [Min(0)] public float followSpeed = 7f;
        [Min(0)] public float followCatchUp = 2.7f;
        public float followFallRecoveryY = -14f;
        [Min(0)] public float formationTransitionSeconds = .42f;
        [Min(0)] public float formationHopHeight = 1.05f;
        [Range(0, 1)] public float totemMovementMultiplier = .5f;
        [Range(.1f, 1)] public float huddleTurnMultiplier = .4f;
        [Min(0)] public float huddleTurnSpeed = 540f;
        [Min(0)] public float carrySpeedMultiplier = .55f;
        [Min(0)] public float cameraFollowDistance = 6.2f;
        [Min(0)] public float totemCameraDistance = 9f;
        [Header("Huddle traversal")]
        [Min(0)] public float dashDistance = 4.5f;
        [Min(.01f)] public float dashDuration = .12f;
        [Min(0)] public float glideGravity = 6f;
        [Header("Synchronized overworld magic")]
        [Min(0)] public float steamRange = 9f;
        [Min(0)] public float plasmaRange = 14f;
        [Min(0)] public float synchronizedProjectileSpeed = 24f;
        [Min(0)] public float stormAimSpeed = 8f;
    }
}
