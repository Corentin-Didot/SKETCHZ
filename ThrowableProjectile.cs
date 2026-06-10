using UnityEngine;

public abstract class ThrowableProjectile : MonoBehaviour
{
    public bool IsExploded { get; protected set; }
    protected LineRenderer lineRenderer;
    protected int throwerID;

    protected virtual void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    public abstract void InitializeStats(int _attackerID, float radius, int damage, float fuse, LayerMask mask);
    public abstract void Arm();
    public abstract void Disarm();

    public void HideLine()
    {
        if (lineRenderer != null) lineRenderer.enabled = false;
    }

    public void IgnorePlayer(Collider playerCollider)
    {
        Collider myCollider = GetComponent<Collider>();
        if (myCollider != null && playerCollider != null)
            Physics.IgnoreCollision(myCollider, playerCollider);
    }
}