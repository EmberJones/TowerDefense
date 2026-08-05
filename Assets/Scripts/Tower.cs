using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Tower : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    public int CurrentHealth { get => currentHealth; set => currentHealth = value; }
    public int MaxHealth { get => maxHealth; set => maxHealth = value; }

    [SerializeField] private MeshRenderer towerMesh;

    void Start()
    {
        CurrentHealth = MaxHealth;
    }

    void Update()
    {

    }


    void IDamageable.TakeDamage(int damageAmount)
    {
        CurrentHealth -= damageAmount;
        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
        //Game Over call
    }

    public void TakeDamage(int damageAmount)
    {
        throw new System.NotImplementedException();
    }

}