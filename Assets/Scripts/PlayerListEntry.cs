using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameField;
    [SerializeField] private Image isReadyIcon;
    [SerializeField] private TextMeshProUGUI scoreField;

    private void Awake()
    {
        isReadyIcon.gameObject.SetActive(false);
        scoreField.gameObject.SetActive(false);
    }

    public void SetIsReady(string playerName, bool isReady)
    {
        nameField.SetText(playerName);
        bool wasAlreadyActive = isReadyIcon.gameObject.activeSelf;
        isReadyIcon.gameObject.SetActive(isReady);
        if(!wasAlreadyActive && isReady)
        {
            Tween.PunchScale(isReadyIcon.transform, Vector3.one * 0.1f, 1f);
        }

        scoreField.gameObject.SetActive(false);
    }

    public void SetScore(int score)
    {
        isReadyIcon.gameObject.SetActive(false);
        scoreField.gameObject.SetActive(true);
        scoreField.SetText(score.ToString());
    }
}
