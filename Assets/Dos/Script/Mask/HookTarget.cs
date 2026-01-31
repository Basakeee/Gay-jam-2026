using UnityEngine;

public class HookTarget : MonoBehaviour
{
    public enum HookType { PullPlayerToPoint, PullObjectToPlayer }
    public HookType hookType;

    [Header("For Pull Object (Platform)")]
    public UniversalPlatform linkedPlatform; 
    public int targetWaypointIndex; // <--- เพิ่มตรงนี้: จะให้ Platform วิ่งไปจุดไหนเมื่อโดนดึง

    public void OnHooked(GameObject player)
    {
        if (hookType == HookType.PullObjectToPlayer)
        {
            if (linkedPlatform != null)
            {
                // สั่ง Force Waypoint
                linkedPlatform.ForceGoToWaypoint(targetWaypointIndex);
            }
        }
    }
}