using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MusicPlayer
{
    /// <summary>
    /// Music player for current scene
    /// </summary>
    public static AudioSource MusicAudioSource;

    public static void ChangeMusicVolume()
    {
        if (MusicAudioSource != null)
        {
            MusicAudioSource.volume = SoundSettings.MusicVolume;
        }
    }
}
