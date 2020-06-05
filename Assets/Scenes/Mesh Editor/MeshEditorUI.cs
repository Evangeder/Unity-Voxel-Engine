using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MeshEditorUI : MonoBehaviour
{
    public Camera MainCamera;
    public Camera MiniCamera;
    public Renderer EntityRenderer;
    public Material Entity_Lit, Entity_Unlit;
    public GameObject HDRP_SceneSettings;
    public GameObject Light_EffectsON, Light_EffectsOFF;

    public GameObject UI_ColorPalette;
    public GameObject UI_Tools;
    public GameObject MeshModel;
    public GameObject Leaves;
    public GameObject Cobble;

    public void UpdateCameraPerspective(Dropdown dd)
    {
        if (dd.value == 0)
        {
            MainCamera.orthographic = false;
            MiniCamera.orthographic = false;
        }
        else
        {
            MainCamera.orthographic = true;
            MiniCamera.orthographic = true;
        }
    }

    public void ToggleCameraPerspective(Toggle toggle)
    {
        if (toggle.isOn)
        {
            MainCamera.orthographic = false;
            MiniCamera.orthographic = false;
        }
        else
        {
            MainCamera.orthographic = true;
            MiniCamera.orthographic = true;
        }
    }

    public void UpdateLightOnMesh(Dropdown dd)
    {
        if (dd.value == 0)
            EntityRenderer.material = Entity_Lit;
        else
            EntityRenderer.material = Entity_Unlit;
    }

    public void ToggleLightOnMesh(Toggle toggle)
    {
        if (toggle.isOn)
            EntityRenderer.material = Entity_Lit;
        else
            EntityRenderer.material = Entity_Unlit;
    }

    public void UpdateEnvironment(Dropdown dd)
    {
        if (dd.value == 0)
        {
            Light_EffectsOFF.SetActive(false);
            Light_EffectsON.SetActive(true);
            HDRP_SceneSettings.SetActive(true);
        }
        else
        {
            Light_EffectsOFF.SetActive(true);
            Light_EffectsON.SetActive(false);
            HDRP_SceneSettings.SetActive(false);
        }
    }

    public void ToggleShaders(Toggle toggle)
    {
        if (toggle.isOn)
        {
            Light_EffectsOFF.SetActive(false);
            Light_EffectsON.SetActive(true);
            HDRP_SceneSettings.SetActive(true);
        }
        else
        {
            Light_EffectsOFF.SetActive(true);
            Light_EffectsON.SetActive(false);
            HDRP_SceneSettings.SetActive(false);
        }
    }
}
