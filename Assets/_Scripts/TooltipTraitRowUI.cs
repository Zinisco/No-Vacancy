using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TooltipTraitRowUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;

    private void Awake()
    {
        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>(true);

        if (labelText == null)
            labelText = GetComponentInChildren<TMP_Text>(true);
    }

    public void SetData(Sprite icon, string label)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (labelText != null)
            labelText.text = label;
    }
}