using System;
using Augmentix.Scripts.AR;
using Augmentix.Scripts.AR.UI;
#if UNITY_EDITOR
using Augmentix.Scripts.OOI.Editor;
#endif
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
            Highlight = 1,
            Text = 2,
            Video = 4,
            Animation = 8,
            Manipulate = 16,
            Scale = 32,
            Changeable = 64
        }

#if UNITY_EDITOR
        [OOIViewEditor.EnumFlagsAttribute]
#endif
        public InteractionFlag Flags;

        [TextArea] public string Text;

        public float TextScale = 0.02f;

        public bool IsTangible { private set; get; } = false;

        private void Start()
        {
            IsTangible = GetComponentInParent<TreverisView>() == null;

            GetComponent<PhotonView>().OwnershipTransfer = OwnershipOption.Takeover;

            if (TargetManager.Instance.Type == TargetManager.PlayerType.Primary)
            {
                PickupTarget.Instance.GotPlayer += player =>
                {
                    var treveris = TreverisView.GetTreverisByPlayer(player.GetComponent<PhotonView>().Owner);
                    if (transform.IsChildOf(treveris.transform))
                        GetComponent<PhotonView>().TransferOwnership(PhotonNetwork.LocalPlayer);
                };

                PickupTarget.Instance.LostPlayer += player =>
                {
                    var treveris = TreverisView.GetTreverisByPlayer(player.GetComponent<PhotonView>().Owner);
                    if (transform.IsChildOf(treveris.transform))
                        GetComponent<PhotonView>().TransferOwnership(player.GetComponent<PhotonView>().Owner);
                };
            }
        }

        private GameObject _videoCube = null;
        private GameObject _textCube = null;

        [PunRPC]
        public void Interact(InteractionFlag flag)
        {
            var view = GetComponent<PhotonView>();
            if (view.IsMine)
                view.RPC("Interact", PickupTarget.Instance.PlayerSync.GetComponent<PhotonView>().Owner, flag);

            switch (flag)
            {
                case InteractionFlag.Highlight:
                {
                    if (!view.IsMine)
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
                    _textCube = new GameObject(gameObject.name + "_Text");
                    _textCube.transform.parent = transform;
                    var text = _textCube.AddComponent<TextMesh>();
                    text.fontSize = 90;
                    text.anchor = TextAnchor.MiddleCenter;
                }

                _textCube.GetComponent<TextMesh>().text = Text;
                _textCube.transform.localScale = new Vector3(TextScale, TextScale, TextScale);

                Vector3 nearestPoint = transform.position;
                foreach (var child in GetComponentsInChildren<Renderer>())
                    if (child.gameObject != _textCube &&
                        Vector3.Distance(player.transform.position,
                            child.bounds.ClosestPoint(player.transform.position)) <
                        Vector3.Distance(player.transform.position, nearestPoint))
                        nearestPoint = child.bounds.ClosestPoint(player.transform.position);

                nearestPoint.y = player.transform.position.y;

                _textCube.SetActive(true);
                _textCube.transform.position = nearestPoint;
                _textCube.transform.LookAt(player.transform);
                _textCube.transform.Rotate(Vector3.up, 180);
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
                Debug.LogError(gameObject.name + " does not contain a VideoPlayer");
                return;
            }

            if (video.isPlaying)
            {
                video.Stop();
                _videoCube.SetActive(false);
            }
            else
            {
                GameObject player = FindObjectOfType<PlayerSynchronizer>().gameObject;

                if (_videoCube == null)
                {
                    _videoCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    _videoCube.name = "VideoCube";
                    _videoCube.transform.parent = transform;
                    _videoCube.GetComponent<Renderer>().material = player.GetComponentInChildren<Renderer>().material;
                    _videoCube.GetComponent<Renderer>().material.shader =
                        Shader.Find("Lightweight Render Pipeline/Unlit");

                    video.targetMaterialRenderer = _videoCube.GetComponent<Renderer>();
                    video.targetMaterialProperty = "_BaseMap";
                }

                Vector3 nearestPoint = transform.position;
                foreach (var child in GetComponentsInChildren<Renderer>())
                    if (child.gameObject != _videoCube &&
                        Vector3.Distance(player.transform.position,
                            child.bounds.ClosestPoint(player.transform.position)) <
                        Vector3.Distance(player.transform.position, nearestPoint))
                        nearestPoint = child.bounds.ClosestPoint(player.transform.position);

                nearestPoint.y = player.transform.position.y;

                _videoCube.SetActive(true);
                _videoCube.transform.position = nearestPoint;
                _videoCube.transform.LookAt(player.transform);
                _videoCube.transform.localScale = new Vector3(1, 1f * video.height / video.width, 0.01f);

                video.Play();
            }
        }

        
        
        
        
        
        private float m_Distance;
        private float m_Angle;

        private PhotonView m_PhotonView;

        private Vector3 m_Direction;
        private Vector3 m_NetworkPosition;
        private Vector3 m_StoredPosition;

        private Quaternion m_NetworkRotation;

        bool m_firstTake = false;

        public void Awake()
        {
            m_PhotonView = GetComponent<PhotonView>();

            m_StoredPosition = Vector3.zero;
            m_NetworkPosition = Vector3.zero;

            m_NetworkRotation = Quaternion.identity;
        }

        void OnEnable()
        {
            m_firstTake = true;
        }
        
        public void Update()
        {
            if (!this.m_PhotonView.IsMine && IsTangible)
            {
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, this.m_NetworkPosition, this.m_Distance * (1.0f / PhotonNetwork.SerializationRate));
                transform.localRotation = Quaternion.RotateTowards(transform.localRotation, this.m_NetworkRotation, this.m_Angle * (1.0f / PhotonNetwork.SerializationRate));
            }
        }
        
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (!IsTangible)
                return;
            
            if (stream.IsWriting)
            {

                var localTransform = PickupTarget.Instance.transform;
                var position = localTransform.InverseTransformPoint(transform.position);
                
                this.m_Direction = position - this.m_StoredPosition;
                this.m_StoredPosition = position;

                stream.SendNext(position);
                stream.SendNext(this.m_Direction);

                stream.SendNext(transform.rotation);

                stream.SendNext(transform.localScale);
            }
            else
            {
                this.m_NetworkPosition = (Vector3) stream.ReceiveNext();
                this.m_Direction = (Vector3) stream.ReceiveNext();

                if (m_firstTake)
                {
                    transform.localPosition = this.m_NetworkPosition;
                    this.m_Distance = 0f;
                }
                else
                {
                    float lag = Mathf.Abs((float) (PhotonNetwork.Time - info.SentServerTime));
                    this.m_NetworkPosition += this.m_Direction * lag;
                    this.m_Distance = Vector3.Distance(transform.localPosition, this.m_NetworkPosition);
                }


                this.m_NetworkRotation = (Quaternion) stream.ReceiveNext();

                if (m_firstTake)
                {
                    this.m_Angle = 0f;
                    transform.localRotation = this.m_NetworkRotation;
                }
                else
                {
                    this.m_Angle = Quaternion.Angle(transform.localRotation, this.m_NetworkRotation);
                }
            }


            transform.localScale = (Vector3) stream.ReceiveNext();

            if (m_firstTake)
            {
                m_firstTake = false;
            }
        }
    }
}