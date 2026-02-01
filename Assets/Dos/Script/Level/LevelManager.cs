using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    // เก็บรายการของที่ต้องรีเซ็ต (Platform, Orbs)
    private List<IResettable> _resettables = new List<IResettable>();
    
    // เก็บจุดเกิดของแต่ละห้อง (Key = RoomIndex, Value = Transform)
    private Dictionary<int, Transform> _roomSpawnPoints = new Dictionary<int, Transform>();

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    // ฟังก์ชันให้ Object มาลงทะเบียนตัวเองตอน Start
    public void RegisterResettable(IResettable item)
    {
        if (!_resettables.Contains(item))
            _resettables.Add(item);
    }

    // ฟังก์ชันให้ Room มาลงทะเบียนจุดเกิด
    public void RegisterSpawnPoint(int roomIndex, Transform spawnPoint)
    {
        if (!_roomSpawnPoints.ContainsKey(roomIndex))
            _roomSpawnPoints.Add(roomIndex, spawnPoint);
    }

    public Transform GetSpawnPoint(int roomIndex)
    {
        if (_roomSpawnPoints.ContainsKey(roomIndex))
            return _roomSpawnPoints[roomIndex];
        return null;
    }

    // สั่งรีเซ็ตทุกอย่าง
    public void RespawnAllObjects()
    {
        foreach (var item in _resettables)
        {
            item.ResetState();
        }
    }
}