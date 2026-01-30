using System;
using System.Collections;
using UnityEngine;
[Serializable]
public class MaskData
{
    [Header("Config")]
    public float jumpHeight;
    public float wallDragSpeed;
    public float cooldownInterval;
    public float currentCooldown;
}
[CreateAssetMenu(fileName = "MaskData", menuName = "ScriptableObjects/MaskNormal")]
public class MaskNormal : MaskBase
{

    public override void ActiveSkill(GameObject parent)
    {
        maskData.currentCooldown = maskData.cooldownInterval;
        Debug.Log("MaskNormal ActiveSkill");
    }
}
[CreateAssetMenu(fileName = "MaskData", menuName = "ScriptableObjects/MaskDash")]
public class MaskDash : MaskBase
{
    [Header("Dash Config")]
    public float dashSpeed;
    public float dashTime;
    public float dashDistanceHardLimit;

    public override void ActiveSkill(GameObject parent)
    {
        maskData.currentCooldown = maskData.cooldownInterval;
        
        Rigidbody2D rb = parent.GetComponent<Rigidbody2D>();
        PlayerController pc = parent.GetComponent<PlayerController>(); 


        int direction = pc.GetFacingRight() ? 1 : -1; 

      
        pc.StartCoroutine(DashRoutine(rb, direction));
        
        Debug.Log("MaskDash ActiveSkill Started");
    }

    private IEnumerator DashRoutine(Rigidbody2D rb, int direction)
    {
        Vector2 startPos = rb.position;
        float timer = 0f;
        
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0;

        while (timer < dashTime)
        {
            float distanceTraveled = Vector2.Distance(startPos, rb.position);
            
            if (distanceTraveled >= dashDistanceHardLimit)
            {
                Debug.Log("Dash stopped by Distance Limit");
                break; 
            }

            rb.linearVelocity = new Vector2(dashSpeed * direction, 0);

            timer += Time.deltaTime;
            
            yield return null; 
        }

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = originalGravity; 
    }
}
[CreateAssetMenu(fileName = "MaskData", menuName = "ScriptableObjects/MaskHook")]
public class MaskHook : MaskBase
{
    [Header("Hook Config")]
    public float hookRange;

    public float hookSpeed;

    public override void ActiveSkill(GameObject parent)
    {
        maskData.currentCooldown = maskData.cooldownInterval;
        Debug.Log("MaskHook ActiveSkill");
    }
}
[CreateAssetMenu(fileName = "MaskData", menuName = "ScriptableObjects/MaskPlatform")]
public class MaskPlatform : MaskBase
{
    [Header("Platform Config")]
    public GameObject platformPrefab;

    public float timePlatformLast;

    public override void ActiveSkill(GameObject parent)
    {
        maskData.currentCooldown = maskData.cooldownInterval;
        PlayerController pc = parent.GetComponent<PlayerController>();
        GameObject platform = Instantiate(platformPrefab,pc.GetPlatformSpawnPos().position, platformPrefab.transform.rotation);
        Destroy(platform, timePlatformLast);
        Debug.Log("MaskPlatform ActiveSkill");
    }
}
