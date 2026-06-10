using Online.Client;
using UnityEngine;
using UnityEngine.UI;

public class DynamicHitmarker : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float punchScale = 1.2f;
    [SerializeField] private float lerpSpeed = 15f;
    [SerializeField] private float maxRotation = 10f;
    [SerializeField] private float kickAmount = 5f;

    private RectTransform rectTransform;
    private Image image;
    private Vector3 initialScale;
    private bool isInitialized = false;

    // Cette fonction garantit que les composants sont récupérés avant usage
    private void EnsureInitialized()
    {
        if (isInitialized) return;

        rectTransform = GetComponent<RectTransform>();

        // Image
        image = GetComponent<Image>();
        if (image == null) image = GetComponentInChildren<Image>();
        if (image == null)
        {
            Debug.LogError($"[DynamicHitmarker] Aucun composant Image trouvé sur {gameObject.name} !");
            return;
        }
        initialScale = rectTransform.localScale;
        image.canvasRenderer.SetAlpha(0); 

        // Online
        if(NetworkClient.GetInstance())
        {
            NetworkClient.GetInstance().onPlayerHit += ShowHitmarker;
        }

        isInitialized = true;
    }

    void Awake()
    {
        EnsureInitialized();
    }

    public void PlayHit(Color color)
    {
        EnsureInitialized();
        if (image == null) return;

        image.color = color;
        image.canvasRenderer.SetAlpha(1);

        rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(-maxRotation, maxRotation));
        rectTransform.localScale = initialScale * punchScale;

        Vector2 randomOffset = Random.insideUnitCircle * kickAmount;
        rectTransform.anchoredPosition = randomOffset;

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySound("HitTick", gameObject, true);

        image.CrossFadeAlpha(0, 0.2f, false);
    }

    private void ShowHitmarker(float _damages)
    {
        PlayHit(Color.white);
    }

    void Update()
    {
        if (!isInitialized) return;

        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, initialScale, Time.deltaTime * lerpSpeed);
        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, Vector2.zero, Time.deltaTime * lerpSpeed);
    }

    private void OnDestroy()
    {
        // Online
        if (NetworkClient.GetInstance())
        {
            NetworkClient.GetInstance().onPlayerHit -= ShowHitmarker;
        }
    }
}