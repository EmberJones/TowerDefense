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

        defenderFactory.CreateDefender(type, transform.position);
        IsOccupied = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsOccupied ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}