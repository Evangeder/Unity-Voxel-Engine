using UnityEngine;
using UnityEngine.UI;

public class UI_SettingsCategoryInteractive : MonoBehaviour
{
    public Sprite spriteHover, spritePressed, spriteNormal;
    Image buttonSprite;
    public Text buttonText, categoryTitle;

    public GameObject SettingPanel;

    void Awake()
    {
        buttonSprite = GetComponent<Image>();
    }

    public void SetSelected()
    {
        if (categoryTitle != null && buttonText != null)
            categoryTitle.text = buttonText.text;

        SettingPanel.SetActive(true);
        buttonSprite.sprite = spriteHover;
    }

    public void SetDeselected()
    {
        SettingPanel.SetActive(false);
        buttonSprite.sprite = spriteNormal;
    }
}
