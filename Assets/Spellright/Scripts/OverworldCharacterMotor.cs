using UnityEngine;

namespace Spellright
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class OverworldCharacterMotor : MonoBehaviour
    {
        CharacterController controller;
        OverworldMovementTuning tuning;
        Vector3 planarVelocity;
        float verticalVelocity;
        float lastGrounded = -10f;
        float lastJumpPressed = -10f;
        float dashRemaining;
        Vector3 dashVelocity;

        public bool Grounded => controller != null && controller.isGrounded;
        public CharacterController Controller => controller;

        public void Initialize(OverworldMovementTuning values)
        {
            tuning = values;
            controller = GetComponent<CharacterController>();
        }

        public void Tick(Vector2 input, Transform cameraTransform, bool sprint, bool jumpPressed,
            bool jumpHeld, bool glide, float speedMultiplier = 1f, float turnMultiplier = 1f,
            bool jumpInPlace = false)
        {
            if (controller == null || !controller.enabled || tuning == null) return;
            if (jumpPressed) lastJumpPressed = Time.time;
            UpdateGrounded();
            Vector3 forward = cameraTransform ? cameraTransform.forward : Vector3.forward;
            forward.y = 0;
            forward = forward.sqrMagnitude > .001f ? forward.normalized : Vector3.forward;
            Vector3 right = new Vector3(forward.z, 0, -forward.x);
            Vector3 direction = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
            if (jumpInPlace && jumpPressed && direction.sqrMagnitude < .001f)
                planarVelocity = Vector3.zero;
            float speed = (sprint ? tuning.sprintSpeed : tuning.walkSpeed) * speedMultiplier;

            bool dashing = dashRemaining > 0;
            if (dashing)
            {
                dashRemaining = Mathf.Max(0, dashRemaining - Time.deltaTime);
                planarVelocity = dashVelocity;
                verticalVelocity = 0f;
            }
            else
            {
                Vector3 target = direction * speed;
                float change = direction.sqrMagnitude > .001f ? tuning.acceleration : tuning.deceleration;
                planarVelocity = Vector3.MoveTowards(planarVelocity, target, change * Time.deltaTime);
            }

            if (!dashing && Time.time - lastJumpPressed <= tuning.jumpBufferTime && Time.time - lastGrounded <= tuning.coyoteTime)
            {
                verticalVelocity = tuning.jumpSpeed;
                lastJumpPressed = -10f;
                lastGrounded = -10f;
            }
            if (!dashing && !jumpHeld && verticalVelocity > tuning.jumpCutSpeed)
                verticalVelocity = tuning.jumpCutSpeed;

            if (!dashing)
            {
                float gravity = glide && verticalVelocity < 0 ? tuning.glideGravity : tuning.gravity;
                verticalVelocity -= gravity * Time.deltaTime;
            }
            controller.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
            Face(direction, turnMultiplier);
        }

        public void Follow(Vector3 target, float speedMultiplier = 1f)
        {
            if (controller == null || !controller.enabled || tuning == null) return;
            UpdateGrounded();
            Vector3 delta = target - transform.position;
            delta.y = 0;
            float distance = delta.magnitude;
            Vector3 targetVelocity = distance < .08f ? Vector3.zero :
                delta.normalized * Mathf.Min(tuning.followSpeed * speedMultiplier, distance * tuning.followCatchUp);
            if (controller.isGrounded && target.y > transform.position.y + .55f && distance > .5f)
                verticalVelocity = tuning.jumpSpeed * .78f;
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity,
                (targetVelocity.sqrMagnitude > .001f ? tuning.acceleration : tuning.deceleration) * Time.deltaTime);
            verticalVelocity -= tuning.gravity * Time.deltaTime;
            controller.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
            Face(delta.normalized);
        }

        public bool TryDash(Vector3 direction)
        {
            if (controller == null || !controller.enabled || controller.isGrounded || tuning == null || dashRemaining > 0)
                return false;
            direction.y = 0;
            if (direction.sqrMagnitude < .01f) direction = transform.forward;
            direction.Normalize();
            dashVelocity = direction * (tuning.dashDistance / tuning.dashDuration);
            dashRemaining = tuning.dashDuration;
            verticalVelocity = 0f;
            return true;
        }

        public void SetMotorEnabled(bool enabled)
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            if (controller != null) controller.enabled = enabled;
            if (!enabled) { planarVelocity = Vector3.zero; dashRemaining = 0; }
        }

        public void SnapTo(Vector3 position, Quaternion rotation)
        {
            bool wasEnabled = controller != null && controller.enabled;
            if (wasEnabled) controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            planarVelocity = Vector3.zero;
            verticalVelocity = 0;
            dashRemaining = 0;
            if (wasEnabled) controller.enabled = true;
        }

        void UpdateGrounded()
        {
            if (controller.isGrounded)
            {
                lastGrounded = Time.time;
                if (verticalVelocity < 0) verticalVelocity = -2f;
            }
        }

        void Face(Vector3 direction, float turnMultiplier = 1f)
        {
            if (direction.sqrMagnitude < .003f) return;
            Quaternion facing = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facing,
                tuning.turnSpeed * turnMultiplier * Time.deltaTime);
        }
    }
}
