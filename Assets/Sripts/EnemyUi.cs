using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyUI : MonoBehaviour
{
    [Header("Referencje")]
    public EnemyStalker enemy;
    [Tooltip("Tekst UI – najlepiej TextMeshProUGUI albo UI.Text")]
    public TextMeshProUGUI messageText;

    [Header("Ustawienia")]
    public string investigateMessage = "PRZECIWNIK CIÊ SZUKA";
    public string chaseMessage = "PRZECIWNIK CIÊ WIDZI";
    public Color investigateColor = new Color(1f, 0.7f, 0f, 1f); // pomarañczowy
    public Color chaseColor = new Color(1f, 0f, 0f, 1f);          // czerwony
    public float fadeSpeed = 5f;

    private CanvasGroup canvasGroup;

    void Start()
    {
        if (enemy == null)
            enemy = FindFirstObjectByType<EnemyStalker>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
    }

    void Update()
    {
        if (enemy == null || messageText == null) return;

        string targetText = null;
        Color targetColor = Color.white;

        switch (enemy.CurrentState)
        {
            case EnemyStalker.State.Investigate:
                targetText = investigateMessage;
                targetColor = investigateColor;
                break;
            case EnemyStalker.State.Chase:
            case EnemyStalker.State.ChaseWindup:
                targetText = chaseMessage;
                targetColor = chaseColor;
                break;
        }

        if (targetText != null)
        {
            messageText.text = targetText;
            messageText.color = targetColor;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, fadeSpeed * Time.deltaTime);
        }
        else
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, fadeSpeed * Time.deltaTime);
        }
    }
}