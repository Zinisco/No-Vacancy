using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform target;
    [SerializeField] private Button button;
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float lerpSpeed = 12f;

    private Vector3 targetScale = Vector3.one;

    private void Awake()
    {
        if (target == null)
            target = transform as RectTransform;

        if (button == null)
            button = GetComponent<Button>();
    }

    private void Update()
    {
        if (target == null)
            return;

        Vector3 desiredScale = Vector3.one;

        if (button != null && button.interactable)
            desiredScale = targetScale;

        target.localScale = Vector3.Lerp(
            target.localScale,
            desiredScale,
            Time.deltaTime * lerpSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable)
            return;

        targetScale = Vector3.one * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = Vector3.one;
    }

    private void OnDisable()
    {
        targetScale = Vector3.one;

        if (target != null)
            target.localScale = Vector3.one;
    }
}