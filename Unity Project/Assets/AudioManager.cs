using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
     

      public static AudioManager Instance { get; private set; }
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
}
