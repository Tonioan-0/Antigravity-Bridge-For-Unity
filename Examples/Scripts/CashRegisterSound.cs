using UnityEngine;

/// <summary>
/// Plays a sound when the player enters the cash register trigger zone.
/// Requires AudioSource and a trigger collider.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class CashRegisterSound : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("Audio clip to play (leave empty to use AudioSource clip)")]
    public AudioClip beepSound;
    
    [Tooltip("Volume of the beep sound")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Trigger Settings")]
    [Tooltip("Tag of the player object")]
    public string playerTag = "Player";
    
    [Tooltip("Cooldown between beeps (seconds)")]
    public float cooldown = 2f;

    private AudioSource audioSource;
    private float lastPlayTime = -999f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        
        if (beepSound != null)
        {
            audioSource.clip = beepSound;
        }

        // Ensure we have a trigger collider
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"CashRegisterSound on {gameObject.name}: Collider should be a trigger!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if it's the player
        if (other.CompareTag(playerTag) || other.GetComponent<PlayerController>() != null)
        {
            PlayBeep();
        }
    }

    public void PlayBeep()
    {
        // Check cooldown
        if (Time.time - lastPlayTime < cooldown) return;

        if (audioSource.clip != null)
        {
            audioSource.Play();
            lastPlayTime = Time.time;
        }
        else
        {
            Debug.LogWarning($"CashRegisterSound on {gameObject.name}: No audio clip assigned!");
        }
    }
}
