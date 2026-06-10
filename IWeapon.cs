using UnityEngine;


public interface IWeapon
{
    void Shoot();
    bool CanShoot();
    void HandleAmmoRecharge() { }
    bool ApplyDamage(RaycastHit _hit, bool _doDamages, out int _hitID, out float _damages);

}