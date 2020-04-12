using UnityEngine;

public class UrlOpener : MonoBehaviour
{
    public string url;
    public void OpenURL()
    {
        Application.OpenURL(url);
    }
}
