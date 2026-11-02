using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class POIManager : MonoBehaviour
{
    public static POIManager instance;
    public List<PointOfInterest> availableAreas { get; private set; }

    private void Awake()
    {
        if(instance == null)
            instance = this;

        InitializeAreaList();
    }

    private void InitializeAreaList()
    {
        availableAreas = new List<PointOfInterest>();

        foreach (GameObject item in GameObject.FindGameObjectsWithTag("Area"))
        {
            availableAreas.Add(item.GetComponent<PointOfInterest>());
        }
    }

    public PointOfInterest GetClosestArea(NPCController npc, POIType type)
    {
        PointOfInterest nearestArea = null;
        float distance = Mathf.Infinity;

        foreach (PointOfInterest area in availableAreas)
        {
            PointOfInterest poi = area.GetComponent<PointOfInterest>();

            //Exploring means going somewhere else, even if the current area was already forgotten
            if (poi == npc.poiHandler.currentArea)
                continue;

            if (type != POIType.None)
            {
                if (poi.type != type)
                    continue;
            }
            else
            {
                if (npc.poiHandler.knownAreas.ContainsKey(poi))
                {
                    if (Time.time - npc.poiHandler.knownAreas[poi] < npc.poiHandler.forgetThreshold)
                        continue;
                }
            }

            float newDistance = Vector3.Distance(npc.transform.position, area.transform.position);
            if (newDistance < distance)
            {
                distance = newDistance;
                nearestArea = area;
            }                
        }

        if (nearestArea == null && type == POIType.None)
            return GetLeastRecentlyVisitedArea(npc);

        return nearestArea;
    }

    private PointOfInterest GetLeastRecentlyVisitedArea(NPCController npc)
    {
        PointOfInterest oldestArea = null;
        float oldestVisit = Mathf.Infinity;

        foreach (PointOfInterest area in availableAreas)
        {
            if (area == npc.poiHandler.currentArea || !npc.poiHandler.knownAreas.ContainsKey(area))
                continue;

            if (npc.poiHandler.knownAreas[area] < oldestVisit)
            {
                oldestVisit = npc.poiHandler.knownAreas[area];
                oldestArea = area;
            }
        }
        return oldestArea;
    }

    public int GetNumberOfUnexploredAreas(NPCController npc)
    {
        int areaCount = 0;

        foreach (PointOfInterest area in availableAreas)
        {
            if (!npc.poiHandler.knownAreas.ContainsKey(area))
                areaCount++;
        }

        return areaCount;
    }
}
