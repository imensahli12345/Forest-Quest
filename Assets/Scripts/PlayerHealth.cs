using System.Collections;
using UnityEngine;

// Hearts of the hero. Touching a monster removes 1 heart.
// Put this script on the Player (with PlayerController).
public class PlayerHealth : MonoBehaviour
{
    public float invincibleTime = 1f;   // after a hit, the hero can't be hit again for 1 second

    private float invincibleUntil = 0f;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        // Difficulty: more time to escape after a hit in Easy, less in Hard
        if (GameState.difficulty == GameState.Easy) invincibleTime *= 1.5f;
        else if (GameState.difficulty == GameState.Hard) invincibleTime *= 0.6f;
    }

    // Called every frame while touching something
    void OnCollisionStay2D(Collision2D collision)
    {
        Monster monster = collision.collider.GetComponent<Monster>();
        if (monster != null && !monster.IsStunned)   // a stunned monster doesn't hurt
        {
            TakeDamage(1);
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;                       // already dead: ignore more hits
        if (Time.time < invincibleUntil) return;
        invincibleUntil = Time.time + invincibleTime;

        GameState.hearts = Mathf.Max(0, GameState.hearts - amount);   // never below 0

        if (GameState.hearts == 0)
        {
            // Last heart lost: short death moment, then the GAME OVER pop-up
            isDead = true;
            StopAllCoroutines();   // stop a Blink that could leave the hero hidden
            StartCoroutine(Die());
            return;
        }

        SoundFX.PlayHurt();
        CameraFollow.Shake(0.1f, 0.2f);
        StartCoroutine(Blink());
    }

    private bool isDead = false;

    // Death moment: everything freezes, the knight (and his sword) flash red and fade out,
    // then the GAME OVER pop-up appears. Unscaled time, so it works while Time.timeScale = 0.
    IEnumerator Die()
    {
        Time.timeScale = 0f;   // stops the hero, the monsters and the physics
        SoundFX.PlayPlayerDeath();   // the soldier's death grunt (VoiceBosch, credited in the main menu)

        SpriteRenderer[] parts = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer part in parts) part.enabled = true;

        float duration = 1f;     // as long as the death grunt
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            Color c = ((int)(t / 0.08f) % 2 == 0) ? Color.red : Color.white;   // flash red / white
            c.a = 1f - t / duration;                                           // fade out
            foreach (SpriteRenderer part in parts) part.color = c;
            yield return null;
        }
        foreach (SpriteRenderer part in parts) part.enabled = false;

        GameOverMenu.Show();   // existing pop-up and its sound
    }

    // Called by HeartPickup: the knight (and his sword) flash green, and the game keeps going
    public void HealFlash()
    {
        if (isDead) return;
        StartCoroutine(FlashGreen());
    }

    IEnumerator FlashGreen()
    {
        SpriteRenderer[] parts = GetComponentsInChildren<SpriteRenderer>();
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            Color c = ((int)(t / 0.08f) % 2 == 0) ? Color.green : Color.white;   // flash green / white
            foreach (SpriteRenderer part in parts) part.color = c;
            yield return null;
        }
        foreach (SpriteRenderer part in parts) part.color = Color.white;   // normal colors again
    }

    // The hero blinks while invincible
    IEnumerator Blink()
    {
        if (sr == null) yield break;
        for (int i = 0; i < 5; i++)
        {
            sr.enabled = false;
            yield return new WaitForSeconds(0.1f);
            sr.enabled = true;
            yield return new WaitForSeconds(0.1f);
        }
    }
}
