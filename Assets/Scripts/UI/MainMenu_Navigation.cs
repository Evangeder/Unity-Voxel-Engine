using System.Collections;
using UnityEngine;

public class MainMenu_Navigation : MonoBehaviour
{
    [SerializeField] GameObject windowEULA;
    [SerializeField] GameObject windowMain;
    [SerializeField] GameObject windowChooseWorld;
    [SerializeField] GameObject windowCreateWorld;
    [SerializeField] GameObject windowWorldSettings;
    [SerializeField] GameObject windowGameSettings;
    [SerializeField] GameObject windowJoinGame;
    [SerializeField] GameObject windowAbout;

    MultiplayerMenu mpMenu;
    OverlayFadeout overlayFadeout;
    [SerializeField] GameObject overlayGameObject;

    void Awake()
    {
        mpMenu = GetComponent<MultiplayerMenu>();
        overlayFadeout = overlayGameObject.GetComponent<OverlayFadeout>();
    }

    public void AcceptEULA()
    {
        windowEULA.SetActive(false);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void ButtonSettings_Click()
    {
        windowGameSettings.SetActive(true);
    }

    public void ButtonSettingsClose_Click()
    {
        windowGameSettings.SetActive(false);
    }

    public void ButtonHostGame_Click()
    {
        windowChooseWorld.SetActive(true);
    }

    public void ButtonHostClose_Click()
    {
        windowChooseWorld.SetActive(false);
    }

    public void HostWorld(int worldId)
    {
        StartCoroutine(Coroutine_HostWorld());
    }

    IEnumerator Coroutine_HostWorld()
    {
        overlayFadeout.FadeIn = true;
        overlayFadeout.FadeOut = false;
        overlayGameObject.SetActive(true);

        while(!overlayFadeout.FadeInDone)
            yield return null;

        mpMenu.Host();
    }
}
