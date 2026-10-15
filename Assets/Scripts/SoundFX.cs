using UnityEngine;

// Plays the short sound effects of the game.
// Put it on one object in the scene and drag the 6 sounds into its fields.
// Other scripts call for example: SoundFX.PlayCoin();
// If there is no SoundFX in the scene (like the menu scenes), nothing happens.
public class SoundFX : MonoBehaviour
{
    public AudioClip coin;
    public AudioClip key;
    public AudioClip monsterKilled;
    public AudioClip hurt;
    public AudioClip gameOver;
    public AudioClip playerDeath;

    private static SoundFX instance;   // the SoundFX of the current scene
    private AudioSource source;        // the "speaker" that plays the sounds

    void Awake()
    {
        instance = this;
        source = gameObject.AddComponent<AudioSource>();   // created by code, no setup needed
    }

    public static void PlayCoin() { Play(instance != null ? instance.coin : null); }
    public static void PlayKey() { Play(instance != null ? instance.key : null); }
    public static void PlayMonsterKilled() { Play(instance != null ? instance.monsterKilled : null); }
    public static void PlayHurt() { Play(instance != null ? instance.hurt : null); }
    // Sounds are not stopped by Time.timeScale = 0, so this also plays when the game is frozen
    public static void PlayGameOver() { Play(instance != null ? instance.gameOver : null); }
    public static void PlayPlayerDeath() { Play(instance != null ? instance.playerDeath : null); }

    static void Play(AudioClip clip)
    {
        // PlayOneShot lets several sounds overlap (for example two coins very fast)
        if (instance != null && clip != null) instance.source.PlayOneShot(clip);
    }
}
