using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResizeUnselected : MonoBehaviour
{
    public bool IsWall = false;

    void Update()
    {
        if (!IsWall)
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
}
