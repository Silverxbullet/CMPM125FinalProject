using System.Collections;
using UnityEngine;

namespace CliffDiversBadge
{
    public sealed class DiveChallenge : MonoBehaviour
    {
        public enum Stage { Exploring, Diving, Resetting, BadgeEarned }
        public ScoutDiver player;
        public DiveCamera followCamera;
        public DiveCollectible[] collectibles;
        public Vector3 spawnPoint = new Vector3(0, 34.08f, -10);
        public float waterHeight = 1.5f;
        public Vector2 lakeX = new Vector2(-17, 17);
        public Vector2 lakeZ = new Vector2(1, 38);
        public float retryDelay = 1.8f;
        public Stage State { get; private set; }
        public bool CanMove => State == Stage.Exploring || State == Stage.Diving;
        public int Collected { get; private set; }
        public int Attempts { get; private set; }
        private string result;
        private GUIStyle heading, body, panel, small;

        private void Start() { ResetAttempt(); }
        private void Update()
        {
            if (ScoutInput.RetryPressed)
            {
                StopAllCoroutines();
                ResetAttempt();
            }
        }

        public void BeginDive()
        {
            if (State == Stage.Exploring) State = Stage.Diving;
        }

        public void ResetAttempt()
        {
            Collected = 0;
            Attempts++;
            State = Stage.Exploring;
            foreach (var item in collectibles) if (item != null) item.Restore();
            player.ResetTo(spawnPoint);
            if (followCamera != null) followCamera.Snap();
        }

        public void CheckTravel(Vector3 from, Vector3 to, Vector3 bodyCenter)
        {
            if (State != Stage.Diving) return;
            bool landed = to.y <= waterHeight;
            Vector3 end = to;
            if (landed && from.y > waterHeight)
                end = Vector3.Lerp(from, to, (from.y - waterHeight) / (from.y - to.y));

            // Collect only along the path above the water, then resolve landing once.
            foreach (var item in collectibles)
            {
                if (item != null && !item.Collected && item.Intersects(from + bodyCenter, end + bodyCenter))
                {
                    item.Collect();
                    Collected++;
                }
            }
            if (landed)
            {
                bool inLake = end.x >= lakeX.x && end.x <= lakeX.y && end.z >= lakeZ.x && end.z <= lakeZ.y;
                if (inLake && Collected == collectibles.Length && collectibles.Length > 0)
                {
                    State = Stage.BadgeEarned;
                    result = "Cliff-diver's Badge acquired!";
                }
                else
                {
                    State = Stage.Resetting;
                    result = inLake ? "Missed " + (collectibles.Length - Collected) + " collectible(s). Try again!"
                        : "You missed the lake. Try again!";
                    StartCoroutine(Retry());
                }
            }
            else if (to.y < -10 || Mathf.Abs(to.x) > 55 || to.z > 65 || to.z < -40)
            {
                State = Stage.Resetting;
                result = "Outside the course. Try again!";
                StartCoroutine(Retry());
            }
        }

        private IEnumerator Retry()
        {
            yield return new WaitForSeconds(retryDelay);
            ResetAttempt();
        }

        private void InitStyles()
        {
            heading = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
            heading.normal.textColor = new Color(1f, 0.85f, 0.35f);
            body = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true };
            small = new GUIStyle(body) { fontSize = 16 };
            panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(22, 22, 18, 18) };
        }

        private void OnGUI()
        {
            if (heading == null) InitStyles();
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));
            GUI.Box(new Rect(24, 24, 530, 155), GUIContent.none, panel);
            GUI.Label(new Rect(44, 38, 490, 40), "CLIFF-DIVER'S BADGE", heading);
            GUI.Label(new Rect(44, 82, 490, 32), "Collectibles: " + Collected + " / " + collectibles.Length +
                "     Attempt: " + Attempts, body);
            GUI.Label(new Rect(44, 120, 490, 48), "Collect every gold ring, then land in the lake.", small);

            string hint = State == Stage.Exploring
                ? (player.AtLaunchPad ? "SPACE: dive!  |  Aim for the gold rings below."
                    : "WASD / arrows: walk to the yellow launch pad.")
                : "A / D: steer sideways    W / S: adjust forward speed";
            GUI.Box(new Rect(200, 638, 880, 56), GUIContent.none, panel);
            GUI.Label(new Rect(220, 652, 840, 32), hint + "    R: retry", small);
            if (State == Stage.Resetting || State == Stage.BadgeEarned)
            {
                GUI.Box(new Rect(280, 270, 720, 168), GUIContent.none, panel);
                GUI.Label(new Rect(306, 290, 670, 76), result, heading);
                GUI.Label(new Rect(306, 370, 670, 40), State == Stage.BadgeEarned
                    ? "All rings collected! Press R to play again."
                    : "Returning to the cliff...", body);
            }
            GUI.matrix = previous;
        }
    }
}
