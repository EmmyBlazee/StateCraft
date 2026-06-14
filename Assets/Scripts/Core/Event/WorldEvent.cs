using System;
using UnityEngine;
using StateCraft.Data;

[Serializable]
public class WorldEvent
{
    public WorldEventType eventType;

    public string title;
    public string description;

    public AlertSeverity severity;

    public CountryDataSO sourceCountry;
    public CountryDataSO targetCountry;

    public RegionDataSO targetRegion;

    public float impactValue;

    public DateTime timestamp;

    public Sprite icon;
}