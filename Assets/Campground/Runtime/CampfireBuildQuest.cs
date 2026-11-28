using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Campground
{
    [DisallowMultipleComponent]
    public class CampfireBuildQuest : MonoBehaviour
    {
        private const int Needed = 5;
        private const float Reach = 5f;
        private const float AimDegrees = 12f;
        private const float PickupRadius = 25f;

        private enum Kind { Rock, Stick, Campfire }

        private struct Target
        {
            public Transform transform;
            public Kind kind;
        }

        [Tooltip("Campfire to build. The rocks and sticks around it are found automatically.")]
        [SerializeField] private Transform campfire;

        private readonly List<Target> targets = new List<Target>();
        private Camera view;
        private bool interactHeld;
        private bool built;
        private int rocks;
        private int sticks;
        public bool Completed => built;
        public int Rocks => rocks;
        public int Sticks => sticks;
        public string CurrentHint => Hint();
        public bool Near(Vector3 point) => campfire != null && Vector3.Distance(point, campfire.position) < 20f;
        private GUIStyle panel;
        private GUIStyle label;

        private void Start()
        {
            view = Camera.main;

            if (campfire == null)
            {
                Debug.LogWarning("CampfireBuildQuest: no campfire assigned.", this);
                enabled = false;
                return;
            }

            targets.Add(new Target { transform = campfire, kind = Kind.Campfire });
            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            {
                if (t == campfire || t.IsChildOf(campfire)) continue;
                if (Vector3.Distance(t.position, campfire.position) > PickupRadius) continue;

                if (t.name.StartsWith("Campfire Rock")) targets.Add(new Target { transform = t, kind = Kind.Rock });
                else if (t.name.StartsWith("Campfire Branch")) targets.Add(new Target { transform = t, kind = Kind.Stick });
            }
        }

        private void Update()
        {
            bool held = Keyboard.current != null && Keyboard.current.fKey.isPressed
                        || Mouse.current != null && Mouse.current.leftButton.isPressed;

            if (!Badgewood.CampGameFlow.CanInteract) { interactHeld = held; return; }

            if (held && !interactHeld) Use();
            interactHeld = held;
        }

        private void Use()
        {
            Target target = FindTarget();

            if (target.kind == Kind.Campfire)
            {
                if (rocks >= Needed && sticks >= Needed) Build();
                return;
            }

            if (target.transform == null) return;

            target.transform.gameObject.SetActive(false);
            if (target.kind == Kind.Rock) rocks++;
            else sticks++;
        }

        private Target FindTarget()
        {
            if (view == null) view = Camera.main;
            if (view == null) return default;

            Vector3 origin = view.transform.position;
            Vector3 forward = view.transform.forward;
            Target best = default;
            float bestAngle = AimDegrees;

            foreach (Target target in targets)
            {
                if (target.transform == null || !target.transform.gameObject.activeInHierarchy) continue;
                if (target.kind == Kind.Campfire && built) continue;

                Vector3 delta = target.transform.position - origin;
                if (delta.sqrMagnitude > Reach * Reach) continue;

                float angle = Vector3.Angle(forward, delta);
                if (angle >= bestAngle) continue;

                bestAngle = angle;
                best = target;
            }

            return best;
        }

        private void Build()
        {
            built = true;

            foreach (Renderer renderer in campfire.GetComponentsInChildren<Renderer>())
            {
                Material opaque = new Material(renderer.sharedMaterial) { name = renderer.sharedMaterial.name + " (built)" };
                Color color = opaque.color;
                color.a = 1f;
                opaque.color = color;
                opaque.SetFloat("_Mode", 0f);
                opaque.SetInt("_SrcBlend", (int)BlendMode.One);
                opaque.SetInt("_DstBlend", (int)BlendMode.Zero);
                opaque.SetInt("_ZWrite", 1);
                opaque.DisableKeyword("_ALPHABLEND_ON");
                opaque.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                opaque.renderQueue = -1;

                renderer.sharedMaterial = opaque;
                renderer.shadowCastingMode = ShadowCastingMode.On;
            }

            Debug.Log("CampfireBuildQuest: campfire built.", this);
        }

        private void OnGUI()
        {
            if (Badgewood.CampGameFlow.Instance != null) return;
            if (panel == null)
            {
                panel = new GUIStyle(GUI.skin.box);
                label = new GUIStyle(GUI.skin.label) { fontSize = 14 };
                label.normal.textColor = Color.white;
            }

            float width = Mathf.Min(420f, Screen.width - 24f);
            if (width < 180f) return;

            GUI.Box(new Rect(12f, Screen.height - 92f, width, 80f), GUIContent.none, panel);
            GUI.Label(new Rect(24f, Screen.height - 86f, width - 24f, 22f),
                "ROCKS " + rocks + " / " + Needed + "     STICKS " + sticks + " / " + Needed, label);
            GUI.Label(new Rect(24f, Screen.height - 62f, width - 24f, 40f), Hint(), label);
        }

        private string Hint()
        {
            if (built) return "Campfire built.";

            Target target = FindTarget();
            if (target.kind == Kind.Campfire)
                return rocks >= Needed && sticks >= Needed
                    ? "Press F or click to build the campfire."
                    : "Need " + (Needed - rocks) + " more rocks and " + (Needed - sticks) + " more sticks.";

            if (target.transform != null)
                return "Press F or click to pick up " + target.transform.name + ".";

            return "Look at a rock or stick and press F or click.";
        }
    }
}
