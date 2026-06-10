using UnityEngine;

public class MineTrigger : MonoBehaviour
{
    private MineProjectile mainMine;

    void Start()
    {
        mainMine = GetComponentInParent<MineProjectile>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (mainMine != null)
        {
            mainMine.OnSensorTriggered(other);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (mainMine != null)
        {
            mainMine.OnSensorTriggered(other);
        }
    }
}