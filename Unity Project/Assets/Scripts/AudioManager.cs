using System.ComponentModel;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
     

      public static AudioManager Instance { get; private set; }

      [SerializeField] private AudioSource musicSource;
      [SerializeField] private AudioSource voiceSource;
      [SerializeField] private AudioSource sfxSource;

     private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);   // kill duplicate (e.g. if scene reloads)
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void playMusic(AudioClip clip)
    {
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void playVoice(AudioClip clip)
    {
        voiceSource.PlayOneShot(clip);
    }

    public void playSFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }
    
    public void StopAudio()
    {
        musicSource.Stop();
        voiceSource.Stop();
        sfxSource.Stop();
    }

    public float GetMasterVolume()
    {
        return musicSource.volume;
    }

}
