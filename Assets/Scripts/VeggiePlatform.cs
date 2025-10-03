using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class VeggiePlatform : MonoBehaviour
{
    [SerializeField] GameObject platformPrefab;

    public enum TypesOfVeggies
    {
        Carrots,
        Corriander,
        Cucumbers
    }

    [SerializeField] List<TypesOfVeggies> platformList;

    private void VeggieAssignmentHandler()
    {
        platformList = new List<TypesOfVeggies>();

        foreach (TypesOfVeggies typeOfVeggies in Enum.GetValues(typeof(TypesOfVeggies)))
        {
            if(!platformList.Contains(typeOfVeggies))
            {
                platformList.Add(typeOfVeggies);
                
            }
        }
    }
    private void InstantiateCounters(GameObject prefab, Vector3 spawnLocation)
    {
        foreach(TypesOfVeggies typeOfVeggies in Enum.GetValues (typeof(TypesOfVeggies)))
        {
            GameObject plat = Instantiate(prefab, spawnLocation, transform.rotation);

            plat.name = typeOfVeggies.ToString();
        }
    }


    private void Start()
    {
        VeggieAssignmentHandler();
        InstantiateCounters(platformPrefab, SpawnPlatformCalculator());
        
    }
    private Vector3 SpawnPlatformCalculator()
    {
        Vector3 spawnTransform = new Vector3(0, 0, 0);
        return spawnTransform;
    }
}
