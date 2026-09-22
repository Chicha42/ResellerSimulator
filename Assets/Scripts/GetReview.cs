using UnityEngine;
using YG;

public class GetReview : MonoBehaviour
{
    public void TryShowReview()
    {
        if (YG2.reviewCanShow)
        {
            YG2.ReviewShow();
            Debug.Log("[YG2] ќкно оценки показано");
        }
        else
        {
            Debug.Log("[YG2] ќценка сейчас недоступна");
        }
    }
}