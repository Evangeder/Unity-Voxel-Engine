using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderToText : MonoBehaviour
{
    Text text;
    public string UserDefinedText;

    void Awake()
    {
        text = this.GetComponent<Text>();
    }

    public void AppendSlider(Slider slider)
    {
        string val = Mathf.RoundToInt(slider.value).ToString();
        if (slider.value == 101 && this.name == "MapLoadingSpeed") val = "Instant";
        if (slider.value == 1 && this.name == "MapLoadingSpeed") val = "Slowest";

        text.text = $"{UserDefinedText}: {val}";
    }
}
