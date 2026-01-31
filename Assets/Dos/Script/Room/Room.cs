using System;
using Unity.Cinemachine;
using UnityEngine;

public class Room : MonoBehaviour
{
    public int roomIndex;
    public CinemachineCamera roomCamera;

    private void Start()
    {
        CinemachineSwitcher.instance.AddCamera(roomIndex,roomCamera);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            Debug.Log($"Player Enter the Room {roomIndex}");
            CinemachineSwitcher.instance.CameraTransition(player.CurrentRoomIndex, roomIndex);
            player.CurrentRoomIndex = roomIndex;
            player.OnTrasitionCamera();
        }
    }
}
