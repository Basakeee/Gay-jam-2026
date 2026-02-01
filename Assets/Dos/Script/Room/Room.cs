using Unity.Cinemachine;
using UnityEngine;

public class Room : MonoBehaviour
{
    public int roomIndex;
    public CinemachineCamera roomCamera;
    
    // [เพิ่ม] จุดเกิดของห้องนี้ (ลาก Game Object ว่างที่วางตำแหน่งเกิดมาใส่)
    public Transform spawnPoint; 

    private void Start()
    {
        CinemachineSwitcher.instance.AddCamera(roomIndex, roomCamera);
        
        // [เพิ่ม] บอก LevelManager ว่าห้องนี้เกิดตรงไหน
        if (spawnPoint != null)
        {
            LevelManager.instance.RegisterSpawnPoint(roomIndex, spawnPoint);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ... (โค้ดเดิม) ...
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            // ...
            CinemachineSwitcher.instance.CameraTransition(player.CurrentRoomIndex, roomIndex);
            player.CurrentRoomIndex = roomIndex;
            player.OnTrasitionCamera();
        }
    }
}