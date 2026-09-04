using UnityEngine;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    public GameObject panelRoot;

    private DefenderPlacementSpot currentSpot;

    private void Awake()
    {
        Instance = this;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Open(DefenderPlacementSpot spot)
    {
        currentSpot = spot;

        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void Close()
    {
        currentSpot = null;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void PurchaseTower(string towerTypeName)
    {
        if (currentSpot == null) return;

        if (System.Enum.TryParse(towerTypeName, out DefenderFactory.DefenderType type))
        {
            currentSpot.PlaceDefender(type);
        }

        Close();
    }
}