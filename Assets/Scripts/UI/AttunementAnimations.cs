using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttunementAnimations : MonoBehaviour
{

    [Header("Spells UI: GO + Shaders")]
    [SerializeField] Material FireAttunement_ShaderMat;
    [SerializeField] Material WaterAttunement_ShaderMat;
    [SerializeField] Material AirAttunement_ShaderMat;
    [SerializeField] Material EarthAttunement_ShaderMat;

    [SerializeField] Material FireAttunement_TextureMat;
    [SerializeField] Material WaterAttunement_TextureMat;
    [SerializeField] Material AirAttunement_TextureMat;
    [SerializeField] Material EarthAttunement_TextureMat;

    [SerializeField] RectTransform FireAttunement_UI_RectTransform;
    [SerializeField] RectTransform WaterAttunement_UI_RectTransform;
    [SerializeField] RectTransform AirAttunement_UI_RectTransform;
    [SerializeField] RectTransform EarthAttunement_UI_RectTransform;

    [SerializeField] float AnimationSpeed = 1f;

    public enum Attunements : byte
    {
        Fire = 0,
        Water = 1,
        Air = 2,
        Earth = 3
    };
    public Attunements SelectedAttunement = Attunements.Fire;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        switch (SelectedAttunement)
        {
            case Attunements.Fire:
                if (FireAttunement_UI_RectTransform.localScale.x < 1f)
                {
                    Vector3 tempScale = FireAttunement_UI_RectTransform.localScale;
                    tempScale.x += AnimationSpeed * Time.deltaTime;
                    tempScale.y += AnimationSpeed * Time.deltaTime;
                    tempScale.z += AnimationSpeed * Time.deltaTime;
                    FireAttunement_UI_RectTransform.localScale = tempScale;
                }
                if (WaterAttunement_UI_RectTransform.localScale.x > 0.8f)
                {
                    Vector3 tempScale = WaterAttunement_UI_RectTransform.localScale;
                    tempScale.x -= AnimationSpeed * Time.deltaTime;
                    tempScale.y -= AnimationSpeed * Time.deltaTime;
                    tempScale.z -= AnimationSpeed * Time.deltaTime;
                    WaterAttunement_UI_RectTransform.localScale = tempScale;
                }
                if (EarthAttunement_UI_RectTransform.localScale.x > 0.8f)
                {
                    Vector3 tempScale = EarthAttunement_UI_RectTransform.localScale;
                    tempScale.x -= AnimationSpeed * Time.deltaTime;
                    tempScale.y -= AnimationSpeed * Time.deltaTime;
                    tempScale.z -= AnimationSpeed * Time.deltaTime;
                    EarthAttunement_UI_RectTransform.localScale = tempScale;
                }
                if (AirAttunement_UI_RectTransform.localScale.x > 0.8f)
                {
                    Vector3 tempScale = AirAttunement_UI_RectTransform.localScale;
                    tempScale.x -= AnimationSpeed * Time.deltaTime;
                    tempScale.y -= AnimationSpeed * Time.deltaTime;
                    tempScale.z -= AnimationSpeed * Time.deltaTime;
                    AirAttunement_UI_RectTransform.localScale = tempScale;
                }

                if (FireAttunement_ShaderMat.GetFloat("_MainAlpha") < 1f) FireAttunement_ShaderMat.SetFloat("_MainAlpha", FireAttunement_ShaderMat.GetFloat("_MainAlpha") + AnimationSpeed * 2f * Time.deltaTime);
                if (WaterAttunement_ShaderMat.GetFloat("_MainAlpha") > 0f) WaterAttunement_ShaderMat.SetFloat("_MainAlpha", WaterAttunement_ShaderMat.GetFloat("_MainAlpha") - AnimationSpeed * 2f * Time.deltaTime);
                if (EarthAttunement_ShaderMat.GetFloat("_MainAlpha") > 0f) EarthAttunement_ShaderMat.SetFloat("_MainAlpha", EarthAttunement_ShaderMat.GetFloat("_MainAlpha") - AnimationSpeed * 2f * Time.deltaTime);
                if (AirAttunement_ShaderMat.GetFloat("_MainAlpha") > 0f) AirAttunement_ShaderMat.SetFloat("_MainAlpha", AirAttunement_ShaderMat.GetFloat("_MainAlpha") - AnimationSpeed * 2f * Time.deltaTime);

                if (FireAttunement_TextureMat.GetColor("_Color").r < 1f)
                {
                    Color tempcol = FireAttunement_TextureMat.GetColor("_Color");
                    tempcol.r += AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.g += AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.b += AnimationSpeed * 2f * Time.deltaTime;
                    FireAttunement_TextureMat.SetColor("_Color", tempcol);
                }
                if (WaterAttunement_TextureMat.GetColor("_Color").r > 0.4f)
                {
                    Color tempcol = WaterAttunement_TextureMat.GetColor("_Color");
                    tempcol.r -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.g -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.b -= AnimationSpeed * 2f * Time.deltaTime;
                    WaterAttunement_TextureMat.SetColor("_Color", tempcol);
                }
                if (EarthAttunement_TextureMat.GetColor("_Color").r > 0.4f)
                {
                    Color tempcol = EarthAttunement_TextureMat.GetColor("_Color");
                    tempcol.r -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.g -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.b -= AnimationSpeed * 2f * Time.deltaTime;
                    EarthAttunement_TextureMat.SetColor("_Color", tempcol);
                }
                if (AirAttunement_TextureMat.GetColor("_Color").r > 0.4f)
                {
                    Color tempcol = AirAttunement_TextureMat.GetColor("_Color");
                    tempcol.r -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.g -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.b -= AnimationSpeed * 2f * Time.deltaTime;
                    AirAttunement_TextureMat.SetColor("_Color", tempcol);
                }

                break;
            case Attunements.Water:
                if (FireAttunement_UI_RectTransform.localScale.x > 0.8f)
                {
                    Vector3 tempScale = FireAttunement_UI_RectTransform.localScale;
                    tempScale.x -= AnimationSpeed * Time.deltaTime;
                    tempScale.y -= AnimationSpeed * Time.deltaTime;
                    tempScale.z -= AnimationSpeed * Time.deltaTime;
                    FireAttunement_UI_RectTransform.localScale = tempScale;
                }
                if (WaterAttunement_UI_RectTransform.localScale.x < 1f)
                {
                    Vector3 tempScale = WaterAttunement_UI_RectTransform.localScale;
                    tempScale.x += AnimationSpeed * Time.deltaTime;
                    tempScale.y += AnimationSpeed * Time.deltaTime;
                    tempScale.z += AnimationSpeed * Time.deltaTime;
                    WaterAttunement_UI_RectTransform.localScale = tempScale;
                }
                if (EarthAttunement_UI_RectTransform.localScale.x > 0.8f)
                {
                    Vector3 tempScale = EarthAttunement_UI_RectTransform.localScale;
                    tempScale.x -= AnimationSpeed * Time.deltaTime;
                    tempScale.y -= AnimationSpeed * Time.deltaTime;
                    tempScale.z -= AnimationSpeed * Time.deltaTime;
                    EarthAttunement_UI_RectTransform.localScale = tempScale;
                }
                if (AirAttunement_UI_RectTransform.localScale.x > 0.8f)
                {
                    Vector3 tempScale = AirAttunement_UI_RectTransform.localScale;
                    tempScale.x -= AnimationSpeed * Time.deltaTime;
                    tempScale.y -= AnimationSpeed * Time.deltaTime;
                    tempScale.z -= AnimationSpeed * Time.deltaTime;
                    AirAttunement_UI_RectTransform.localScale = tempScale;
                }

                if (FireAttunement_TextureMat.GetColor("_Color").r > 0.4f)
                {
                    Color tempcol = FireAttunement_TextureMat.GetColor("_Color");
                    tempcol.r -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.g -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.b -= AnimationSpeed * 2f * Time.deltaTime;
                    FireAttunement_TextureMat.SetColor("_Color", tempcol);
                }
                if (WaterAttunement_TextureMat.GetColor("_Color").r < 1f)
                {
                    Color tempcol = WaterAttunement_TextureMat.GetColor("_Color");
                    tempcol.r += AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.g += AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.b += AnimationSpeed * 2f * Time.deltaTime;
                    WaterAttunement_TextureMat.SetColor("_Color", tempcol);
                }
                if (EarthAttunement_TextureMat.GetColor("_Color").r > 0.4f)
                {
                    Color tempcol = EarthAttunement_TextureMat.GetColor("_Color");
                    tempcol.r -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.g -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.b -= AnimationSpeed * 2f * Time.deltaTime;
                    EarthAttunement_TextureMat.SetColor("_Color", tempcol);
                }
                if (AirAttunement_TextureMat.GetColor("_Color").r > 0.4f)
                {
                    Color tempcol = AirAttunement_TextureMat.GetColor("_Color");
                    tempcol.r -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.g -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.b -= AnimationSpeed * 2f * Time.deltaTime;
                    AirAttunement_TextureMat.SetColor("_Color", tempcol);
                }

                if (FireAttunement_ShaderMat.GetFloat("_MainAlpha") > 0f) FireAttunement_ShaderMat.SetFloat("_MainAlpha", FireAttunement_ShaderMat.GetFloat("_MainAlpha") - AnimationSpeed * 2f * Time.deltaTime);
                if (WaterAttunement_ShaderMat.GetFloat("_MainAlpha") < 1f) WaterAttunement_ShaderMat.SetFloat("_MainAlpha", WaterAttunement_ShaderMat.GetFloat("_MainAlpha") + AnimationSpeed * 2f * Time.deltaTime);
                if (EarthAttunement_ShaderMat.GetFloat("_MainAlpha") > 0f) EarthAttunement_ShaderMat.SetFloat("_MainAlpha", EarthAttunement_ShaderMat.GetFloat("_MainAlpha") - AnimationSpeed * 2f * Time.deltaTime);
                if (AirAttunement_ShaderMat.GetFloat("_MainAlpha") > 0f) AirAttunement_ShaderMat.SetFloat("_MainAlpha", AirAttunement_ShaderMat.GetFloat("_MainAlpha") - AnimationSpeed * 2f * Time.deltaTime);
                break;
            case Attunements.Earth:
                if (FireAttunement_UI_RectTransform.localScale.x > 0.8f)
                {
                    Vector3 tempScale = FireAttunement_UI_RectTransform.localScale;
                    tempScale.x -= AnimationSpeed * Time.deltaTime;
                    tempScale.y -= AnimationSpeed * Time.deltaTime;
                    tempScale.z -= AnimationSpeed * Time.deltaTime;
                    FireAttunement_UI_RectTransform.localScale = tempScale;
                }
                if (WaterAttunement_UI_RectTransform.localScale.x > 0.8f)
                {
                    Vector3 tempScale = WaterAttunement_UI_RectTransform.localScale;
                    tempScale.x -= AnimationSpeed * Time.deltaTime;
                    tempScale.y -= AnimationSpeed * Time.deltaTime;
                    tempScale.z -= AnimationSpeed * Time.deltaTime;
                    WaterAttunement_UI_RectTransform.localScale = tempScale;
                }
                if (EarthAttunement_UI_RectTransform.localScale.x < 1f)
                {
                    Vector3 tempScale = EarthAttunement_UI_RectTransform.localScale;
                    tempScale.x += AnimationSpeed * Time.deltaTime;
                    tempScale.y += AnimationSpeed * Time.deltaTime;
                    tempScale.z += AnimationSpeed * Time.deltaTime;
                    EarthAttunement_UI_RectTransform.localScale = tempScale;
                }
                if (AirAttunement_UI_RectTransform.localScale.x > 0.8f)
                {
                    Vector3 tempScale = AirAttunement_UI_RectTransform.localScale;
                    tempScale.x -= AnimationSpeed * Time.deltaTime;
                    tempScale.y -= AnimationSpeed * Time.deltaTime;
                    tempScale.z -= AnimationSpeed * Time.deltaTime;
                    AirAttunement_UI_RectTransform.localScale = tempScale;
                }

                if (FireAttunement_TextureMat.GetColor("_Color").r > 0.4f)
                {
                    Color tempcol = FireAttunement_TextureMat.GetColor("_Color");
                    tempcol.r -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.g -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.b -= AnimationSpeed * 2f * Time.deltaTime;
                    FireAttunement_TextureMat.SetColor("_Color", tempcol);
                }
                if (WaterAttunement_TextureMat.GetColor("_Color").r > 0.4f)
                {
                    Color tempcol = WaterAttunement_TextureMat.GetColor("_Color");
                    tempcol.r -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.g -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.b -= AnimationSpeed * 2f * Time.deltaTime;
                    WaterAttunement_TextureMat.SetColor("_Color", tempcol);
                }
                if (EarthAttunement_TextureMat.GetColor("_Color").r < 1f)
                {
                    Color tempcol = EarthAttunement_TextureMat.GetColor("_Color");
                    tempcol.r += AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.g += AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.b += AnimationSpeed * 2f * Time.deltaTime;
                    EarthAttunement_TextureMat.SetColor("_Color", tempcol);
                }
                if (AirAttunement_TextureMat.GetColor("_Color").r > 0.4f)
                {
                    Color tempcol = AirAttunement_TextureMat.GetColor("_Color");
                    tempcol.r -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.g -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.b -= AnimationSpeed * 2f * Time.deltaTime;
                    AirAttunement_TextureMat.SetColor("_Color", tempcol);
                }

                if (FireAttunement_ShaderMat.GetFloat("_MainAlpha") > 0f) FireAttunement_ShaderMat.SetFloat("_MainAlpha", FireAttunement_ShaderMat.GetFloat("_MainAlpha") - AnimationSpeed * 2f * Time.deltaTime);
                if (WaterAttunement_ShaderMat.GetFloat("_MainAlpha") > 0f) WaterAttunement_ShaderMat.SetFloat("_MainAlpha", WaterAttunement_ShaderMat.GetFloat("_MainAlpha") - AnimationSpeed * 2f * Time.deltaTime);
                if (EarthAttunement_ShaderMat.GetFloat("_MainAlpha") < 1f) EarthAttunement_ShaderMat.SetFloat("_MainAlpha", EarthAttunement_ShaderMat.GetFloat("_MainAlpha") + AnimationSpeed * 2f * Time.deltaTime);
                if (AirAttunement_ShaderMat.GetFloat("_MainAlpha") > 0f) AirAttunement_ShaderMat.SetFloat("_MainAlpha", AirAttunement_ShaderMat.GetFloat("_MainAlpha") - AnimationSpeed * 2f * Time.deltaTime);
                break;
            case Attunements.Air:
                if (FireAttunement_UI_RectTransform.localScale.x > 0.8f)
                {
                    Vector3 tempScale = FireAttunement_UI_RectTransform.localScale;
                    tempScale.x -= AnimationSpeed * Time.deltaTime;
                    tempScale.y -= AnimationSpeed * Time.deltaTime;
                    tempScale.z -= AnimationSpeed * Time.deltaTime;
                    FireAttunement_UI_RectTransform.localScale = tempScale;
                }
                if (WaterAttunement_UI_RectTransform.localScale.x > 0.8f)
                {
                    Vector3 tempScale = WaterAttunement_UI_RectTransform.localScale;
                    tempScale.x -= AnimationSpeed * Time.deltaTime;
                    tempScale.y -= AnimationSpeed * Time.deltaTime;
                    tempScale.z -= AnimationSpeed * Time.deltaTime;
                    WaterAttunement_UI_RectTransform.localScale = tempScale;
                }
                if (EarthAttunement_UI_RectTransform.localScale.x > 0.8f)
                {
                    Vector3 tempScale = EarthAttunement_UI_RectTransform.localScale;
                    tempScale.x -= AnimationSpeed * Time.deltaTime;
                    tempScale.y -= AnimationSpeed * Time.deltaTime;
                    tempScale.z -= AnimationSpeed * Time.deltaTime;
                    EarthAttunement_UI_RectTransform.localScale = tempScale;
                }
                if (AirAttunement_UI_RectTransform.localScale.x < 1f)
                {
                    Vector3 tempScale = AirAttunement_UI_RectTransform.localScale;
                    tempScale.x += AnimationSpeed * Time.deltaTime;
                    tempScale.y += AnimationSpeed * Time.deltaTime;
                    tempScale.z += AnimationSpeed * Time.deltaTime;
                    AirAttunement_UI_RectTransform.localScale = tempScale;
                }

                if (FireAttunement_TextureMat.GetColor("_Color").r > 0.4f)
                {
                    Color tempcol = FireAttunement_TextureMat.GetColor("_Color");
                    tempcol.r -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.g -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.b -= AnimationSpeed * 2f * Time.deltaTime;
                    FireAttunement_TextureMat.SetColor("_Color", tempcol);
                }
                if (WaterAttunement_TextureMat.GetColor("_Color").r > 0.4f)
                {
                    Color tempcol = WaterAttunement_TextureMat.GetColor("_Color");
                    tempcol.r -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.g -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.b -= AnimationSpeed * 2f * Time.deltaTime;
                    WaterAttunement_TextureMat.SetColor("_Color", tempcol);
                }
                if (EarthAttunement_TextureMat.GetColor("_Color").r > 0.4f)
                {
                    Color tempcol = EarthAttunement_TextureMat.GetColor("_Color");
                    tempcol.r -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.g -= AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.b -= AnimationSpeed * 2f * Time.deltaTime;
                    EarthAttunement_TextureMat.SetColor("_Color", tempcol);
                }
                if (AirAttunement_TextureMat.GetColor("_Color").r < 1f)
                {
                    Color tempcol = AirAttunement_TextureMat.GetColor("_Color");
                    tempcol.r += AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.g += AnimationSpeed * 2f * Time.deltaTime;
                    tempcol.b += AnimationSpeed * 2f * Time.deltaTime;
                    AirAttunement_TextureMat.SetColor("_Color", tempcol);
                }

                if (FireAttunement_ShaderMat.GetFloat("_MainAlpha") > 0f) FireAttunement_ShaderMat.SetFloat("_MainAlpha", FireAttunement_ShaderMat.GetFloat("_MainAlpha") - AnimationSpeed * 2f * Time.deltaTime);
                if (WaterAttunement_ShaderMat.GetFloat("_MainAlpha") > 0f) WaterAttunement_ShaderMat.SetFloat("_MainAlpha", WaterAttunement_ShaderMat.GetFloat("_MainAlpha") - AnimationSpeed * 2f * Time.deltaTime);
                if (EarthAttunement_ShaderMat.GetFloat("_MainAlpha") > 0f) EarthAttunement_ShaderMat.SetFloat("_MainAlpha", EarthAttunement_ShaderMat.GetFloat("_MainAlpha") - AnimationSpeed * 2f * Time.deltaTime);
                if (AirAttunement_ShaderMat.GetFloat("_MainAlpha") < 1f) AirAttunement_ShaderMat.SetFloat("_MainAlpha", AirAttunement_ShaderMat.GetFloat("_MainAlpha") + AnimationSpeed * 2f * Time.deltaTime);
                break;
            default:
                Debug.LogError("Something went wrong with attunement!");
                break;
        }
    }
}
