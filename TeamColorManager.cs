using UnityEngine;
using System; 

[DefaultExecutionOrder(-100)]
public class TeamColorManager : MonoBehaviour
{
    #region Singleton Logic
    public static TeamColorManager instance { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); 
    }
    #endregion

    [Header("Settings")]
    [SerializeField] private Color _playerColor = Color.blue;
    public Color playerColor => _playerColor;
    public static event Action<Color> OnColorChanged;

    public void ChangePlayerColor(Color newColor)
    {
        if (_playerColor == newColor) return;

        _playerColor = newColor;

        OnColorChanged?.Invoke(_playerColor);
    }

    public void SetColorRed() => ChangePlayerColor(Color.red);
    public void SetColorBlue() => ChangePlayerColor(Color.blue);
    public void SetColorGreen() => ChangePlayerColor(Color.green);
}