using UnityEngine;

public class CoinUI : MonoBehaviour
{
    [SerializeField] private string format = "Coins: {0}";

    private CoinWallet wallet;

    private void OnEnable()
    {
        wallet = CoinWallet.Instance;
        wallet.OnCoinsChanged += Refresh;
        Refresh(wallet.Coins);
    }

    private void Start()
    {
        if (wallet != null)
            Refresh(wallet.Coins);
    }

    private void OnDisable()
    {
        if (wallet != null)
            wallet.OnCoinsChanged -= Refresh;
    }

    private void Refresh(int coins)
    {
        UIText.Set(gameObject, string.Format(format, coins));
    }
}