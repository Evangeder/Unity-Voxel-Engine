using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyLoadingAnimation : MonoBehaviour
{
    public bool DestroyMeFromAnimation = false;
    public GameObject Object1;
    public GameObject Object2;
    // Update is called once per frame
    void Update()
    {
        if (DestroyMeFromAnimation)
        {
            if (Object1 != null)
                Destroy(Object1);
            if (Object2 != null)
                Destroy(Object2);
            Destroy(gameObject);
        }
    }
}

