using UnityEngine;

// A heart on the ground: gives back all the hearts of the hero (3).
// If the hearts are already full, it stays on the ground for later.
// Needs a Collider 2D with "Is Trigger" ticked.
public class HeartPickup : MonoBehaviour
{
    public int maxHearts = 3;

    // Stay (not Enter): if the hero is hurt while standing on it, it still works
    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (GameState.hearts >= maxHearts)
        {
            MessageUI.Show("Your hearts are already full!");
            return;
        }

        GameState.hearts = maxHearts;
        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null) health.HealFlash();   // the hero flashes green
        MessageUI.Show("Your hearts are full again!");
        SoundFX.PlayKey();
        Destroy(gameObject);
    }
}
