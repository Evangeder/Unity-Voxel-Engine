using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Text_Fadeout : MonoBehaviour
{
    private UnityEngine.UI.Text text;

    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<UnityEngine.UI.Text>();
    }

    // Update is called once per frame
    void Update()
    {
        if (text.color.a > 0f)
        {
            Color col = text.color;
            col.a -= 0.25f * Time.deltaTime;
            text.color = col;
        }
    }
}
