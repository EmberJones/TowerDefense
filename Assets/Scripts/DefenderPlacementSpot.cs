using UnityEngine;

public class DefenderPlacementSpot : MonoBehaviour
{
    public bool IsOccupied { get; private set; }

    private DefenderFactory towerFactory;

    public void Initialize(DefenderFactory factory)
    {
        towerFactory = factory;
    }

    private void OnMouseDown()
    {
        PlaceDefender();
    }

    public void PlaceDefender()
    {
        if (IsOccupied || towerFactory == null) return;

        towerFactory.CreateDefender(DefenderFactory.DefenderType.Basic, transform.position);
        IsOccupied = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsOccupied ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}