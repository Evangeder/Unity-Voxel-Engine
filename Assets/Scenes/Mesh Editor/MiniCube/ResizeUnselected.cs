using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResizeUnselected : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.localScale.x > 0.05f)
        {
            Vector3 newScale = transform.localScale;
            newScale.x -= 0.5f * Time.deltaTime;
            newScale.y -= 0.5f * Time.deltaTime;
            transform.localScale = newScale;
        }
    }
}
