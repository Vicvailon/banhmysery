using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class veggieCounter : MonoBehaviour
{
    
    [SerializeField] private List<Transform> transformBoundariesList = new List<Transform>();   
    [SerializeField] private int maxNumberOfPlatforms;
    [SerializeField] private Collider2D core;
    [SerializeField] private Transform upperLeftBoundary;
    [SerializeField] private Transform upperRightBoundary;
    [SerializeField] private Transform lowerRightBoundary;
    [SerializeField] private Transform lowerLeftBoundary;
    [SerializeField] private GameObject veggiePlatform;
    [SerializeField] private List<GameObject> veggiePlatformList = new List<GameObject>();




    private void Start()
    {
        
       
    }
}
