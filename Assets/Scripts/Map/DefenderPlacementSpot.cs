using UnityEngine;

public class DefenderPlacementSpot : MonoBehaviour
{
    public bool IsOccupied { get; private set; }

    private DefenderFactory defenderFactory;
    public void Initialize(DefenderFactory factory)
    {
        defenderFactory = factory;
    }

    private void OnMouseDown()
    {
        if (IsOccupied) return;
        ShopUI.Instance?.Open(this);
    }

    public void PlaceDefender(DefenderFactory.DefenderType type)
    {
        if (IsOccupied || defenderFactory == null) return;

        defenderFactory.CreateTower(type, transform.position);
        IsOccupied = true;

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsOccupied ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}