using UnityEngine;

public class ColorChangerUI : MonoBehaviour
{
    /// <summary>
    /// Permet de mettre une color random sur tout les élément du player 
    /// </summary>
    public void SetRandomColor()
    {
        Color randomColor = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);

        if (TeamColorManager.instance != null)
        {
            TeamColorManager.instance.ChangePlayerColor(randomColor);
        }
    }

    /// <summary>
    /// Permet de set tout la couleur du player a blue (test) 
    /// </summary>
    public void SetColorBlue()
    {
        if (TeamColorManager.instance != null)
        {
            TeamColorManager.instance.ChangePlayerColor(Color.blue);
        }
    }
}