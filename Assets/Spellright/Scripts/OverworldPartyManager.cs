using System.Collections.Generic;
using UnityEngine;

namespace Spellright
{
    public sealed class OverworldPartyManager : MonoBehaviour
    {
        readonly List<OverworldCharacterIdentity> members = new List<OverworldCharacterIdentity>();
        OverworldInputReader input;
        OverworldMovementTuning tuning;
        OverworldCharacterMotor groupMotor;
        OverworldFormationController formations;
        OverworldTotemMagic totemMagic;
        OverworldHuddleTraversal huddleTraversal;
        OverworldElementalInteraction elementalInteraction;
        Camera viewCamera;
        Vector3 cameraSmoothVelocity;
        readonly Vector3[] transitionStarts = new Vector3[3];
        readonly Vector3[] transitionTargets = new Vector3[3];
        readonly Quaternion[] transitionRotations = new Quaternion[3];
        int leaderIndex;
        int spreadControlIndex;
        bool transitioning;
        float transitionElapsed;
        float cameraYaw;
        float cameraPitch = 13f;
        float speedMultiplier = 1f;
        bool cursorLocked = true;
        public string Message { get; private set; } = "Line formation: Brim leads.";
        public OverworldFormationController Formations => formations;
        public OverworldInputReader Input => input;
        public OverworldMovementTuning Tuning => tuning;
        public OverworldCharacterMotor GroupMotor => groupMotor;
        public Camera ViewCamera => viewCamera;
        public bool IsTransitioning => transitioning;
        public bool IsCarrying => huddleTraversal != null && huddleTraversal.IsCarrying;
        public Transform GroupAnchor => groupMotor ? groupMotor.transform : null;
        public OverworldCharacterIdentity Leader => members.Count > 0 ? members[leaderIndex] : null;
        public OverworldCharacterIdentity Controlled => members.Count > 0
            ? members[formations.Current == FormationKind.Spread ? spreadControlIndex : leaderIndex] : null;

        public void Initialize(OverworldInputReader actionReader, OverworldMovementTuning movementValues, Camera camera)
        {
            input = actionReader;
            tuning = movementValues;
            viewCamera = camera;
            SetCursorLock(true);
            formations = gameObject.AddComponent<OverworldFormationController>();
            formations.Initialize(this);
            groupMotor = CreateGroupMotor();
            CreateHero(0, "Brim", Element.Fire, new Color(1f, .22f, .1f), new Vector3(0, 0, 0));
            CreateHero(1, "Brooke", Element.Water, new Color(.08f, .48f, 1f), new Vector3(0, 0, -1.7f));
            CreateHero(2, "Blitz", Element.Electric, new Color(1f, .83f, .06f), new Vector3(0, 0, -3.4f));
            groupMotor.SnapTo(members[0].transform.position, members[0].transform.rotation);
            totemMagic = gameObject.AddComponent<OverworldTotemMagic>();
            totemMagic.Initialize(this, input, tuning);
            huddleTraversal = gameObject.AddComponent<OverworldHuddleTraversal>();
            huddleTraversal.Initialize(this, input, tuning);
            elementalInteraction = gameObject.AddComponent<OverworldElementalInteraction>();
            elementalInteraction.Initialize(this, input);
            ApplyMotorModes();
            RefreshSelection();
        }

        public OverworldCharacterIdentity Character(int index) => index >= 0 && index < members.Count ? members[index] : null;
        public string NameOf(int index) => Character(index) ? Character(index).name : "?";
        public int CharacterIndex(Element element)
        {
            for (int i = 0; i < members.Count; i++) if (members[i].Element == element) return i;
            return -1;
        }
        public void Say(string text) { Message = text; }

        public void SetLeaderFromTotemTop(int characterIndex)
        {
            if (characterIndex < 0 || characterIndex >= members.Count) return;
            leaderIndex = characterIndex;
            RefreshSelection();
        }

        void Update()
        {
            if (input == null || members.Count < 3) return;
            UpdateCameraInput();
            if (input.Pressed("CursorLock")) SetCursorLock(!cursorLocked);

            if (!transitioning)
            {
                if (input.Pressed("FormationLine")) formations.SetFormation(FormationKind.Line);
                else if (input.Pressed("FormationSpread")) formations.SetFormation(FormationKind.Spread);
                else if (input.Pressed("FormationTotem")) formations.SetFormation(FormationKind.Totem);
                else if (input.Pressed("FormationHuddle")) formations.SetFormation(FormationKind.Huddle);

                if (!transitioning && input.Pressed("TotemOrder") && formations.Current == FormationKind.Totem)
                    formations.RotateTotemOrder();
                bool canSwapCharacter = formations.Current != FormationKind.Huddle;
                if (!transitioning && canSwapCharacter && input.Pressed("Next")) CycleControlled(1);
                if (!transitioning && canSwapCharacter && input.Pressed("Previous")) CycleControlled(-1);
            }

            if (transitioning)
            {
                UpdateFormationTransition();
                return;
            }

            totemMagic.Tick();
            speedMultiplier = huddleTraversal.Tick();
            elementalInteraction.Tick(huddleTraversal.ConsumedInteract, huddleTraversal.ConsumedAttack);
            if (totemMagic.IsAimingStorm)
            {
                return;
            }

            Vector2 move = input.Vector("Move");
            bool totemFormation = formations.Current == FormationKind.Totem;
            bool sprint = !totemFormation && input.Held("Sprint");
            bool jumpPressed = !totemFormation && input.Pressed("Jump");
            bool jumpHeld = !totemFormation && input.Held("Jump");
            if (formations.Current == FormationKind.Spread)
            {
                groupMotor.SetMotorEnabled(false);
                for (int i = 0; i < members.Count; i++)
                {
                    var motor = members[i].GetComponent<OverworldCharacterMotor>();
                    motor.SetMotorEnabled(i == spreadControlIndex);
                    if (i == spreadControlIndex)
                        motor.Tick(move, viewCamera ? viewCamera.transform : null, sprint, jumpPressed, jumpHeld, false, speedMultiplier);
                }
            }
            else
            {
                groupMotor.SetMotorEnabled(true);
                for (int i = 0; i < members.Count; i++)
                {
                    bool lineFollower = formations.Current == FormationKind.Line && i != leaderIndex;
                    members[i].GetComponent<OverworldCharacterMotor>().SetMotorEnabled(lineFollower);
                }
                float formationSpeed = totemFormation ? tuning.totemMovementMultiplier : 1f;
                groupMotor.Tick(move, viewCamera ? viewCamera.transform : null, sprint, jumpPressed, jumpHeld,
                    huddleTraversal.Gliding, speedMultiplier * formationSpeed,
                    formations.Current == FormationKind.Huddle ? tuning.huddleTurnMultiplier : 1f,
                    formations.Current == FormationKind.Line);
                PlaceFormationCharacters();
            }
            huddleTraversal.LateTick();
            RefreshSelection();
        }

        void LateUpdate()
        {
            if (!viewCamera || Controlled == null) return;
            Quaternion orbit = Quaternion.Euler(cameraPitch, cameraYaw, 0);
            OverworldCharacterIdentity focusCharacter = formations.Current == FormationKind.Huddle ? Leader : Controlled;
            Vector3 focus = (focusCharacter ? focusCharacter.transform.position : Controlled.transform.position) + Vector3.up * .95f;
            float cameraDistance = formations.Current == FormationKind.Totem
                ? tuning.totemCameraDistance : tuning.cameraFollowDistance;
            Vector3 desired = focus + orbit * new Vector3(0, 1.2f, -cameraDistance);
            viewCamera.transform.position = Vector3.SmoothDamp(viewCamera.transform.position, desired,
                ref cameraSmoothVelocity, .075f, 1000f, Time.deltaTime);
            viewCamera.transform.rotation = Quaternion.LookRotation(focus - viewCamera.transform.position, Vector3.up);
        }

        void UpdateCameraInput()
        {
            Vector2 look = input.Vector("Look");
            if (input.LookFromPointer)
            {
                cameraYaw += look.x * .12f;
                cameraPitch = Mathf.Clamp(cameraPitch - look.y * .1f, -4f, 32f);
            }
            else
            {
                cameraYaw += look.x * 155f * Time.deltaTime;
                cameraPitch = Mathf.Clamp(cameraPitch - look.y * 105f * Time.deltaTime, -4f, 32f);
            }
        }

        void SetCursorLock(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void CycleControlled(int direction)
        {
            if (formations.Current == FormationKind.Totem)
                return;
            int from = formations.Current == FormationKind.Spread ? spreadControlIndex : leaderIndex;
            int to = (from + direction + members.Count) % members.Count;
            if (formations.Current == FormationKind.Spread)
            {
                spreadControlIndex = to;
                RefreshSelection();
                Say("Spread control: " + NameOf(to) + ". The others stay in place.");
                return;
            }
            leaderIndex = to;
            BeginFormationTransition(formations.Current, formations.Current);
            Say("Line leader: " + NameOf(leaderIndex) + ".");
        }

        public void BeginFormationTransition(FormationKind previous, FormationKind next)
        {
            if (members.Count < 3) return;
            if (next == FormationKind.Spread)
            {
                spreadControlIndex = leaderIndex;
                for (int i = 0; i < members.Count; i++)
                {
                    transitionStarts[i] = members[i].transform.position;
                    transitionRotations[i] = members[i].transform.rotation;
                    members[i].GetComponent<OverworldCharacterMotor>().SetMotorEnabled(false);
                }
                groupMotor.SetMotorEnabled(false);
                transitionElapsed = 0;
                transitioning = true;
                CalculateFormationTargets(next);
                Say("Spread: switch characters and position each one independently.");
                return;
            }
            if (previous == FormationKind.Spread)
            {
                leaderIndex = spreadControlIndex;
                groupMotor.SnapTo(members[leaderIndex].transform.position, members[leaderIndex].transform.rotation);
            }

            for (int i = 0; i < members.Count; i++)
            {
                transitionStarts[i] = members[i].transform.position;
                transitionRotations[i] = members[i].transform.rotation;
                members[i].GetComponent<OverworldCharacterMotor>().SetMotorEnabled(false);
            }
            groupMotor.SetMotorEnabled(false);
            transitionElapsed = 0;
            transitioning = true;
            CalculateFormationTargets(next);
            Say("Changing to " + next + " formation.");
        }

        void CalculateFormationTargets(FormationKind next)
        {
            Quaternion facing = groupMotor.transform.rotation;
            switch (next)
            {
                case FormationKind.Line:
                    int[] order = formations.LineOrder(leaderIndex);
                    Vector3 leaderPosition = groupMotor.transform.position;
                    for (int slot = 0; slot < order.Length; slot++)
                        transitionTargets[order[slot]] = leaderPosition - facing * Vector3.forward * (slot * tuning.followDistance);
                    break;
                case FormationKind.Spread:
                    int[] spreadOrder = formations.LineOrder(leaderIndex);
                    Vector3 forward = facing * Vector3.forward;
                    Vector3 right = facing * Vector3.right;
                    Vector3 lineLeader = groupMotor.transform.position;
                    transitionTargets[spreadOrder[0]] = lineLeader;
                    transitionTargets[spreadOrder[1]] = lineLeader - forward * 1.6f + right * 2.2f;
                    transitionTargets[spreadOrder[2]] = lineLeader - forward * 1.6f - right * 2.2f;
                    break;
                default:
                    for (int i = 0; i < members.Count; i++)
                        transitionTargets[i] = groupMotor.transform.position + FormationOffset(i, next, facing);
                    break;
            }
        }

        Vector3 FormationOffset(int index, FormationKind formation, Quaternion facing)
        {
            if (formation == FormationKind.Totem)
            {
                int slot = formations.SlotOf(index);
                return Vector3.up * ((2 - slot) * 1.25f);
            }
            if (formation == FormationKind.Huddle)
            {
                if (index == leaderIndex) return Vector3.zero;
                int[] order = formations.LineOrder(leaderIndex);
                bool leftFollower = index == order[1];
                Vector3 local = leftFollower ? new Vector3(-.68f, 0, -.55f) : new Vector3(.68f, 0, -.55f);
                return facing * local;
            }
            return Vector3.zero;
        }

        void UpdateFormationTransition()
        {
            transitionElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(transitionElapsed / tuning.formationTransitionSeconds);
            float eased = t * t * (3f - 2f * t);
            float hop = Mathf.Sin(t * Mathf.PI) * tuning.formationHopHeight;
            for (int i = 0; i < members.Count; i++)
            {
                members[i].transform.position = Vector3.Lerp(transitionStarts[i], transitionTargets[i], eased) + Vector3.up * hop;
                members[i].transform.rotation = Quaternion.Slerp(transitionRotations[i], groupMotor.transform.rotation, eased);
            }
            if (t < 1f) return;
            for (int i = 0; i < members.Count; i++) members[i].transform.position = transitionTargets[i];
            transitioning = false;
            ApplyMotorModes();
            RefreshSelection();
            Say(formations.Current + " formation.");
        }

        void PlaceFormationCharacters()
        {
            if (formations.Current == FormationKind.Line)
            {
                int[] order = formations.LineOrder(leaderIndex);
                members[leaderIndex].transform.SetPositionAndRotation(groupMotor.transform.position, groupMotor.transform.rotation);
                for (int slot = 1; slot < order.Length; slot++)
                {
                    int index = order[slot];
                    var previous = members[order[slot - 1]];
                    Vector3 target = previous.transform.position - previous.transform.forward * tuning.followDistance;
                    var follower = members[index];
                    var motor = follower.GetComponent<OverworldCharacterMotor>();
                    if (follower.transform.position.y < tuning.followFallRecoveryY)
                    {
                        Vector3 recoveryPosition = members[leaderIndex].transform.position -
                            members[leaderIndex].transform.forward * (slot * tuning.followDistance);
                        motor.SnapTo(recoveryPosition, members[leaderIndex].transform.rotation);
                        continue;
                    }
                    motor.Follow(target, speedMultiplier);
                }
            }
            else
            {
                for (int i = 0; i < members.Count; i++)
                {
                    Vector3 target = groupMotor.transform.position + FormationOffset(i, formations.Current, groupMotor.transform.rotation);
                    Quaternion facing = formations.Current == FormationKind.Huddle
                        ? Quaternion.RotateTowards(members[i].transform.rotation, groupMotor.transform.rotation,
                            tuning.huddleTurnSpeed * Time.deltaTime)
                        : groupMotor.transform.rotation;
                    members[i].transform.SetPositionAndRotation(target, facing);
                }
            }
        }

        void ApplyMotorModes()
        {
            bool spread = formations.Current == FormationKind.Spread;
            groupMotor.SetMotorEnabled(!spread && !transitioning);
            for (int i = 0; i < members.Count; i++)
            {
                bool enabled = !transitioning && (spread ? i == spreadControlIndex :
                    formations.Current == FormationKind.Line && i != leaderIndex);
                members[i].GetComponent<OverworldCharacterMotor>().SetMotorEnabled(enabled);
            }
        }

        OverworldCharacterMotor CreateGroupMotor()
        {
            var root = new GameObject("Party Formation Anchor");
            root.transform.SetParent(transform);
            var collider = root.AddComponent<CharacterController>();
            collider.height = 1.8f; collider.radius = .4f; collider.center = Vector3.up * .9f;
            var motor = root.AddComponent<OverworldCharacterMotor>();
            motor.Initialize(tuning);
            return motor;
        }

        void CreateHero(int index, string heroName, Element element, Color color, Vector3 position)
        {
            var hero = new GameObject(heroName);
            hero.transform.position = position;
            var cc = hero.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = .42f; cc.center = Vector3.up * .9f; cc.skinWidth = .04f; cc.stepOffset = .35f;
            var motor = hero.AddComponent<OverworldCharacterMotor>();
            motor.Initialize(tuning);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = heroName + " Placeholder";
            visual.transform.SetParent(hero.transform, false);
            visual.transform.localPosition = Vector3.up * .9f;
            Destroy(visual.GetComponent<Collider>());
            var bodyMaterial = RuntimeMaterials.Lit(color);
            visual.GetComponent<Renderer>().material = bodyMaterial;
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "Control Marker";
            marker.transform.SetParent(hero.transform, false);
            marker.transform.localPosition = Vector3.up * 2.15f;
            marker.transform.localScale = Vector3.one * .22f;
            Destroy(marker.GetComponent<Collider>());
            var markerMaterial = RuntimeMaterials.Lit(Color.white);
            marker.GetComponent<Renderer>().material = markerMaterial;
            marker.SetActive(false);
            var identity = hero.AddComponent<OverworldCharacterIdentity>();
            identity.Initialize(index, element, color, visual.GetComponent<Renderer>(), marker);
            members.Add(identity);
        }

        void RefreshSelection()
        {
            for (int i = 0; i < members.Count; i++) members[i].SetSelected(Controlled && i == Controlled.Index);
        }

        void OnDrawGizmosSelected()
        {
            if (members.Count == 0) return;
            Gizmos.color = Color.cyan;
            foreach (var member in members) if (member) Gizmos.DrawWireSphere(member.transform.position, .7f);
        }

        void OnDestroy()
        {
            if (cursorLocked) SetCursorLock(false);
        }
    }
}
