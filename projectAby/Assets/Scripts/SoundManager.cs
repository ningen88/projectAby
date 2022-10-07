using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    [Range(0.0f, 1.0f)]
    public float volume;
    [Range(0.1f, 3.0f)]
    public float pitch;
    public bool loop;
    [Range(0,256)]
    public int priority;

    [HideInInspector]
    public AudioSource source;
}

public class SoundManager : MonoBehaviour
{
    [SerializeField] Sound[] sounds;
    private Dictionary<string, Sound> soundsList = new Dictionary<string, Sound>();

    private void Awake()
    {
        foreach(Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            s.source.priority = s.priority;

            soundsList.Add(s.name, s);
        }
    }

    public void PlaySound(string name)
    {
        Sound sound;
        if (soundsList.TryGetValue(name, out sound))
        {
            sound.source.Play();
        }
        else
        {
            Debug.Log("Cannot find audio file");
            return;
        }
    }

    public void StopSound(string name)
    {
        Sound sound;
        if(soundsList.TryGetValue(name, out sound))
        {
            sound.source.Stop();
        }
        else
        {
            Debug.Log("Cannot find audio file");
            return;
        }
    }
}
