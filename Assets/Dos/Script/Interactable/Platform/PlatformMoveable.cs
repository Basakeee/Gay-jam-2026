using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformMoveable : MonoBehaviour
{
    [Header("Waypoints")]
    public List<Transform> Waypoints = new List<Transform>();
    
    [Header("Config")]
    public float speed = 2f;
    public float waitTime = 0.5f;

    private int currentWaypointIndex = 0;
    private bool isWaiting = false;

    private void Update()
    {
        if (Waypoints.Count == 0) return;

        if (!isWaiting)
        {
            MovePlatform();
        }
    }

    private void MovePlatform()
    {
        Transform target = Waypoints[currentWaypointIndex];
        
        transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target.position) < 0.01f)
        {
            StartCoroutine(NextWaypointRoutine());
        }
    }

    private IEnumerator NextWaypointRoutine()
    {
        isWaiting = true;
        
        yield return new WaitForSeconds(waitTime);

        currentWaypointIndex++;

        if (currentWaypointIndex >= Waypoints.Count)
        {
            currentWaypointIndex = 0;
        }

        isWaiting = false;
    }
}