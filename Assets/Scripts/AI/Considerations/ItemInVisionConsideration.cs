using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="ItemInFOV", menuName ="UtilityAI/Considerations/ItemInFOV")]
public class ItemInVisionConsideration : Consideration
{
    [SerializeField] public AnimationCurve responseCurve;
    public override float ScoreConsideration(NPCController npc)
    {
        score = responseCurve.Evaluate(Convert.ToInt32(npc.HasItemToCollect));
        return score;
    }
}
