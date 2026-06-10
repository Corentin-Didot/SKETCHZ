using Online.Shared.Game;
using Online.Shared.Utils;
using System;
using UnityEngine;

public class Hitbox : MonoBehaviour, IDamageable
{
    // IDamageable Interface
    public int DamageableID { get; set; }
    public Validator<LobbyObject> lobby { get; set; }

    [Header("Multiplicateur de dégâts")]
    public float damageMultiplier = 1f;
    [HideInInspector] public IDamageable healthScript;
    public event Action<IDamageable> OnDestroyDamageable;
    [SerializeField] private bool isHead = false;

    public void OnHit(float _baseDamage, Vector3 _dir, int _attackerID, bool _broadcast, out float _finalDamages)
    {
        _finalDamages = _baseDamage * this.damageMultiplier;
        TakeDamage(_baseDamage, _dir, _attackerID, _broadcast, false, isHead);
    }

    public void TakeDamage(float _amount, Vector3 _dir, int _attackerID, bool _broadcast, bool isPlayer = false, bool _isInHead = false)
    {
        healthScript.TakeDamage(_amount * this.damageMultiplier, _dir, _attackerID, _broadcast, isPlayer, isHead);
    }

    public float GetHealth()
    {
        return this.healthScript.GetHealth();
    }

    public float GetMaximumHealth()
    {
        return this.healthScript.GetMaximumHealth();
    }

    public int GetDamageableID()
    {
        return this.healthScript.DamageableID;
    }

    public void SetLobby(LobbyObject _lobby)
    {
        this.healthScript.SetLobby(_lobby);
    }

    public void BroadcastDamages(int _attacker, float _damages, float _health, Vector3 _dir)
    {
        this.healthScript.BroadcastDamages(_attacker, _damages, _health, _dir);
    }
}