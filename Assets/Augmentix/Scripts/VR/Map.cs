using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    public static Map Instance { private set; get; }
    
    public SpriteRenderer MapImage;
    public PointerTarget[] Targets;

    private void Awake()
    {
        Instance = this;
    }
}
