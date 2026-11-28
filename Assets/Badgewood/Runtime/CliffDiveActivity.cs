using System;
using UnityEngine;

namespace Badgewood
{
    public sealed class CliffDiveActivity : MonoBehaviour
    {
        [Serializable]
        public sealed class Course
        {
            public string title;
            public float gravity;
            public float forwardSpeed;
            public float steerSpeed;
            public float ringScale;
            public Transform root;
            public Transform[] rings;
        }
        public CampFirstPerson player;
        public CampGameFlow game;
        public Transform courseSpace;
        public Course[] courses;
        public Vector3 approach = new Vector3(0f, 34.12f, -10f);
        public Vector3 launch = new Vector3(0f, 34.08f, 1.5f);
        public Vector3 returnToCamp = new Vector3(50f, 34.14f, -27f);
        public float waterHeight = 1.5f;
        public int CourseIndex { get; private set; }
        public int RingsPassed { get; private set; }
        public int Attempts { get; private set; }
        public bool IsDiving { get; private set; }
        public bool ResultVisible { get; private set; }
        public bool LastSucceeded { get; private set; }
        public bool BadgeEarned { get; private set; }
        public int StagesCleared { get; private set; }
        public string ResultMessage { get; private set; }
        private bool trimArmed;
        public Course Current => courses[CourseIndex];
        public float DiveGravity => Current.gravity;
        public float ForwardSpeed => Current.forwardSpeed;
        public float SteerSpeed => Current.steerSpeed;
        public Vector3 CourseRight => courseSpace.right;
        public Vector3 CourseForward => courseSpace.forward;
        public bool CanLaunch
        {
            get
            {
                if (IsDiving || ResultVisible || !player.Grounded) return false;
                Vector3 local = courseSpace.InverseTransformPoint(player.transform.position);
                return Mathf.Abs(local.x) < 2.1f && local.z > -4.8f && local.z < 0.8f && local.y > 33.5f && local.y < 36.5f;
            }
        }

        private void Start() { ActivateCourse(); }

        private void ActivateCourse()
        {
            RingsPassed = 0;
            foreach (Course course in courses)
            {
                course.root.gameObject.SetActive(course == Current);
                foreach (Transform ring in course.rings) ring.gameObject.SetActive(true);
            }
        }

        public void Launch()
        {
            if (game == null || !game.GameplayActive) return;
            ActivateCourse();
            IsDiving = true;
            ResultVisible = false;
            Attempts++;
            trimArmed = false;
            float yaw = courseSpace.eulerAngles.y;
            player.Teleport(courseSpace.TransformPoint(launch), yaw, 28f);
            game.Notify("Aim through the openings. A / D steers; forward motion is automatic.");
        }

        public float ForwardAdjustment(float input)
        {
            // Walking into the launch pad with W held must not accidentally accelerate a dive.
            if (!trimArmed)
            {
                if (Mathf.Abs(input) < 0.1f) trimArmed = true;
                return 0f;
            }
            return input * 1.25f;
        }

        public void CheckTravel(Vector3 from, Vector3 to, Vector3 feet)
        {
            if (!IsDiving) return;
            // Stop collision checking at the water plane: nothing can be collected after landing.
            float landingY = courseSpace.TransformPoint(new Vector3(0f, waterHeight, 0f)).y;
            bool landed = feet.y <= landingY + 0.02f;
            Vector3 end = to;
            float feetBefore = from.y - (to.y - feet.y);
            if (landed && feetBefore > landingY)
            {
                float fraction = Mathf.Clamp01((feetBefore - landingY) / Mathf.Max(0.00001f, feetBefore - feet.y));
                end = Vector3.Lerp(from, to, fraction);
            }
            // Ordered, forward plane crossings avoid side/back pickups and frame tunnelling.
            while (RingsPassed < Current.rings.Length)
            {
                Transform ring = Current.rings[RingsPassed];
                if (!DiveAperture.Crossed(from, end, ring.position, ring.forward, out Vector3 cross)) break;
                float innerRadius = 0.91f * Current.ringScale;
                if (!DiveAperture.Fits(cross, ring.position, ring.forward, innerRadius, player.Radius, 0.10f))
                {
                    Resolve(false, "Missed ring " + (RingsPassed + 1) + ". Pass through the open center.");
                    return;
                }
                ring.gameObject.SetActive(false);
                RingsPassed++;
            }
            Vector3 local = courseSpace.InverseTransformPoint(feet);
            if (landed)
            {
                bool lake = local.x >= -16.5f && local.x <= 16.5f && local.z >= 1f && local.z <= 38f;
                bool success = lake && RingsPassed == Current.rings.Length;
                Resolve(success, success ? "Every ring cleared, with a clean lake landing!" :
                    lake ? "You landed safely, but missed one or more rings." : "Land inside the lake to finish the stage.");
            }
            else if (Mathf.Abs(local.x) > 28f || local.z > 48f || local.z < -8f || local.y < -5f)
                Resolve(false, "Outside the course. Retry from the launch pad.");
        }

        private void Resolve(bool success, string message)
        {
            IsDiving = false;
            LastSucceeded = success;
            ResultMessage = message;
            ResultVisible = true;
            if (success)
            {
                StagesCleared = Mathf.Max(StagesCleared, CourseIndex + 1);
                if (CourseIndex == courses.Length - 1) BadgeEarned = true;
            }
            game.ShowDiveResult();
        }

        public void Retry()
        {
            IsDiving = ResultVisible = false;
            ActivateCourse();
            player.Teleport(courseSpace.TransformPoint(approach), courseSpace.eulerAngles.y);
            game.ReturnToPlay();
        }

        public void NextStage()
        {
            if (!LastSucceeded) return;
            if (CourseIndex < courses.Length - 1) CourseIndex++;
            else CourseIndex = 0;
            Retry();
            game.Notify("Stage " + (CourseIndex + 1) + ": " + Current.title + ". Walk onto the yellow pad and press Space.");
        }

        public void ReturnToCamp()
        {
            if (ResultVisible && LastSucceeded && CourseIndex < courses.Length - 1) CourseIndex++;
            IsDiving = ResultVisible = false;
            ActivateCourse();
            player.Teleport(returnToCamp, -90f);
            game.ReturnToPlay();
            game.Notify(BadgeEarned ? "Cliff Diver Badge earned! Continue exploring camp." : "Course progress kept. Return to the cliff whenever you're ready.");
        }
    }
}
