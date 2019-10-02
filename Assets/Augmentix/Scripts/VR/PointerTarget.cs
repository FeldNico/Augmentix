using System;
using System.Collections;
using System.Collections.Generic;
using Augmentix.Scripts;
using Augmentix.Scripts.VR;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class PointerTarget : MonoBehaviour
{
    public UnityAction OnHoverStart;
    public UnityAction OnHoverEnd;
    public UnityAction<Vector2> OnPress;
    public UnityAction<Vector2> OnRelease;

    public Vector3 TeleportTarget;

    private Button _button;
    public void Awake()
    {
        _button = GetComponent<Button>();
        
        OnRelease += pos =>
        {
            if (!TeleportTarget.Equals(Vector3.zero))
            {
                var fadeDuraction = ((StandaloneTargetManager) TargetManager.Instance).TeleportFadeDuration;
                
                SteamVR_Fade.Start( Color.clear, 0 );
                SteamVR_Fade.Start( Color.black, fadeDuraction );
                FindObjectOfType<Player>().transform.position = TeleportTarget;

                StartCoroutine(WaitForFade());
                
                IEnumerator WaitForFade()
                {
                    yield return new WaitForSeconds(fadeDuraction);
                    SteamVR_Fade.Start( Color.clear, fadeDuraction );
                }

            }
        };

        if (_button != null)
        {
            OnRelease += pos =>
            {
                var colors = _button.colors;
                var normal = colors.normalColor;
                colors.normalColor = colors.selectedColor;
                colors.selectedColor = normal;
                _button.colors = colors;
            };
            
            OnPress += pos =>
            {
                var colors = _button.colors;
                var normal = colors.normalColor;
                colors.normalColor = colors.selectedColor;
                colors.selectedColor = normal;
                _button.colors = colors;
            };

            OnHoverStart += () =>
            {
                var colors = _button.colors;
                var normal = colors.normalColor;
                colors.normalColor = colors.highlightedColor;
                colors.highlightedColor = normal;
                _button.colors = colors;
            };
        
            OnHoverEnd += () =>
            {
                var colors = _button.colors;
                var normal = colors.normalColor;
                colors.normalColor = colors.highlightedColor;
                colors.highlightedColor = normal;
                _button.colors = colors;
            };
        }
        
        
    }
}
