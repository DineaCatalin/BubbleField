using UnityEngine.Audio;
using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
	public AudioMixerGroup mixerGroup;

	public Sound[] sounds;

    // Singleton
    private static AudioManager sharedInstace;
    public static AudioManager SharedInstance { get { return sharedInstace; } }

    void Awake()
    {
        if (sharedInstace != null && sharedInstace != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            sharedInstace = this;
        }

        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.loop = s.loop;

            s.source.outputAudioMixerGroup = mixerGroup;
        }
    }

	public void Play(string soundName)
	{
		Sound sound = Array.Find(sounds, item => item.name == soundName);
		if (sound == null)
		{
			Debug.LogWarning("Sound: " + name + " not found!");
			return;
		}

		sound.source.volume = sound.volume * (1f + UnityEngine.Random.Range(-sound.volumeVariance / 2f, sound.volumeVariance / 2f));
		sound.source.pitch = sound.pitch * (1f + UnityEngine.Random.Range(-sound.pitchVariance / 2f, sound.pitchVariance / 2f));

		sound.source.Play();
	}

}
