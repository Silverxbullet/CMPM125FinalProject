using UnityEngine;

public class birdclick : MonoBehaviour
{
    public BirdWatchingQuest quest;
    private bool collected = false;

    private void OnMouseDown()
    {
        // The first-person controller performs a center-screen raycast in the new game.
        if (Badgewood.CampGameFlow.Instance != null) return;
        Observe();
    }

    public void Observe()
    {
        if (!Badgewood.CampGameFlow.CanInteract) return;
        if (collected) return;
        if (Badgewood.CampGameFlow.Instance != null) quest = Badgewood.CampGameFlow.Instance.birds;
        if (quest == null) quest = FindFirstObjectByType<BirdWatchingQuest>();
        if (quest == null) return;
        collected = true;
        quest.BirdClicked();
        gameObject.SetActive(false);
    }
}
