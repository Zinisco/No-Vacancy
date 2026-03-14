using UnityEngine;
using UnityEngine.UI;

public class TraitIconUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    private void Awake()
    {
        if (iconImage == null)
            iconImage = GetComponent<Image>();

        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>(true);
    }

    public void SetSprite(Sprite sprite)
    {
        if (iconImage == null)
        {
            Debug.LogWarning($"TraitIconUI on {name} could not find an Image component.");
            return;
        }

        iconImage.sprite = sprite;
        iconImage.enabled = sprite != null;
    }
}