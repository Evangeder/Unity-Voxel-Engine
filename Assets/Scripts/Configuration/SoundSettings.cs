using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SoundSettings
{
    public static float Master = 0.8f;
    public static float Music = 1f;
    public static float Environment = 1f;
    public static float Effects = 1f;
    public static float UI = 1f;

    public static float MusicVolume 
    {
        get 
        { 
            return Music * Master; 
        }
    }

    public static float EnvironmentVolume
    {
        get
        {
            return Environment * Master;
        }
    }
    public static float EffectsVolume
    {
        get
        {
            return Effects * Master;
        }
    }
    public static float UIVolume
    {
        get
        {
            return UI * Master;
        }
    }
}
