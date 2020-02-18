using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverlayFadeout : MonoBehaviour
{
    // Start is called before the first frame update
    UnityEngine.UI.Image img;
    public bool FadeOut = true, FadeIn = false;

    void Start()
    {
        img = GetComponent<UnityEngine.UI.Image>();
    }

    // Update is called once per frame
    void Update()
    {
        if (FadeOut)
        {
            Color col = img.color;
            if (col.a <= 0f)
            {
                FadeOut = false;
                gameObject.SetActive(false);
            }
            col.a -= 1f * Time.deltaTime;
            img.color = col;
        }
        if (FadeIn)
        {
            Color col = img.color;
            if (col.a >= 1f)
            {
                FadeIn = false;
                gameObject.SetActive(false);
            }
            col.a += 1f * Time.deltaTime;
            img.color = col;
        }
    }
}
