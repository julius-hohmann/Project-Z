using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatsHandler : MonoBehaviour
{
    public string Name;
    //public Team team;
    private Inventory inventory;
    private EventHandler eventHandler;

    public float maxHealth = 100f;
    public float maxStamina = 100f;
    public float maxHunger = 100f;
    public float maxThirst = 100f;

    //Damage per second while hunger or thirst are empty
    public float starvationDamage = 0.25f;
    public float dehydrationDamage = 0.5f;
    private bool isDead;

    public float health {  get; private set; }
    public float stamina { get; private set; }
    public float hunger { get; private set; }
    public float thirst { get; private set; }
    public float armor { get; private set; }
    public GameObject recentlyAttackedBy {  get; private set; }


    private void Awake()
    {
        InitializeStats();
        InitializeEvents();
        StartCoroutine(GetHungry());
        StartCoroutine(GetThirsty());
    }

    private void InitializeEvents()
    {
        eventHandler = GetComponent<EventHandler>();
        eventHandler.OnEquipmentChange += UpdateArmor;
        eventHandler.OnConsumableUsed += RestoreStats;
    }

    public void DealDamage(float damage, GameObject attacker)
    {
        //Calculate damage by multiplying damage by 100 divided by 100 + current armor
        float newDamage = damage * 100 / (100 + armor);
        health -= newDamage;

        recentlyAttackedBy = attacker;

        if (health <= 0)
            Die();
    }

    //Ignores armor and doesn't count as an attack
    private void TakeDamageOverTime(float damage)
    {
        health -= damage;

        if (health <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        NPCController npc = gameObject.GetComponent<NPCController>();

        if (npc != null)
            eventHandler.PlayerDeath(npc);

        while(inventory.slots.Count > 0)
        {
            inventory.DropItem(inventory.slots[0]);
        }

        Destroy(gameObject);
    }

    private void InitializeStats()
    {
        inventory = gameObject.GetComponent<Inventory>();

        health = maxHealth;
        stamina = maxStamina;
        hunger = maxHunger;
        thirst = maxThirst;
        armor = 0f;
    }

    private void UpdateArmor(float amount)
    {
        armor = amount;
        Debug.Log(name + ": " + armor);
    }

    private IEnumerator GetHungry()
    {
        float depletionRate = 0.1f;
        while (true)
        {
            yield return new WaitForSeconds(1f);
            if(hunger > 0.01f)
                hunger -= depletionRate;
            else
                TakeDamageOverTime(starvationDamage);
        }
    }
    private IEnumerator GetThirsty()
    {
        float depletionRate = 0.2f;
        while (true)
        {
            yield return new WaitForSeconds(1f);
            if(thirst > 0.01f)
                thirst -= depletionRate;
            else
                TakeDamageOverTime(dehydrationDamage);
        }
    }
    private void RestoreStats(ConsumableType type, int amount)
    {
        switch (type)
        {
            case ConsumableType.Healing:
                RestoreHealth(amount);
                break;
            case ConsumableType.Food:
                RestoreHunger(amount);
                break;
            case ConsumableType.Water:
                RestoreThirst(amount);
                break;
        }
    }


    private void RestoreHealth(int amount)
    {
        if (health >= maxHealth)
            return;

        if (health + amount >= maxHealth)
            health = maxHealth;
        else
            health += amount;
    }

    private void RestoreHunger(int amount)
    {
        if (hunger + amount >= maxHunger)
            hunger = maxHunger;
        else
            hunger += amount;
    }

    private void RestoreThirst(int amount)
    {
        if (thirst + amount >= maxThirst)
            thirst = maxThirst;
        else
            thirst += amount;
    }
}
