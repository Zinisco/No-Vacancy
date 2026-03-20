using System.Collections;
using UnityEngine;

public class CardAnimationController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HandManager handManager;
    [SerializeField] private RectTransform animationLayer;
    [SerializeField] private RectTransform drawSpawnPoint;

    [Header("Timing")]
    [SerializeField] private float cardMoveDuration = 0.18f;
    [SerializeField] private float drawSettleDuration = 0.08f;

    [Header("Draw Animation")]
    [SerializeField] private float drawStartRotationZ = -12f;
    [SerializeField] private float drawOvershootDistance = 6f;

    public IEnumerator AnimateDrawCardToHand(GuestCard card)
    {
        if (card == null || handManager == null)
            yield break;

        RectTransform handContainer = handManager.GetHandContainer() as RectTransform;
        if (handContainer == null)
            yield break;

        Vector2 startAnchoredPos = card.GetTargetHandAnchoredPos();

        if (drawSpawnPoint != null)
        {
            Vector3 localPoint3 = handContainer.InverseTransformPoint(drawSpawnPoint.position);
            startAnchoredPos = new Vector2(localPoint3.x, localPoint3.y);
        }

        yield return StartCoroutine(AnimateCardIntoHandSlot(
            card,
            startAnchoredPos,
            drawStartRotationZ,
            4f
        ));
    }

    public IEnumerator AnimateCardToHandPose(GuestCard card)
    {
        if (card == null || handManager == null)
            yield break;

        RectTransform cardRect = card.transform as RectTransform;
        RectTransform handContainer = handManager.GetHandContainer() as RectTransform;

        if (cardRect == null || handContainer == null)
            yield break;

        Vector3 localPoint3 = handContainer.InverseTransformPoint(cardRect.position);
        Vector2 startAnchoredPos = new Vector2(localPoint3.x, localPoint3.y);

        float startRotationZ = cardRect.localEulerAngles.z;
        if (startRotationZ > 180f)
            startRotationZ -= 360f;

        float overshootRotationZ = card.GetTargetHandRotationZ() * 0.35f;

        yield return StartCoroutine(AnimateCardIntoHandSlot(
            card,
            startAnchoredPos,
            startRotationZ,
            overshootRotationZ
        ));
    }

    public IEnumerator AnimateCardToTarget(GuestCard card, Transform targetParent)
    {
        if (card == null || targetParent == null)
            yield break;

        RectTransform cardRect = card.transform as RectTransform;
        RectTransform targetRect = targetParent as RectTransform;

        if (cardRect == null || targetRect == null)
        {
            card.transform.SetParent(targetParent, false);
            card.transform.localPosition = Vector3.zero;
            card.transform.localScale = Vector3.one;
            yield break;
        }

        Canvas rootCanvas = card.GetComponentInParent<Canvas>();
        Transform animParent = animationLayer != null ? animationLayer : rootCanvas.transform;

        Vector3 startWorldPos = cardRect.position;
        Vector3 endWorldPos = targetRect.position;

        card.transform.SetParent(animParent, true);
        cardRect.position = startWorldPos;

        float elapsed = 0f;

        while (elapsed < cardMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cardMoveDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            cardRect.position = Vector3.Lerp(startWorldPos, endWorldPos, t);
            yield return null;
        }

        card.transform.SetParent(targetParent, false);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.localScale = Vector3.one;
    }

    private IEnumerator AnimateCardIntoHandSlot(
        GuestCard card,
        Vector2 startAnchoredPos,
        float startRotationZ,
        float overshootRotationZ)
    {
        if (card == null || handManager == null)
            yield break;

        RectTransform handContainer = handManager.GetHandContainer() as RectTransform;
        if (handContainer == null)
            yield break;

        Canvas.ForceUpdateCanvases();

        Vector2 finalAnchoredPos = card.GetTargetHandAnchoredPos();
        float finalRotationZ = card.GetTargetHandRotationZ();

        Vector2 toTarget = finalAnchoredPos - startAnchoredPos;
        Vector2 direction = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : Vector2.right;
        Vector2 overshootAnchoredPos = finalAnchoredPos + direction * drawOvershootDistance;

        card.SetHandPoseLerpEnabled(false);
        card.SetRootPoseInstant(startAnchoredPos, startRotationZ);

        float elapsed = 0f;

        while (elapsed < cardMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cardMoveDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            Vector2 pos = Vector2.Lerp(startAnchoredPos, overshootAnchoredPos, eased);
            float rot = Mathf.Lerp(startRotationZ, overshootRotationZ, eased);

            card.SetRootPoseInstant(pos, rot);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < drawSettleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / drawSettleDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            Vector2 pos = Vector2.Lerp(overshootAnchoredPos, finalAnchoredPos, eased);
            float rot = Mathf.Lerp(overshootRotationZ, finalRotationZ, eased);

            card.SetRootPoseInstant(pos, rot);
            yield return null;
        }

        card.SetRootPoseInstant(finalAnchoredPos, finalRotationZ);
        card.SetHandPoseLerpEnabled(true);
    }
}