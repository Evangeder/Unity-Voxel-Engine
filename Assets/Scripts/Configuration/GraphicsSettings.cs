using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

public class GraphicsSettings : MonoBehaviour
{
    [Header("Values")]
    [SerializeField] int2 screenResolution;
    [SerializeField] bool fullscreen;
    [SerializeField] int refreshRate;
    [SerializeField] int frameLimiter;
    [SerializeField] float interfaceScale;
    [SerializeField] bool scaleInterface;
    [SerializeField] float fullScreenGamma;
    [SerializeField] int settingsPreset;
    [SerializeField] int textureQuality;
    [SerializeField] int antiAliasing;
    [SerializeField] int environmentQuality;
    [SerializeField] int distanceLOD;
    [SerializeField] int renderSampling;
    [SerializeField] int shadowsQuality;
    [SerializeField] int shadersQuality;
    [SerializeField] int postProcessing;
    [SerializeField] bool ambientOcclusion;
    [SerializeField] bool textureFiltering;
    [SerializeField] bool depthBlur;
    [SerializeField] bool verticalSynch;
    [SerializeField] int renderDistance;

    [Header("UI Elements")]
    [SerializeField] Dropdown dropdownResolution;

    public static GraphicsSettings Instance { get; private set; }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Instance = this;
        GetScreenResolutions(dropdownResolution);
        Debug.Log($"Current texture quality: {QualitySettings.masterTextureLimit}");
    }

    public void LoadSettings()
    {
        // Reference Json class here
    }

    public void SaveSettings()
    {
        // Reference Json class here
    }

    public void GetScreenResolutions(Dropdown dropdownResolution)
    {
        foreach (var res in Screen.resolutions)
            dropdownResolution.options.Add(new Dropdown.OptionData($"{res.width}x{res.height}:{res.refreshRate}Hz"));
    }

    public void SetResolution(Dropdown dropdownResolution)
    {
        Resolution[] resolutions = Screen.resolutions;
        int index = dropdownResolution.value;

        screenResolution = new int2(resolutions[index].width, resolutions[index].height);
        refreshRate = resolutions[index].refreshRate;
    }

    public void SetFullscreen(Toggle toggleFullscreen)
    {
        fullscreen = toggleFullscreen.isOn;
    }

    public void SetFrameLimiter(Dropdown dropdownFrameLimiter)
    {
        switch (dropdownFrameLimiter.value)
        {
            case 0: frameLimiter = 120; break;
            case 1: frameLimiter = 60;  break;
            case 2: frameLimiter = 30;  break;
            case 3: frameLimiter = 300; break;
        }
        Application.targetFrameRate = frameLimiter;
    }

    public void SetInterfaceScaling(Toggle toggleInterfaceScale)
    {
        scaleInterface = toggleInterfaceScale.isOn;

        if (scaleInterface)
            ChangeCanvasScale(interfaceScale, CanvasScaler.ScaleMode.ScaleWithScreenSize);
        else
            ChangeCanvasScale(interfaceScale, CanvasScaler.ScaleMode.ConstantPixelSize);
    }

    public void SetInterfaceScale(Dropdown dropdownInterfaceScale)
    {
        switch (dropdownInterfaceScale.value)
        {
            case 0: interfaceScale = 0.6f;  break;
            case 1: interfaceScale = 0.8f;  break;
            case 2: interfaceScale = 1f;    break;
            case 3: interfaceScale = 1.5f;  break;
            case 4: interfaceScale = 2f;    break;
        }

        if (scaleInterface)
            ChangeCanvasScale(interfaceScale, CanvasScaler.ScaleMode.ScaleWithScreenSize);
        else
            ChangeCanvasScale(interfaceScale, CanvasScaler.ScaleMode.ConstantPixelSize);
    }

    void ChangeCanvasScale(float scale, CanvasScaler.ScaleMode scaleMode)
    {
        foreach (var canvas in CanvasList.canvas)
        {
            if (scaleInterface)
                canvas.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            else
                canvas.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvas.scaleFactor = scale;
        }
    }

    public void SetFullscreenGamma(Slider sliderGamma)
    {
        fullScreenGamma = sliderGamma.value;
        if (fullscreen)
            RenderSettings.ambientLight = new Color(fullScreenGamma, fullScreenGamma, fullScreenGamma, 1.0f);
        else
            RenderSettings.ambientLight = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    }

    public void SetTextureQuality(Dropdown dropdownTextureQuality)
    {
        textureQuality = dropdownTextureQuality.value;
        QualitySettings.masterTextureLimit = textureQuality;
    }

    public void ApplySettings()
    {
        if (screenResolution.x > 0 && screenResolution.y > 0)
            Screen.SetResolution(screenResolution.x, screenResolution.y, fullscreen);

        if (interfaceScale > 0f)
        {
            if (scaleInterface)
                ChangeCanvasScale(interfaceScale, CanvasScaler.ScaleMode.ScaleWithScreenSize);
            else
                ChangeCanvasScale(interfaceScale, CanvasScaler.ScaleMode.ConstantPixelSize);
        }

        if (fullScreenGamma > 0f)
        {
            if (fullscreen)
                RenderSettings.ambientLight = new Color(fullScreenGamma, fullScreenGamma, fullScreenGamma, 1.0f);
            else
                RenderSettings.ambientLight = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        }

        if (frameLimiter > 0)
            Application.targetFrameRate = frameLimiter;

        QualitySettings.masterTextureLimit = textureQuality;
        SaveSettings();
    }
}
