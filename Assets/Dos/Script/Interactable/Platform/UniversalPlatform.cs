using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class UniversalPlatform : MonoBehaviour , IResettable
{
    // เพิ่ม FallingWaypoint ใน Enum
    public enum PlatformType { Static, Moving, Falling, FallingWaypoint, HookMove, HookMoveBack }
    public enum SurfaceType { Normal, Sticky, Slippery, Weak }

    [Header("Main Settings")]
    public PlatformType movementType = PlatformType.Static;
    public SurfaceType surfaceType = SurfaceType.Normal;

    [Header("Movement Config")]
    public List<Transform> waypoints;
    public float moveSpeed = 3f;
    public float waitTime = 1f;
    
    private int _currentWaypointIndex = 0;
    private bool _isWaiting = false;
    private Coroutine _activeHookCoroutine;

    [Header("Falling Config")]
    public float fallDelay = 0.5f;
    public float fallGravity = 3f; // สำหรับ Falling ปกติ (Dynamic)
    public float fallSpeed = 10f;  // [เพิ่ม] ความเร็วในการร่วงสำหรับ FallingWaypoint
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private bool _hasFallenToWaypoint = false; // [เพิ่ม] เช็คว่าร่วงไปถึงจุดหรือยัง

    [Header("Weak Config")]
    public float destroyDelay = 0.5f;
    public float respawnDelay = 3f;

    [Header("Surface Config")]
    [Range(0.1f, 1f)] public float stickySpeedMultiplier = 0.5f;
    public PhysicsMaterial2D slipperyMaterial;
    public PhysicsMaterial2D stickyMaterial;

    [Header("Hook Config")] public float hookSpeed = 6f;
    
    [Header("Lock Config")]
    public bool isLocked = false;
    public GameObject lockVisuals;
    
    [Header("SFX Config")]
    public AudioClip stickySound; // เสียงตอนเหยียบพื้นหนืด
    public AudioClip weakBreakSound; // เสียงตอนพื้นพัง
    public AudioClip dropSound; // เสียงตอนเริ่มร่วง

    private Rigidbody2D _rb;
    private Collider2D _col;
    private bool _isFalling = false;
    private PlayerController _playerController;
    private float _originalPlayerSpeed;

    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
        if (LevelManager.instance != null) LevelManager.instance.RegisterResettable(this);

        // ถ้าเป็น Falling ปกติ ให้เป็น Kinematic ก่อน (รอเหยียบค่อย Dynamic)
        // ถ้าเป็น FallingWaypoint ให้เป็น Kinematic ตลอดไป (เราคุมตำแหน่งเอง)
        _rb.bodyType = RigidbodyType2D.Kinematic;
        
        UpdateLockVisuals();
    }

    private void Update()
    {
        if (isLocked) return;
        if (movementType == PlatformType.Moving && !_isWaiting)
        {
            PatrolMovePlatform();
        }
    }
    
    private void PatrolMovePlatform()
    {
        if (waypoints.Count == 0) return;

        Transform target = waypoints[_currentWaypointIndex];
        transform.position = Vector2.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target.position) < 0.01f)
        {
            StartCoroutine(NextWaypointRoutine());
        }
    }

    private IEnumerator NextWaypointRoutine()
    {
        _isWaiting = true;
        yield return new WaitForSeconds(waitTime);
        _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Count;
        _isWaiting = false;
    }

    public void ForceGoToWaypoint(int targetWaypointIndex)
    {
        if (waypoints == null || waypoints.Count == 0) return;
        if (isLocked) return;
        if (targetWaypointIndex < 0 || targetWaypointIndex >= waypoints.Count) return;

        if (_activeHookCoroutine != null) StopCoroutine(_activeHookCoroutine);

        Transform targetPoint = waypoints[targetWaypointIndex];

        if (movementType == PlatformType.HookMove)
        {
            _activeHookCoroutine = StartCoroutine(HookMoveRoutine(targetPoint.position));
        }
        else if (movementType == PlatformType.HookMoveBack)
        {
            _activeHookCoroutine = StartCoroutine(HookMoveBackRoutine(targetPoint.position));
        }
        else if (movementType == PlatformType.Moving || movementType == PlatformType.Static)
        {
            _activeHookCoroutine = StartCoroutine(HookMoveRoutine(targetPoint.position));
        }
    }

    private IEnumerator HookMoveRoutine(Vector3 targetPos)
    {
        while (Vector2.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPos, hookSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;
        _activeHookCoroutine = null;
    }

    private IEnumerator HookMoveBackRoutine(Vector3 targetPos)
    {
        Vector3 startPos = transform.position;
        while (Vector2.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPos, hookSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos; 

        yield return new WaitForSeconds(waitTime);

        while (Vector2.Distance(transform.position, startPos) > 0.01f)
        {
            transform.position = Vector2.MoveTowards(transform.position, startPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = startPos; 

        _activeHookCoroutine = null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // ให้เกาะได้หมด ยกเว้น Falling (Dynamic) ที่กำลังร่วง
            // FallingWaypoint เป็น Kinematic ดังนั้นเกาะได้ตลอดไม่มีปัญหา
            if (movementType != PlatformType.Falling || !_isFalling)
            {
                collision.transform.SetParent(this.transform);
            }

            HandleSurfaceEnter(collision.gameObject);

            if (surfaceType == SurfaceType.Weak)
            {
                StartCoroutine(WeakPlatformRoutine());
            }

            // 1. Falling แบบเดิม (Physics)
            if (movementType == PlatformType.Falling && !_isFalling)
            {
                StartCoroutine(FallRoutine());
            }
            // 2. [เพิ่มใหม่] Falling แบบ Waypoint (Kinematic)
            else if (movementType == PlatformType.FallingWaypoint && !_isFalling && !_hasFallenToWaypoint)
            {
                StartCoroutine(FallToWaypointRoutine());
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
            HandleSurfaceExit(collision.gameObject);
        }
    }

    // --- Logic การร่วงแบบ Waypoint ---
    private IEnumerator FallToWaypointRoutine()
    {
        _isFalling = true; // กันไม่ให้เรียกซ้ำระหว่างร่วง
        yield return new WaitForSeconds(fallDelay); // รอเวลาก่อนร่วง

        // ร่วงไปหา Waypoint แรก (Index 0)
        if (waypoints != null && waypoints.Count > 0 && waypoints[0] != null)
        {
            Vector3 target = waypoints[0].position;
            
            // วนลูปขยับลงไปหา
            while (Vector2.Distance(transform.position, target) > 0.01f)
            {
                // ใช้ fallSpeed ที่ตั้งแยกมา หรือจะใช้ moveSpeed ก็ได้
                transform.position = Vector2.MoveTowards(transform.position, target, fallSpeed * Time.deltaTime);
                yield return null;
            }
            
            // ถึงเป้าหมายแล้ว Snap ตำแหน่ง
            transform.position = target;
        }

        _isFalling = false;
        _hasFallenToWaypoint = true; // ล็อกไว้ ไม่ให้ร่วงซ้ำอีก
    }

    // --- Helper Functions ---
    private void HandleSurfaceEnter(GameObject player)
    {
        _playerController = player.GetComponent<PlayerController>();

        if (surfaceType == SurfaceType.Sticky)
        {
            if(stickySound != null) AudioManager.instance.PlayOneShotSFX(stickySound);
            if (_playerController != null)
            {
                _originalPlayerSpeed = _playerController.speed;
                _playerController.speed *= stickySpeedMultiplier;
            }
            if (stickyMaterial != null) _rb.sharedMaterial = stickyMaterial;
        }
        else if (surfaceType == SurfaceType.Slippery)
        {
            if (_playerController != null)
            {
                _playerController.isOnIce = true;
            }
            if (slipperyMaterial != null) _rb.sharedMaterial = slipperyMaterial;
        }
    }

    private void HandleSurfaceExit(GameObject player)
    {
        if (surfaceType == SurfaceType.Sticky && _playerController != null)
        {
            _playerController.speed = _originalPlayerSpeed;
        }

        if (surfaceType == SurfaceType.Slippery && _playerController != null)
        {
            _playerController.isOnIce = false;
        }

        _rb.sharedMaterial = null;
    }

    private IEnumerator WeakPlatformRoutine()
    {
        yield return new WaitForSeconds(destroyDelay);

        if (weakBreakSound != null) AudioManager.instance.PlayOneShotSFX(weakBreakSound);
        
        _col.enabled = false;
        GetComponent<SpriteRenderer>().enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        _col.enabled = true;
        GetComponent<SpriteRenderer>().enabled = true;
    }

    private IEnumerator FallRoutine()
    {
        if (dropSound != null) AudioManager.instance.PlayOneShotSFX(dropSound);
        
        _isFalling = true;
        yield return new WaitForSeconds(fallDelay);

        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.gravityScale = fallGravity;
    }
    public void ResetState()
    {
        ResetPlatform(); // เรียกฟังก์ชันเดิมที่คุณมีอยู่แล้ว
        
        // เพิ่มเติม: ถ้าเป็น Locked Platform ต้องกลับไป Lock เหมือนเดิมไหม?
        // if (lockVisuals != null) isLocked = true; UpdateLockVisuals(); // ถ้าต้องการ
    }
    public void ResetPlatform()
    {
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0;
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;
        _isFalling = false;
        _hasFallenToWaypoint = false; // รีเซ็ตสถานะร่วง

        _col.enabled = true;
        if(gameObject.TryGetComponent<SpriteRenderer>(out var sr))
            sr.enabled = true;
        else if (gameObject.TryGetComponent<SpriteShapeRenderer>(out var ssr))
            ssr.enabled = true;
        
        if (_activeHookCoroutine != null) StopCoroutine(_activeHookCoroutine);
        
        UpdateLockVisuals();
    }

    public void Unlock()
    {
        if (isLocked)
        {
            isLocked = false;
            UpdateLockVisuals();
        }
    }
    
    private void UpdateLockVisuals()
    {
        if (lockVisuals != null)
        {
            lockVisuals.SetActive(isLocked);
        }
    }

    private void OnDrawGizmos()
    {
        if (waypoints != null && waypoints.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] != null)
                {
                    Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);
                    
                    if (movementType == PlatformType.Moving)
                    {
                        if (i < waypoints.Count - 1 && waypoints[i + 1] != null)
                            Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                    }
                    // วาดเส้นสำหรับ FallingWaypoint ด้วย จะได้เห็นว่าจะร่วงไปไหน
                    else if (movementType == PlatformType.FallingWaypoint && i == 0)
                    {
                         Gizmos.color = Color.red;
                         Gizmos.DrawLine(transform.position, waypoints[0].position);
                    }
                }
            }
            
            if (movementType == PlatformType.Moving && waypoints[0] != null && waypoints[waypoints.Count - 1] != null)
                Gizmos.DrawLine(waypoints[waypoints.Count - 1].position, waypoints[0].position);
        }
    }
}