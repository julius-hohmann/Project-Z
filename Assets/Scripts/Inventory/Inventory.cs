using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AYellowpaper.SerializedCollections;
using Unity.VisualScripting;

public class Inventory : MonoBehaviour
{
    public List<ContainerSlot> slots;
    public int maxSlots = 20;

    private EventHandler eventHandler;
    private void Awake()
    {
        eventHandler = GetComponent<EventHandler>();
    }

    public bool AddItem(Item newItem, int amount)
    {
        if (!newItem.itemData.isUnique && slots.Count > 0)
        {
            ContainerSlot existingSlot = slots.Find(slot => slot.item == newItem.itemData);
            if (existingSlot != null)
            {
                existingSlot.quantity += newItem.amount;
                return true;
            }
        }

        if (slots.Count < maxSlots)
        {
            ContainerSlot cs = new ContainerSlot(newItem.itemData, amount);

            slots.Add(cs);
            return true;
        }
        return false;
    }

    public bool RemoveItem(ItemObject item, int amount)
    {
        ContainerSlot slot = slots.Find(s => s.item.name == item.name);
        if (slot != null)
        {
            slot.quantity -= amount;
            if (slot.quantity <= 0) slots.Remove(slot);
            return true;
        }
        return false;
    }
    public int CheckForAmmo(ContainerSlot slot, int requestedAmount)
    {
        if (slot.quantity >= requestedAmount)
        {
            slot.quantity -= requestedAmount;

            if (slot.quantity <= 0)
                slots.Remove(slot);

            return requestedAmount;
        }
        else
        {
            int remainingAmmo = slot.quantity;

            slots.Remove(slot);

            return remainingAmmo;
        }
    }

    internal void UseItem(ConsumableType consumableType)
    {
        foreach (ContainerSlot slot in slots)
        {
            if(slot.item.type != ItemType.Consumable)
                continue;

            Consumable item = slot.item as Consumable;

            if(item.consumableType != consumableType)
                continue;

            eventHandler.ConsumeItem(consumableType, item.value);
            slot.quantity--;
            if (slot.quantity <= 0)
                slots.Remove(slot);
            return;
        }
    }

    internal void DropItem(ContainerSlot slot)
    {
        Vector3 randomPosition = gameObject.transform.position + new Vector3(Random.Range(-1f, 2f), 0.5f, Random.Range(-1f, 2f));

        GameObject droppedItem = Instantiate(slot.item.itemPrefab, randomPosition, Quaternion.identity);
        droppedItem.GetComponent<Item>().amount = slot.quantity;

        RemoveItem(slot.item, slot.quantity);
    }

    //Consumable, Resource, Ammunition, Equipment, Weapon
    //TODO: Make this return item instead of bool
    public bool HasConsumable(ConsumableType consumableType)
    {
        foreach(ContainerSlot slot in slots)
        {
            if (slot.item.type != ItemType.Consumable)
                continue;

            Consumable item = slot.item as Consumable;

            if (item.consumableType != consumableType)
                continue;

            if (item.consumableType == consumableType)
                return true;
        }
        return false;
    }
    public bool HasResource(ResourceType resourceType)
    {
        foreach (ContainerSlot slot in slots)
        {
            if (slot.item.type != ItemType.Resource)
                continue;

            Resource item = slot.item as Resource;

            if (item.ResourceType != resourceType)
                continue;

            if (item.ResourceType == resourceType)
                return true;
        }
        return false;
    }
    public bool HasAmmunition(WeaponType type)
    {
        foreach (ContainerSlot slot in slots)
        {
            if (slot.item.type != ItemType.Ammunition)
                continue;

            Ammunition item = slot.item as Ammunition;

            if (item.ammoType != type)
                continue;

            if (item.ammoType == type)
                return true;
        }
        return false;
    }
    public bool HasEquipment(EquipmentType type)
    {
        foreach (ContainerSlot slot in slots)
        {
            if (slot.item.type != ItemType.Equipment)
                continue;

            Equipment item = slot.item as Equipment;

            if (item.equipType != type)
                continue;

            if (item.equipType == type)
                return true;
        }
        return false;
    }
    public Weapon HasWeapon(WeaponType type)
    {
        foreach (ContainerSlot slot in slots)
        {
            if (slot.item.type != ItemType.Weapon)
                continue;

            Weapon item = slot.item as Weapon;

            if (item.weaponType != type)
                continue;

            if (item.weaponType == type)
                return item;
        }
        return null;
    }

    public List<Weapon> GetAllWeapons()
    {
        List<Weapon> weapons = new List<Weapon>();

        foreach (ContainerSlot slot in slots)
        {
            if(slot.item.type == ItemType.Weapon)
                weapons.Add(slot.item as Weapon);
        }

        return weapons;
    }
}
