using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CinemachineSwitcher : MonoBehaviour
{
    public static CinemachineSwitcher instance;
    public CinemachineCamera startCamera;
    private Dictionary<int,CinemachineCamera> vCams = new Dictionary<int, CinemachineCamera>();
    private void Awake()
    {
        if (instance == null)
            instance = this;
        vCams.Add(0,startCamera);
    }

    public void AddCamera(int roomIndex,CinemachineCamera cam)
    {
        vCams.Add(roomIndex, cam);
    }
    public void CameraTransition(int startIndex, int endIndex)
    {
        vCams[startIndex].Priority = 0;
        vCams[endIndex].Priority = 1;
    }
}
