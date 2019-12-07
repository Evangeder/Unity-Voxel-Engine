using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class PlayerSettings
{
    // WORLD CHUNKS
    public static int Chunk_LoadingSpeed = 30; // 30 default
    public static int Chunk_DrawDistance = 30; // 30 default
}

public class Settings : MonoBehaviour
{
    public int test;

    public void Change_ChunkLoadingSpeed(int value)
    {
        PlayerSettings.Chunk_LoadingSpeed = value;
    }

    public void Change_ChunkLoadingSpeed(Slider slider)
    {
        PlayerSettings.Chunk_LoadingSpeed = Mathf.RoundToInt(slider.value);
    }

    public void Change_ChunkDrawDistance(int value)
    {
        PlayerSettings.Chunk_DrawDistance = value;
    }
    public void Change_ChunkDrawDistance(Slider slider)
    {
        PlayerSettings.Chunk_DrawDistance = Mathf.RoundToInt(slider.value);
    }
}