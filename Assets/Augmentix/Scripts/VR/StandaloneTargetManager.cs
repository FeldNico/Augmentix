using System.Collections;
using Photon.Pun;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace Augmentix.Scripts.VR
{
    public class StandaloneTargetManager: TargetManager
    {
        public SteamVR_Action_Boolean Menu =  SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Menu");
        public SteamVR_Action_Boolean Help =  SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Help");

        public float TeleportFadeDuration = 0.1f;
        
        new void Start()
        {
            base.Start();

            OnConnection += () =>
            {
                VRUI.Instance.ConnectionText.text = "Connected";
                VRUI.Instance.ConnectionText.color = Color.green;
                var player = PhotonNetwork.Instantiate("Player", Vector3.zero, Quaternion.identity);
                player.transform.parent = Camera.main.transform;
                player.transform.localPosition = Vector3.zero;
                player.transform.localRotation = Quaternion.identity;
                PhotonNetwork.Instantiate("Treveris", new Vector3(), new Quaternion());

                //StartCoroutine(test());
                IEnumerator test()
                {
                    yield return new WaitForSeconds(3);
                    GameObject.Find("00001(Clone)").GetComponent<OOI.OOI>().Interact(OOI.OOI.InteractionFlag.Text);
                }
            };
            
            Menu.AddOnStateUpListener((action, source) =>
            {
                Map.Instance.gameObject.SetActive(!Map.Instance.gameObject.activeSelf);
            },SteamVR_Input_Sources.LeftHand);
            Map.Instance.gameObject.SetActive(false);
        }
    }
}

