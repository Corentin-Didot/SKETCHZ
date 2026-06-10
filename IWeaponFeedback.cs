using UnityEngine;

public interface IWeaponFeedback
{
    void SpawnImpact(RaycastHit hit, Vector3 shootDirection) { }
    void PlayAttackFeedback();

}

