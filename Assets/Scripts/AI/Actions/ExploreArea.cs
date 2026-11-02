using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Explore", menuName = "UtilityAI/Actions/Explore")]
public class ExploreArea : Action
{
    public POIType type;
    public override void Execute(NPCController npc)
    {
        //Already in the wanted type of area, so look around here instead of walking to the next one
        PointOfInterest currentArea = npc.poiHandler.currentArea;
        if (type != POIType.None && currentArea != null && currentArea.type == type)
        {
            npc.MoveAroundArea();
            return;
        }

        PointOfInterest nearestArea = POIManager.instance.GetClosestArea(npc, type);

        npc.ExploreArea(nearestArea);
    }
}
