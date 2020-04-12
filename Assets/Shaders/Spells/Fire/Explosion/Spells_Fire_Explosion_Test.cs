using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spells_Fire_Explosion_Test : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] GameObject parentObject;
    [SerializeField] [Range(0f, 10f)] float scale = 1f;
    [SerializeField] bool destroyFromAnim = false;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 newscale = new Vector3(scale, scale, scale);
        transform.localScale = newscale;

        if (destroyFromAnim) 
        {
            Destroy(parentObject);
            Destroy(gameObject); 
        }
    }
}
