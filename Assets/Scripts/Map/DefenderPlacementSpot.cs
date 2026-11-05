using UnityEngine;

public class DefenderPlacementSpot : MonoBehaviour
{
    public bool IsOccupied { get; private set; }

    private DefenderFactory defenderFactory;
    public void Initialize(DefenderFactory factory)
    {
        defenderFactory = factory;
    }

    private void OnMouseUpAsButton()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        if (IsOccupied) return;
        ShopUI.Instance?.Open(this);
    }

    public bool PlaceDefender(DefenderFactory.DefenderType type)
    {
        if (IsOccupied || defenderFactory == null) return false;

        Defender placed = defenderFactory.CreateTower(type, transform.position);
        if (placed == null) return false;

        IsOccupied = true;

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        return true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsOccupied ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}