using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class PointerTarget : MonoBehaviour
{
    public UnityAction OnHoverStart;
    public UnityAction OnHoverEnd;
    public UnityAction<Vector2> OnPress;
    public UnityAction<Vector2> OnRelease;

    public Vector3 TeleportTarget;

    public void Awake()
    {
        OnRelease += pos =>
        {
            if (!TeleportTarget.Equals(Vector3.zero))
            {
                SteamVR_Fade.Start( Color.clear, 0 );
                SteamVR_Fade.Start( Color.black, 0.2f );
                FindObjectOfType<Player>().transform.position = TeleportTarget;
            }
        };
    }
}
