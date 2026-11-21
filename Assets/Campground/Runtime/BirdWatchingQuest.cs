using UnityEngine;

public class BirdWatchingQuest : MonoBehaviour
{
    private const int Needed = 5;

    private int birdsSeen = 0;
    private bool questVisible = false;
    private bool completed = false;

    private float notificationTimer = 0f;
    private const float NotificationDuration = 3f;

    private GUIStyle panel;
    private GUIStyle label;

    public void BirdClicked()
    {
        if (completed) return;
        questVisible = true;
        birdsSeen++;

        if (birdsSeen >= Needed)
        {
            birdsSeen = Needed;
            completed = true;
            notificationTimer = NotificationDuration;
            Debug.Log("Bird Watching Quest Completed!");
        }

    }

    private void Update()
    {
        if (notificationTimer > 0f)
        {
            notificationTimer -= Time.deltaTime;
        }
        
    }
    private void OnGUI()
    {
        if (panel == null)
        {
            panel = new GUIStyle(GUI.skin.box);
            label = new GUIStyle(GUI.skin.label);

            label.fontSize = 18;
            label.normal.textColor = Color.white;
        }

        if (questVisible && !completed)
        {
            GUI.Box(new Rect(12f, 12f, 240f, 65f), GUIContent.none, panel);
            GUI.Label(new Rect(24f, 18f, 220f, 25f), "BIRD WATCHING", label);
            GUI.Label(new Rect(24f, 42f, 220f, 25f), $"Birds Seen: {birdsSeen}/{Needed}", label);
        }

        if (completed && notificationTimer > 0f)
        {
            GUI.Box(new Rect(Screen.width / 2f - 150f, 80f, 300f, 55f), "Bird Watching Badge Earned!", panel);
        }
    }

}
