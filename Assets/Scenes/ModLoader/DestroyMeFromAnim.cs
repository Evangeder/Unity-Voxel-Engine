using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyMeFromAnim : MonoBehaviour
{
    public bool DestroyMeFromAnimation = false;
    public GameObject thisaswell;
    public GameObject andshowthis;
    public GameObject DiscordManagerObject;
    public GameObject ModManagerObject;
    // Update is called once per frame
    void Update()
    {
        if (DestroyMeFromAnimation)
        {
            Destroy(thisaswell);
            andshowthis.SetActive(true);
            DiscordManagerObject.SetActive(true);
            ModManagerObject.SetActive(true);
            Destroy(gameObject);
        }
    }
}
