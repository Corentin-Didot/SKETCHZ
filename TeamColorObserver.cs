using UnityEngine;

public abstract class TeamColorObserver : MonoBehaviour
{
    protected Color colorTeam;
    /// <summary>
    /// Dès que l'observer est crée il prend tout les class qui hérite de cette observer et
    /// appel la fonction InternalUpdateColor dès qu'il y a un changement de color dans un dès code relier a la color
    /// </summary>
    protected virtual void OnEnable()
    {
        TeamColorManager.OnColorChanged += InternalUpdateColor;

        if (TeamColorManager.instance != null)
            InternalUpdateColor(TeamColorManager.instance.playerColor);
    }

    protected virtual void OnDisable()
    {
        TeamColorManager.OnColorChanged -= InternalUpdateColor;
    }

    private void InternalUpdateColor(Color newColor)
    {
        colorTeam = newColor;
        OnColorUpdated(newColor); 
    }

    /// <summary>
    /// Fonction qui permet de dire a chaque classe fille "Je change de color"
    /// </summary>
    /// <param name="newColor"></param>
    protected abstract void OnColorUpdated(Color newColor);
}