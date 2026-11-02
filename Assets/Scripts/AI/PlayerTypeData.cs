using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerType { Achiever, Explorer, Socializer, Killer }
[CreateAssetMenu(fileName ="New Playertype", menuName ="UtilityAI/PlayerType")]
public class PlayerTypeData : ScriptableObject
{
    public PlayerType playerType;

    //Interests min = 0 / max = 1
    public float teamUp;
    public float fight;
    public float collectItems;
    public float completeQuests;
    public float betrayGroup;
}
