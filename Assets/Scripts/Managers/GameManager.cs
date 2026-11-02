using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    [SerializeField] public List<NPCController> playerList { get; private set; }
    [SerializeField] public List<Group> groupList { get; private set; }

    private NPCSpawner npcSpawner;

    public Camera spectatorCamera;

    private void Start()
    {
        if(instance == null)
            instance = this;

        InitializePlayerList();
        InitializeEvents();
        InitializeGroupList();

        InitializeSpawning();
    }
    private void Update()
    {
        //Debug.Log(Time.time);
    }

    private void InitializeSpawning()
    {
        npcSpawner = GetComponent<NPCSpawner>();

        npcSpawner.StartSpawningLoop();
    }

    private void InitializeGroupList()
    {
        groupList = new List<Group>();

        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Human"))
        {
            Group group = go.GetComponent<GroupHandler>().currentGroup;

            if (group != null)
            {
                groupList.Add(group);
            }
        }
    }

    private void InitializePlayerList()
    {
        playerList = new List<NPCController>();

        foreach(GameObject go in GameObject.FindGameObjectsWithTag("Human"))
        {
            playerList.Add(go.GetComponent<NPCController>());
        }
    }

    private void InitializeEvents()
    {
        foreach (NPCController npc in playerList)
        {
            AddPlayer(npc);
        }
    }

    private void OnPlayerDeath(NPCController npc)
    {
        npc.eventHandler.OnPlayerDeathEvent -= OnPlayerDeath;
        playerList.Remove(npc);
    }

    private void OnGroupUpdate(Group group)
    {
        if(group == null) 
            return;

        if (!groupList.Contains(group))
            groupList.Add(group);

        if (group.IsEmpty())
        {
            groupList.Remove(group);
        }
    }
    public void AddPlayer(NPCController npc)
    {
        playerList.Add(npc);

        npc.eventHandler.OnPlayerDeathEvent += OnPlayerDeath;
        npc.eventHandler.OnGroupUpdateEvent += OnGroupUpdate;
    }
}
