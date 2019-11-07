using System;
using System.Collections;
using System.Collections.Generic;
using Augmentix.Scripts.AR;
using Augmentix.Scripts.AR.UI;
#if UNITY_EDITOR
using Augmentix.Scripts.OOI.Editor;
#endif
using Augmentix.Scripts.VR;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

#if UNITY_ANDROID
using Vuforia;
#endif

namespace Augmentix.Scripts.OOI
{
    [RequireComponent(typeof(PhotonView))]
    public class OOI : MonoBehaviourPunCallbacks
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
            Changeable = 64,
            Lockable = 128
        }

#if UNITY_EDITOR
        [OOIViewEditor.EnumFlagsAttribute]
#endif
        public InteractionFlag Flags = InteractionFlag.Highlight | InteractionFlag.Lockable;

        [TextArea] public string Text;

        public float TextScaleMax = 10f;
        public float TextScaleMin = 0.5f;

        private LineRenderer lineRenderer;
        private List<MeshCollider> _convexCollider = new List<MeshCollider>();
        
#if UNITY_ANDROID
        private TrackableBehaviour _trackable;
#endif
        private void Start()
        {
            GenerateConvexMeshCollider();
            
            GetComponent<PhotonView>().OwnershipTransfer = OwnershipOption.Takeover;

            if (TargetManager.Instance.Type == TargetManager.PlayerType.Primary)
            {
#if UNITY_ANDROID
                _trackable = PickupTarget.Instance.GetComponent<TrackableBehaviour>();
#endif

                lineRenderer = gameObject.AddComponent<LineRenderer>();
                lineRenderer.widthMultiplier = 0.001f;
                lineRenderer.material = OOIUI.Instance.HeightLineMaterial;


                PickupTarget.Instance.GotPlayer += player =>
                {
                    var treveris = Treveris.GetTreverisByPlayer(player.GetComponent<PhotonView>().Owner);
                    if (transform.IsChildOf(treveris.transform))
                        GetComponent<PhotonView>().TransferOwnership(PhotonNetwork.LocalPlayer);
                };

                PickupTarget.Instance.LostPlayer += player =>
                {
                    var treveris = Treveris.GetTreverisByPlayer(player.GetComponent<PhotonView>().Owner);
                    if (transform.IsChildOf(treveris.transform))
                        GetComponent<PhotonView>().TransferOwnership(player.GetComponent<PhotonView>().Owner);
                };
            }
        }

        private void Update()
        {
            if (lineRenderer == null)
                return;

#if UNITY_ANDROID
            if (_trackable.CurrentStatus == TrackableBehaviour.Status.NO_POSE)
                return;
            
            
            var pos = _trackable.transform.InverseTransformPoint(transform.position);
            if (pos.y > OOIUI.Instance.HeightLineThreshold)
            {
                pos.y = 0f;
                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0,transform.position - new Vector3(0f,0.001f,0f));
                lineRenderer.SetPosition(1,_trackable.transform.TransformPoint(pos));
            }
            else
            {
                lineRenderer.enabled = false;
            }
#endif
        }

        private GameObject _videoCube = null;
        private GameObject _textCube = null;

        [PunRPC]
        public void Interact(InteractionFlag flag)
        {
            var view = GetComponent<PhotonView>();
            if (view.IsMine && PickupTarget.Instance && PickupTarget.Instance.Current)
                view.RPC("Interact", PickupTarget.Instance.Current.GetComponent<PhotonView>().Owner, flag);

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

        private Coroutine _moveText;

        private void ToggleText()
        {
            GameObject player = FindObjectOfType<PlayerSynchronizer>().gameObject;

#if UNITY_STANDALONE
            player = player.transform.parent.parent.gameObject;
#endif

            if (_textCube == null || !_textCube.gameObject.activeSelf)
            {
                if (_textCube == null)
                {
                    _textCube = Instantiate(TargetManager.Instance.OOITextPrefab);
                    _textCube.transform.parent = transform;
                    _textCube.GetComponent<TextMeshPro>().text = Text;
                }

                _textCube.SetActive(true);
                _moveText = StartCoroutine(MoveText());

                IEnumerator MoveText()
                {
                    while (true)
                    {
                        var isInside = false;
                        Vector3 nearestPoint = transform.position;

                        foreach (var meshCollider in _convexCollider)
                        {
                            if (meshCollider.gameObject != _textCube)
                            {
                                var closest = meshCollider.ClosestPoint(player.transform.position);
                                if (Vector3.Distance(player.transform.position, closest) <
                                    Vector3.Distance(player.transform.position, nearestPoint))
                                {
                                    nearestPoint = closest;
                                }
                            }
                        }

                        
                        nearestPoint.y = player.transform.position.y;

                        var direction = nearestPoint - player.transform.position;

                        var newposition = Vector3.zero;
                        
                        if (direction.sqrMagnitude < 1)
                        {
                            newposition = player.transform.position + player.transform.forward;
                        }
                        else
                        {
                            newposition = player.transform.position + (nearestPoint - player.transform.position).normalized;
                        }

                        _textCube.transform.position = Vector3.Lerp(_textCube.transform.position, newposition, 0.2f);
                        _textCube.transform.LookAt(player.transform);
                        _textCube.transform.Rotate(Vector3.up, 180);

                        yield return new WaitForEndOfFrame();
                    }
                }
            }
            else
            {
                if (_moveText != null)
                    StopCoroutine(_moveText);
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
                    //_videoCube.GetComponent<Renderer>().material = player.GetComponentInChildren<Renderer>().material;
                    //_videoCube.GetComponent<Renderer>().material.shader = Shader.Find("Standard");

                    video.targetMaterialRenderer = _videoCube.GetComponent<Renderer>();
                    //video.targetMaterialProperty = "_BaseMap";
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


        private void GenerateConvexMeshCollider()
        {
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

            if (meshFilters.Length == 0)
                return;

            List<CombineInstance> combine = new List<CombineInstance>();


            Mesh prevMesh = null;
            if (GetComponent<MeshFilter>())
                prevMesh = GetComponent<MeshFilter>().sharedMesh;
            
            var count = 0;
            
            var filter = GetComponent<MeshFilter>();
            if (filter == null)
                filter = gameObject.AddComponent<MeshFilter>();

            for (int i = 0; i < meshFilters.Length; i++)
            {
                var c = new CombineInstance();
                c.mesh = meshFilters[i].sharedMesh;
                c.transform = transform.worldToLocalMatrix *
                              meshFilters[i].transform.localToWorldMatrix;
                combine.Add(c);

                count += meshFilters[i].sharedMesh.vertexCount;

                if (i + 1 == meshFilters.Length || meshFilters[i + 1].sharedMesh.vertexCount + count > 65536)
                {
                    
                    filter.sharedMesh = new Mesh();
                    filter.sharedMesh
                        .CombineMeshes(combine.ToArray());

                    
                    var collider = gameObject.AddComponent<MeshCollider>();
                    collider.convex = true;
                    collider.isTrigger = true;
                    _convexCollider.Add(collider);

                    combine.Clear();
                    count = 0;
                }
            }

            if (prevMesh != null)
                GetComponent<MeshFilter>().sharedMesh = prevMesh;
            else
                DestroyImmediate(GetComponent<MeshFilter>());
        }
    }
}