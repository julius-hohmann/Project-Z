using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroupHandler : MonoBehaviour
{
    public Group currentGroup;

    public bool isInvited = false;
    private Group invitedBy = null;
    public float invitationCooldown {  get; private set; }

    public float groupJoinTime;
    public float TimeInGroup
    {
        get { return Time.time - groupJoinTime; }
    }

    private List<GameObject> recentlyInvited = new List<GameObject>();
    public List<GameObject> RecentlyInvited
    {
        get
        {
            recentlyInvited.RemoveAll(obj => !obj);
            return recentlyInvited;
        }
    }

    private EventHandler eventHandler;
    private NPCController npc;

    private void Awake()
    {
        eventHandler = GetComponent<EventHandler>();
        npc = GetComponent<NPCController>();

        eventHandler.OnPlayerDeathEvent += OnDeath;
    }


    private void FixedUpdate()
    {
        invitationCooldown -= Time.deltaTime;
    }

    //Agents can only join a group if they aren't responsible for other group members
    public bool CanJoinGroup()
    {
        if (currentGroup == null)
            return true;

        return currentGroup.groupLeader == npc && currentGroup.GroupMembers.Count == 1;
    }

    public void InviteToGroup(Group newGroup)
    {
        if (isInvited || !CanJoinGroup())
            return;

        invitedBy = newGroup;
        isInvited = true;
    }

    public void AcceptGroupInvitation()
    {
        Group newGroup = invitedBy;

        isInvited = false;
        invitedBy = null;

        invitationCooldown = 999f;

        //The situation might have changed since the invitation was sent
        if (newGroup != null && newGroup.groupLeader != null && CanJoinGroup())
            newGroup.AddMember(npc);
    }
    public void DeclineGroupInvitation()
    {
        isInvited = false;
        invitedBy = null;

        invitationCooldown = 999f;
    }

    //The traitor leaves the group and neither side invites the other one again
    public void BetrayGroup()
    {
        if (currentGroup == null)
            return;

        foreach (NPCController member in currentGroup.GroupMembers)
        {
            if (member == npc)
                continue;

            member.groupHandler.RecentlyInvited.Add(gameObject);
            RecentlyInvited.Add(member.gameObject);
        }

        currentGroup.RemoveMember(npc);
    }

    private void OnDeath(NPCController deadNPC)
    {
        if (currentGroup != null)
            currentGroup.RemoveMember(npc);
    }
}

public class Group
{
    private const int SlotsPerRing = 8;
    private const float FirstRingRadius = 2.5f;
    private const float RingSpacing = 1.5f;

    public string Name;
    public NPCController groupLeader;

    private List<NPCController> groupMembers = new List<NPCController>();
    public List<NPCController> GroupMembers
    {
        get
        {
            groupMembers.RemoveAll(obj => !obj);
            return groupMembers;
        }
    }

    public Group(NPCController leader)
    {
        groupLeader = leader;
        AddMember(leader);
    }

    public void AddMember(NPCController newMember)
    {
        if (GroupMembers.Contains(newMember))
            return;

        if (newMember.groupHandler.currentGroup != null)
            newMember.groupHandler.currentGroup.RemoveMember(newMember);

        GroupMembers.Add(newMember);
        newMember.groupHandler.currentGroup = this;
        newMember.groupHandler.groupJoinTime = Time.time;
        newMember.eventHandler.GroupUpdate(this);
    }

    public void RemoveMember(NPCController member)
    {
        if (!GroupMembers.Contains(member))
            return;

        GroupMembers.Remove(member);
        member.groupHandler.currentGroup = null;

        if (groupLeader == member)
            groupLeader = IsEmpty() ? null : GroupMembers[0];

        member.eventHandler.GroupUpdate(this);
    }

    public bool IsEmpty()
    {
        return GroupMembers.Count == 0;
    }

    //Every follower gets its own slot, starting directly behind the leader and filling up rings around it
    public Vector3 GetFormationPosition(NPCController member)
    {
        int followerIndex = 0;
        foreach (NPCController groupMember in GroupMembers)
        {
            if (groupMember == member)
                break;

            if (groupMember != groupLeader)
                followerIndex++;
        }

        int ring = followerIndex / SlotsPerRing;
        int slot = followerIndex % SlotsPerRing;

        float angle = 180f + (slot % 2 == 0 ? 1 : -1) * ((slot + 1) / 2) * (360f / SlotsPerRing);
        float radius = FirstRingRadius + ring * RingSpacing;

        return groupLeader.transform.position + groupLeader.transform.rotation * Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
    }
}
