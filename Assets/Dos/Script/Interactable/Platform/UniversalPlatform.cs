using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UniversalPlatform : MonoBehaviour
{
    // เพิ่ม HookMove (ไปแล้วหยุด) และ HookMoveBack (ไปแล้วกลับ)
    public enum PlatformType { Static, Moving, Falling, HookMove, HookMoveBack }
    public enum SurfaceType { Normal, Sticky, Slippery, Weak }

    [Header("Main Settings")]
    public PlatformType movementType = PlatformType.Static;
    public SurfaceType surfaceType = SurfaceType.Normal;

    [Header("Movement Config")]
    public List<Transform> waypoints;
    public float moveSpeed = 3f;
    public float waitTime = 1f; // ใช้เป็นเวลารอก่อนกลับสำหรับ HookMoveBack ด้วย
    
    private int _currentWaypointIndex = 0;
    private bool _isWaiting = false;
    private Coroutine _activeHookCoroutine; // เก็บ Coroutine ไว้เช็คว่ากำลังโดนดึงอยู่ไหม

    [Header("Falling Config")]
    public float fallDelay = 0.5f;
    public float fallGravity = 3f;
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

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

        // ถ้าไม่ใช่ Falling ให้เป็น Kinematic ทั้งหมดเพื่อควบคุมการเคลื่อนที่เอง
        if (movementType != PlatformType.Falling)
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
        }
        else
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
        }
        UpdateLockVisuals();
    }

    private void Update()
    {
        // เฉพาะแบบ Moving ปกติเท่านั้นที่รันใน Update 
        // ส่วน HookMove / HookMoveBack จะทำงานผ่าน Coroutine เมื่อถูกเรียก
        if (isLocked) return;
        if (movementType == PlatformType.Moving && !_isWaiting)
        {
            PatrolMovePlatform();
        }
    }
    

    // ฟังก์ชันเดินวนสำหรับ Platform แบบ Moving ปกติ
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

    // ---------------------------------------------------------
    //  SECTION: HOOK LOGIC (ส่วนที่เพิ่ม/แก้ไข)
    // ---------------------------------------------------------

    public void ForceGoToWaypoint(int targetWaypointIndex)
    {
        if (waypoints == null || waypoints.Count == 0) return;
        if (isLocked) return;

        if (targetWaypointIndex < 0 || targetWaypointIndex >= waypoints.Count) return;

        // ถ้ามีการทำงานค้างอยู่ ให้หยุดก่อน (เพื่อเริ่มคำสั่งใหม่)
        if (_activeHookCoroutine != null) StopCoroutine(_activeHookCoroutine);

        Transform targetPoint = waypoints[targetWaypointIndex];

        // แยกการทำงานตาม Type
        if (movementType == PlatformType.HookMove)
        {
            // แบบที่ 1: ดึงแล้วไปหยุดที่นั่นเลย
            _activeHookCoroutine = StartCoroutine(HookMoveRoutine(targetPoint.position));
        }
        else if (movementType == PlatformType.HookMoveBack)
        {
            // แบบที่ 2: ดึงแล้วไป รอแปปนึง แล้วกลับมาที่เดิม
            _activeHookCoroutine = StartCoroutine(HookMoveBackRoutine(targetPoint.position));
        }
        else if (movementType == PlatformType.Moving || movementType == PlatformType.Static)
        {
            // กรณีพิเศษ: ถ้าดึง Platform ปกติ ก็ให้ขยับไปจุดนั้น (แล้วแต่จะดีไซน์)
            _activeHookCoroutine = StartCoroutine(HookMoveRoutine(targetPoint.position));
        }
    }

    // Logic แบบที่ 1: ไปแล้วหยุด
    private IEnumerator HookMoveRoutine(Vector3 targetPos)
    {
        while (Vector2.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPos, hookSpeed * Time.deltaTime);
            yield return null;
        }
        
        // ถึงที่หมายแล้วจบการทำงาน (หยุดนิ่ง)
        transform.position = targetPos;
        _activeHookCoroutine = null;
    }

    // Logic แบบที่ 2: ไป -> รอ -> กลับ
    private IEnumerator HookMoveBackRoutine(Vector3 targetPos)
    {
        Vector3 startPos = transform.position; // จำจุดเริ่มต้นไว้

        // 1. เคลื่อนที่ไปหาเป้าหมาย
        while (Vector2.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPos, hookSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos; // Snap ตำแหน่ง

        // 2. รอเวลา (ใช้ waitTime ที่ตั้งไว้)
        yield return new WaitForSeconds(waitTime);

        // 3. เคลื่อนที่กลับจุดเริ่มต้น
        while (Vector2.Distance(transform.position, startPos) > 0.01f)
        {
            transform.position = Vector2.MoveTowards(transform.position, startPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = startPos; // Snap ตำแหน่ง

        _activeHookCoroutine = null;
    }

    // ---------------------------------------------------------

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // ให้เกาะได้หมด ยกเว้น Falling ที่กำลังร่วง
            if (movementType != PlatformType.Falling || !_isFalling)
            {
                collision.transform.SetParent(this.transform);
            }

            HandleSurfaceEnter(collision.gameObject);

            if (surfaceType == SurfaceType.Weak)
            {
                StartCoroutine(WeakPlatformRoutine());
            }

            if (movementType == PlatformType.Falling && !_isFalling)
            {
                StartCoroutine(FallRoutine());
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

    private void HandleSurfaceEnter(GameObject player)
    {
        _playerController = player.GetComponent<PlayerController>();

        if (surfaceType == SurfaceType.Sticky)
        {
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

        _col.enabled = false;
        GetComponent<SpriteRenderer>().enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        _col.enabled = true;
        GetComponent<SpriteRenderer>().enabled = true;
    }

    private IEnumerator FallRoutine()
    {
        _isFalling = true;
        yield return new WaitForSeconds(fallDelay);

        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.gravityScale = fallGravity;
    }

    public void ResetPlatform()
    {
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0;
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;
        _isFalling = false;

        _col.enabled = true;
        GetComponent<SpriteRenderer>().enabled = true;
        
        // หยุด Hook Coroutine ด้วยถ้ามีการรีเซ็ต
        if (_activeHookCoroutine != null) StopCoroutine(_activeHookCoroutine);
    }
    public void Unlock()
    {
        if (isLocked)
        {
            isLocked = false;
            UpdateLockVisuals();
            Debug.Log("Platform Unlocked!");
        }
    }
    private void UpdateLockVisuals()
    {
        if (lockVisuals != null)
        {
            lockVisuals.SetActive(isLocked); // ถ้า Lock=เปิดภาพ, ไม่ Lock=ปิดภาพ
        }
    }

    private void OnDrawGizmos()
    {
        // วาด Gizmos สำหรับทุกแบบที่มี Waypoint
        if (waypoints != null && waypoints.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] != null)
                {
                    Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);
                    
                    // วาดเส้นเชื่อมเฉพาะแบบ Moving
                    if (movementType == PlatformType.Moving)
                    {
                        if (i < waypoints.Count - 1 && waypoints[i + 1] != null)
                            Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                    }
                }
            }
            
            // เส้นปิด Loop เฉพาะแบบ Moving
            if (movementType == PlatformType.Moving && waypoints[0] != null && waypoints[waypoints.Count - 1] != null)
                Gizmos.DrawLine(waypoints[waypoints.Count - 1].position, waypoints[0].position);
        }
    }
}