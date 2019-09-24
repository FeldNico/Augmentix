using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Valve.VR;

public class Pointer : MonoBehaviour
{

    public SteamVR_Action_Boolean OnClick;
    public GameObject Dot;
    public PointerTarget CurrentPointerTarget { private set; get; }

    private LineRenderer lineRenderer;
    private float MaxLength = 5f;
    // Start is called before the first frame update
    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        
        OnClick.AddOnStateDownListener((action, source) =>
        {
            if (CurrentPointerTarget == null)
                return;

            var pos = CurrentPointerTarget.transform.InverseTransformPoint(Dot.transform.position);
            
            CurrentPointerTarget.OnPress.Invoke(new Vector2(pos.x,pos.z));
        },SteamVR_Input_Sources.RightHand);
        
        OnClick.AddOnStateUpListener((action, source) =>
        {
            if (CurrentPointerTarget == null)
                return;

            var pos = CurrentPointerTarget.transform.InverseTransformPoint(Dot.transform.position);
            
            CurrentPointerTarget.OnRelease.Invoke(new Vector2(pos.x,pos.z));
        },SteamVR_Input_Sources.RightHand);
    }

    // Update is called once per frame
    private void Update()
    {
        UpdateLine();
    }

    private void UpdateLine()
    {
        RaycastHit hit;
        var ray = new Ray(transform.position,transform.forward);
        Physics.Raycast(ray, out hit, MaxLength);

        if (hit.collider != null && hit.transform.GetComponent<PointerTarget>())
        {
            Dot.transform.position = hit.point;
            lineRenderer.SetPosition(0,transform.position);
            lineRenderer.SetPosition(1,hit.point);

            var pointerTarget = hit.transform.GetComponent<PointerTarget>();

            if (pointerTarget != CurrentPointerTarget)
            {
                if (CurrentPointerTarget != null)
                {
                    CurrentPointerTarget.OnHoverEnd.Invoke();
                }
                pointerTarget.OnHoverStart.Invoke();
                CurrentPointerTarget = pointerTarget;
                Dot.gameObject.SetActive(true);
            }
        }
        else
        {
            if (CurrentPointerTarget != null)
            {
                lineRenderer.SetPosition(0,Vector3.zero);
                lineRenderer.SetPosition(1,Vector3.zero);
                CurrentPointerTarget.OnHoverEnd.Invoke();
            }
            CurrentPointerTarget = null;
            Dot.gameObject.SetActive(false);
        }
    }
}
