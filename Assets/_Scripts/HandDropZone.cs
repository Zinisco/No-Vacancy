using UnityEngine;

public class HandDropZone : MonoBehaviour
{
    [SerializeField] private HandManager handManager;
    [SerializeField] private RectTransform dropArea;

    private void Awake()
    {
        if (dropArea == null)
            dropArea = transform as RectTransform;
    }

    public bool ContainsScreenPoint(
        Vector2 screenPosition,
        Camera eventCamera)
    {
        if (dropArea == null)
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(
            dropArea,
            screenPosition,
            eventCamera
        );
    }

    public int GetDropIndex(
        Vector2 screenPosition,
        Camera eventCamera,
        GuestCard draggedCard)
    {
        if (handManager == null)
            return -1;

        return handManager.GetDropIndex(
            screenPosition,
            eventCamera,
            draggedCard
        );
    }

    public int PreviewDrop(
        Vector2 screenPosition,
        Camera eventCamera,
        GuestCard draggedCard)
    {
        if (handManager == null)
            return -1;

        return handManager.PreviewDrop(
            screenPosition,
            eventCamera,
            draggedCard
        );
    }

    public void ClearPreview()
    {
        if (handManager != null)
            handManager.ClearDropPreview();
    }
}