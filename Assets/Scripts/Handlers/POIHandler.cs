using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class POIHandler : MonoBehaviour
{
    public PointOfInterest currentArea {  get; private set; }
    public PointOfInterest destinationArea {  get; private set; }

    public SerializedDictionary<PointOfInterest, float> knownAreas;
    public float explorationCooldown { get; private set; }
    public float forgetThreshold;

    private EventHandler eventHandler;

    private void Awake()
    {
        eventHandler = GetComponent<EventHandler>();
        eventHandler.UpdateCurrentAreaEvent += UpdateCurrentArea;
        eventHandler.UpdateDestinationEvent += UpdateDestination;
    }
    private void FixedUpdate()
    {
        explorationCooldown -= Time.deltaTime;
    }

    private void UpdateDestination(PointOfInterest poi)
    {
        destinationArea = poi;
    }

    public void MarkAreaAsVisited(PointOfInterest poi)
    {
        knownAreas[poi] = Time.time;
    }

    private void UpdateCurrentArea(PointOfInterest poi)
    {
        currentArea = poi;

        if (poi != null)
        {
            //eventHandler.UpdateDestination(null);

            if (!knownAreas.ContainsKey(poi))
                knownAreas.Add(poi, Time.time);
            else
                knownAreas[poi] = Time.time;

            explorationCooldown = 300f;
        }
    }
}
