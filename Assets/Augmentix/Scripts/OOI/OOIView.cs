using System;
using Augmentix.Scripts.AR.UI;
using Augmentix.Scripts.OOI.Editor;
using Augmentix.Scripts.VR;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Video;

namespace Augmentix.Scripts.OOI
{
    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(MeshCollider))]
    public class OOIView : MonoBehaviourPunCallbacks, IPunObservable
    {
        [Flags]
        public enum InteractionFlag
        {
            Highlight = 0x1,
            Text = 0x2,
            Video = 0x4,
            Animation = 0x8,
            Manipulate = 0x16
        }

#if UNITY_EDITOR
        [OOIViewEditor.EnumFlagsAttribute]
#endif
        public InteractionFlag Flags;

        [TextArea]
        public string Text;

        public float TextScale = 0.02f;

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            throw new System.NotImplementedException();
        }

        private void Start()
        {

        }

        private GameObject _videoCube = null;
        private GameObject _textCube = null;
        [PunRPC]
        public void Interact(InteractionFlag flag)
        {
            var view = GetComponent<PhotonView>();
            if (!view.IsMine)
                view.RPC("Interact",view.Owner,flag);

            switch (flag)
            {
                case InteractionFlag.Highlight:
                {
                    if (view.IsMine)
                    {
                        VRUI.Instance.ToggleHighlightTarget(gameObject);
                    }
                    else
                    {
                        OOIUI.Instance.ToggleHighlightTarget(gameObject);
                    }
                    break;
                }
                case InteractionFlag.Video:
                {
                    ToggleVideo();
                    break;
                }
                case InteractionFlag.Text:
                {
                    ToggleText();
                    break;
                }
            }
        }

        private void ToggleText()
        {

            GameObject player = FindObjectOfType<PlayerSynchronizer>().gameObject;

            if (_textCube == null || !_textCube.gameObject.activeSelf)
            {
                if (_textCube == null)
                {
                    _textCube = new GameObject(gameObject.name+"_Text");
                    _textCube.transform.parent = transform;
                    var text = _textCube.AddComponent<TextMesh>();
                    text.fontSize = 90;
                    text.anchor = TextAnchor.MiddleCenter;
                }

                _textCube.GetComponent<TextMesh>().text = Text;
                _textCube.transform.localScale = new Vector3(TextScale,TextScale,TextScale);

                Vector3 nearestPoint = transform.position;
                foreach (var child in GetComponentsInChildren<Renderer>())
                    if (child.gameObject != _textCube && Vector3.Distance(player.transform.position,child.bounds.ClosestPoint(player.transform.position)) < Vector3.Distance(player.transform.position,nearestPoint))
                        nearestPoint = child.bounds.ClosestPoint(player.transform.position);

                nearestPoint.y = player.transform.position.y;

                _textCube.SetActive(true);
                _textCube.transform.position = nearestPoint;
                _textCube.transform.LookAt(player.transform);
                _textCube.transform.Rotate(Vector3.up,180);
            }
            else
            {
                _textCube.SetActive(false);
            }

        }
    
        private void ToggleVideo()
        {
            var video = GetComponent<VideoPlayer>();
            if (video == null)
            {
                Debug.LogError(gameObject.name+" does not contain a VideoPlayer");
                return;
            }

            if (video.isPlaying)
            {
                video.Stop();
                _videoCube.SetActive(false);
            } else {
                GameObject player = FindObjectOfType<PlayerSynchronizer>().gameObject;

                if (_videoCube == null)
                {
                    _videoCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    _videoCube.name = "VideoCube";
                    _videoCube.transform.parent = transform;
                    _videoCube.GetComponent<Renderer>().material = player.GetComponentInChildren<Renderer>().material;
                    _videoCube.GetComponent<Renderer>().material.shader = Shader.Find("Lightweight Render Pipeline/Unlit");

                    video.targetMaterialRenderer = _videoCube.GetComponent<Renderer>();
                    video.targetMaterialProperty = "_BaseMap";
                }

                Vector3 nearestPoint = transform.position;
                foreach (var child in GetComponentsInChildren<Renderer>())
                    if (child.gameObject != _videoCube && Vector3.Distance(player.transform.position,child.bounds.ClosestPoint(player.transform.position)) < Vector3.Distance(player.transform.position,nearestPoint))
                        nearestPoint = child.bounds.ClosestPoint(player.transform.position);

                nearestPoint.y = player.transform.position.y;

                _videoCube.SetActive(true);
                _videoCube.transform.position = nearestPoint;
                _videoCube.transform.LookAt(player.transform);
                _videoCube.transform.localScale = new Vector3(1,1f *video.height/video.width,0.01f);
            
                video.Play();
            }
        }
    }
}

































