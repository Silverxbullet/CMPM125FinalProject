using UnityEngine;

public class birdclick : MonoBehaviour
{
    public BirdWatchingQuest quest;
    private bool collected = false;

    private void OnMouseDown()
    {
        if (collected) return;
        collected = true;
        quest.BirdClicked();
        gameObject.SetActive(false);
    }
}
