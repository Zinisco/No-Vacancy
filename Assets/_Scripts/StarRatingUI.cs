using UnityEngine;
using UnityEngine.UI;

public class StarRatingUI : MonoBehaviour
{
    [SerializeField] private Image outlineStar;
    [SerializeField] private Image goldStar;

    public void SetFilled(bool filled)
    {
        if (outlineStar != null)
            outlineStar.gameObject.SetActive(true);

        if (goldStar == null)
            return;

        goldStar.gameObject.SetActive(false);

        if (filled)
        {
            goldStar.transform.localScale = Vector3.zero;
            goldStar.gameObject.SetActive(true);
        }
    }
}