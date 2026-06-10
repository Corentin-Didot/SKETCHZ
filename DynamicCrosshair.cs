using UnityEngine;
using UnityEngine.UI;
using Online.Shared.Game;

public class DynamicCrosshair : MonoBehaviour
{
    [Header("UI Parts")]
    [SerializeField] private RectTransform topPart;
    [SerializeField] private RectTransform bottomPart;
    [SerializeField] private RectTransform leftPart;
    [SerializeField] private RectTransform rightPart;
    [SerializeField] private CanvasGroup crosshairGroup; // Pour la transparence

    [Header("Movement & Spread Settings")]
    [SerializeField] private float crosshairSmoothness = 10f;
    [SerializeField] private float spreadMultiplier = 5f;
    [SerializeField] private float movementImpact = 25f;
    [SerializeField] private float minSpread = 150f;

    [Header("Sprint Transparency")]
    [Range(0f, 1f)]
    [SerializeField] private float sprintAlpha = 0f;      // Alpha cible quand on court
    [SerializeField] private float fadeSpeed = 10f;       // Vitesse de la transition
    [SerializeField] private float speedThreshold = 8f;   // Vitesse à partir de laquelle on considère qu'il court

    private Player_Movement movement;
    private Ranged_Weapon_Online weapon;
    private float currentVisualSpread;
    private float targetAlpha = 1f;

    void Start()
    {
        movement = FindAnyObjectByType<Player_Movement>();
        weapon = FindAnyObjectByType<Ranged_Weapon_Online>();

        if (weapon == null) Debug.LogError("<color=red>[Crosshair]</color> ARME NON TROUVÉE !");
        else Debug.Log("<color=green>[Crosshair]</color> Arme liée avec succès.");

        if (crosshairGroup == null) crosshairGroup = GetComponent<CanvasGroup>();
    }

    void Update()
    {
        if (movement == null || weapon == null) return;

        float weaponAngle = weapon.GetCurrentTotalAngle();
        float playerSpeed = movement.CurrentHorizontalSpeed;
        float targetSpread = minSpread + (weaponAngle * spreadMultiplier) + (playerSpeed * movementImpact);
        

        if (targetSpread > currentVisualSpread)
        {
            currentVisualSpread = Mathf.Lerp(currentVisualSpread, targetSpread, Time.deltaTime * crosshairSmoothness * 2f);
        }
        else
        {
            currentVisualSpread = Mathf.Lerp(currentVisualSpread, targetSpread, Time.deltaTime * crosshairSmoothness);
        }

        ApplySpread(currentVisualSpread);

        if (playerSpeed > speedThreshold)
        {
            targetAlpha = sprintAlpha; 
        }
        else
        {
            targetAlpha = 1f; 
        }

        crosshairGroup.alpha = Mathf.Lerp(crosshairGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
    }

    private void ApplySpread(float spread)
    {
        topPart.anchoredPosition = new Vector2(0, spread);
        bottomPart.anchoredPosition = new Vector2(0, -spread);
        leftPart.anchoredPosition = new Vector2(-spread, 0);
        rightPart.anchoredPosition = new Vector2(spread, 0);
    }
}