using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu_AutoPlayMusic : MonoBehaviour
{
    // Start is called before the first frame update
    AudioSource _as;
    [SerializeField] AudioClip MainTheme;

    void Start()
    {
        _as = gameObject.GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!_as.isPlaying) _as.PlayOneShot(MainTheme, 0.5f);
    }
}
