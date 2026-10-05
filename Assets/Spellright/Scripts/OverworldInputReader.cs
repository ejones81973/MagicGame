using UnityEngine;
using UnityEngine.InputSystem;

namespace Spellright
{
    /// <summary>Small named-action adapter shared by overworld systems.</summary>
    public sealed class OverworldInputReader : MonoBehaviour
    {
        InputActionAsset asset;
        InputActionMap player;

        public void Initialize(InputActionAsset inputActions)
        {
            asset = inputActions;
            player = asset != null ? asset.FindActionMap("Player", true) : null;
            if (player != null) player.Enable();
        }

        public Vector2 Vector(string actionName) => Action(actionName)?.ReadValue<Vector2>() ?? Vector2.zero;
        public bool Pressed(string actionName) => Action(actionName)?.WasPressedThisFrame() ?? false;
        public bool Held(string actionName) => Action(actionName)?.IsPressed() ?? false;
        public float Axis(string actionName) => Action(actionName)?.ReadValue<float>() ?? 0f;
        public bool LookFromPointer => Action("Look")?.activeControl?.device is Pointer;

        InputAction Action(string actionName) => player != null ? player.FindAction(actionName, false) : null;

        void OnDestroy()
        {
            if (player != null) player.Disable();
        }
    }
}
