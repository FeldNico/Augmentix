﻿using System.Collections;
using Augmentix.Scripts.AR;
using Augmentix.Scripts.AR.UI;
using Augmentix.Scripts.VR;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.PUN;
using UnityEngine;
using UnityEngine.UI;
using Vuforia;
using Recorder = Photon.Voice.Unity.Recorder;

namespace Augmentix.Scripts
{
    public class VideoStream : MonoBehaviour, IOnEventCallback
    {
        public const byte CHANGEORIENTATION = 4;
        public const byte TOOGLESTREAM = 5;
        public const byte SENDIMAGE = 6;

        public bool IsStreaming { private set; get; }
        public bool IsVideoEnabled { private set; get; } = true;
        private bool _isPrimary;
        private WebCamTexture _webCam;
        private Texture2D _tex2d;
        private GameObject _voiceStreamer;

        void Start()
        {
            _isPrimary = TargetManager.Instance.Type == TargetManager.PlayerType.Primary;
            _tex2d = new Texture2D(1, 1);

            TargetManager.Instance.OnConnection += () =>
            {
                GetComponent<Recorder>().Init(GetComponent<PhotonVoiceNetwork>().VoiceClient);
                GetComponent<Recorder>().InterestGroup = (byte) PhotonNetwork.LocalPlayer.ActorNumber;
                _voiceStreamer = PhotonNetwork.Instantiate("VoiceStreamer", Vector3.one, Quaternion.identity);
            };

            if (_isPrimary)
            {
#if !UNITY_EDITOR && UNITY_ANDROID
                var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
                var context = activity.Call<AndroidJavaObject>("getApplicationContext");
                var packageManager = context.Call<AndroidJavaObject>("getPackageManager");
                if (packageManager.Call<bool>("hasSystemFeature", "android.hardware.camera") &&
                    packageManager.Call<bool>("hasSystemFeature", "android.hardware.camera.front"))
                {
                    _webCam = new WebCamTexture();
                    foreach (var device in WebCamTexture.devices)
                    {
                        if (device.isFrontFacing)
                            _webCam = new WebCamTexture(device.name, 640, 480);
                    }

                    _webCam.Play();

                    IsVideoEnabled = true;

                    if (!CameraDevice.Instance.IsActive())
                    {
                        _webCam.Stop();
                        IsVideoEnabled = false;
                        ARUI.Instance.StreamToggle.GetComponentInChildren<Text>().text = "Audio Stream";


                        activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                        {
                            AndroidJavaClass Toast = new AndroidJavaClass("android.widget.Toast");
                            AndroidJavaObject javaString = new AndroidJavaObject("java.lang.String",
                                "This device does not support the usage of multiple cameras.\n Videostream is disabled.");
                            AndroidJavaObject toast = Toast.CallStatic<AndroidJavaObject>("makeText", context,
                                javaString, Toast.GetStatic<int>("LENGTH_LONG"));
                            toast.Call("show");
                        }));
                    }
                    else
                    {
                        IsVideoEnabled = true;
                    }
                }
                else
                {
                    IsVideoEnabled = false;
                    ARUI.Instance.StreamToggle.GetComponentInChildren<Text>().text = "Audio Stream";
                    
                    activity.Call("runOnUiThread",new AndroidJavaRunnable(() =>
                    {
                        AndroidJavaClass Toast = new AndroidJavaClass("android.widget.Toast");
                        AndroidJavaObject javaString = new AndroidJavaObject("java.lang.String", "This device does not support the usage of multiple cameras.\n Videostream is disabled.");
                        AndroidJavaObject toast = Toast.CallStatic<AndroidJavaObject>("makeText", context, javaString, Toast.GetStatic<int>("LENGTH_LONG"));
                        toast.Call("show");
                    }));
                }
#endif

                if (IsVideoEnabled)
                    InvokeRepeating("SendImage", 0.3f, 0.3f);

                ARUI.Instance.StreamToggle.onValueChanged.AddListener(toggle => { ToogleStream(toggle); });

                PickupTarget.Instance.GotPlayer += (player) =>
                {
                    PhotonNetwork.RaiseEvent(CHANGEORIENTATION, Input.deviceOrientation,
                        new RaiseEventOptions
                        {
                            TargetActors = new[]
                                {PickupTarget.Instance.PlayerSync.GetComponent<PhotonView>().Controller.ActorNumber}
                        }, SendOptions.SendReliable);
                };

                PickupTarget.Instance.LostPlayer += (player) =>
                {
                    if (IsStreaming)
                        ARUI.Instance.StreamToggle.isOn = false;
                };
            }
            else
            {
                FindObjectOfType<PlayerSynchronizer>().OnLost += player =>
                {
                    var recorder = GetComponent<Recorder>();
                    recorder.StopRecording();
                    GetComponent<PhotonVoiceNetwork>().Client.OpChangeGroups(new[] {(byte)player.ActorNumber}, null);
                    recorder.InterestGroup = (byte) PhotonNetwork.LocalPlayer.ActorNumber;
                    VRUI.Instance.VideoImage.gameObject.SetActive(false);
                    IsStreaming = false;
                };
            }
        }

        private DeviceOrientation _prevOrientation = DeviceOrientation.Unknown;

        void Update()
        {
            if (_isPrimary)
            {
                if (_prevOrientation == DeviceOrientation.Unknown)
                    _prevOrientation = Input.deviceOrientation;

                if (Input.deviceOrientation != _prevOrientation)
                {
                    _prevOrientation = Input.deviceOrientation;
                    if (PickupTarget.Instance != null && PickupTarget.Instance.PlayerSync != null)
                        PhotonNetwork.RaiseEvent(CHANGEORIENTATION, _prevOrientation,
                            new RaiseEventOptions
                            {
                                TargetActors = new[]
                                    {PickupTarget.Instance.PlayerSync.GetComponent<PhotonView>().Controller.ActorNumber}
                            }, SendOptions.SendReliable);
                }
            }
        }

        private void SendImage()
        {
            if (!_isPrimary || !IsStreaming || PickupTarget.Instance == null ||
                PickupTarget.Instance.PlayerSync == null || !_webCam.isPlaying || _webCam.width < 100)
                return;

            if (_tex2d.width == 1)
                _tex2d = new Texture2D(_webCam.width, _webCam.height);

            _tex2d.SetPixels(_webCam.GetPixels());
            _tex2d.Apply();

            var options = new RaiseEventOptions
            {
                CachingOption = EventCaching.DoNotCache, Receivers = ReceiverGroup.Others,
                TargetActors = new[]
                    {PickupTarget.Instance.PlayerSync.GetComponent<PhotonView>().Controller.ActorNumber}
            };

            PhotonNetwork.RaiseEvent(SENDIMAGE, _tex2d.EncodeToJPG(10), options, SendOptions.SendUnreliable);
        }

        public void OnEvent(EventData photonEvent)
        {
            if (_isPrimary)
                return;

            switch (photonEvent.Code)
            {
                case CHANGEORIENTATION:
                {
                    if (VRUI.Instance == null)
                        return;

                    var image = VRUI.Instance.VideoImage;
                    switch ((DeviceOrientation) photonEvent.CustomData)
                    {
                        case DeviceOrientation.Portrait:
                        {
                            image.transform.localRotation = Quaternion.Euler(0, 0, 90);
                            break;
                        }
                        case DeviceOrientation.PortraitUpsideDown:
                        {
                            image.transform.localRotation = Quaternion.Euler(0, 0, 270);
                            break;
                        }
                        case DeviceOrientation.LandscapeLeft:
                        {
                            image.transform.localRotation = Quaternion.Euler(0, 0, 0);
                            break;
                        }
                        case DeviceOrientation.LandscapeRight:
                        {
                            image.transform.localRotation = Quaternion.Euler(0, 0, 180);
                            break;
                        }
                    }

                    break;
                }

                case SENDIMAGE:
                {
                    var pixels = (byte[]) photonEvent.CustomData;

                    if (pixels != null && pixels.Length > 0)
                    {
                        if (!VRUI.Instance.VideoImage.gameObject.activeSelf)
                            VRUI.Instance.VideoImage.gameObject.SetActive(true);

                        _tex2d.LoadImage(pixels);
                        VRUI.Instance.VideoImage.sprite =
                            Sprite.Create(_tex2d, new Rect(0, 0, 640, 480), new Vector2(0.5f, 0.5f));
                    }

                    break;
                }

                case TOOGLESTREAM:
                {
                    var group = (byte) photonEvent.CustomData;
                    var recorder = GetComponent<Recorder>();
                    if (!IsStreaming)
                    {
                        GetComponent<PhotonVoiceNetwork>().Client.OpChangeGroups(null, new[] {group});
                        recorder.InterestGroup = group;
                        recorder.StartRecording();
                        VRUI.Instance.VideoImage.gameObject.SetActive(true);
                        IsStreaming = true;
                    }
                    else
                    {
                        recorder.StopRecording();
                        GetComponent<PhotonVoiceNetwork>().Client.OpChangeGroups(new[] {group}, null);
                        recorder.InterestGroup = (byte) PhotonNetwork.LocalPlayer.ActorNumber;
                        VRUI.Instance.VideoImage.gameObject.SetActive(false);
                        IsStreaming = false;
                    }

                    break;
                }
            }
        }

        public void ToogleStream(bool toggle)
        {
            if (PickupTarget.Instance == null || PickupTarget.Instance.PlayerSync == null || !_isPrimary)
                return;

            GetComponent<PhotonVoiceNetwork>().Client
                .OpChangeGroups(null, new[] {(byte) PhotonNetwork.LocalPlayer.ActorNumber});

            if (!IsStreaming && toggle)
            {
                GetComponent<Recorder>().StartRecording();

                PhotonNetwork.RaiseEvent(TOOGLESTREAM, (byte) PhotonNetwork.LocalPlayer.ActorNumber,
                    new RaiseEventOptions
                    {
                        TargetActors = new[]
                            {PickupTarget.Instance.PlayerSync.GetComponent<PhotonView>().Controller.ActorNumber}
                    }, SendOptions.SendReliable);
                IsStreaming = true;
            }
            
            
            if (IsStreaming && !toggle)
            {
                GetComponent<Recorder>().StopRecording();

                PhotonNetwork.RaiseEvent(TOOGLESTREAM, (byte) PhotonNetwork.LocalPlayer.ActorNumber,
                    new RaiseEventOptions
                    {
                        TargetActors = new[]
                            {PickupTarget.Instance.PlayerSync.GetComponent<PhotonView>().Controller.ActorNumber}
                    }, SendOptions.SendReliable);
                IsStreaming = false;
            }
        }

        public void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }

        public void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }
    }
}