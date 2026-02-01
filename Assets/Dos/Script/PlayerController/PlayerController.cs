using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    // ... (Header และตัวแปรเดิม คงไว้เหมือนเดิม) ...
    [Header("Mask")]
    public List<MaskBase> allMask;
    public MaskType currentMaskType;
    
    [Header("Animation Components")]
    private Animator _anim;
    private CapsuleCollider2D _col;

    [Header("Player Movement Config")]
    public float speed;
    public float jumpCheckRange = 0.6f;
    [HideInInspector] public bool isOnIce = false;
    
    [Header("Wall Config")]
    public float wallCheckDistance = 0.6f; 
    public LayerMask wallMask; 

    public float jumpCutMultipier = 0.5f;
    public LayerMask groundMask;

    [Header("Player Mask Config")]
    public float cooldownInterval = 1f;
    private float _currentSwapCooldown;
    public float freezeGravityTime = 0.25f;
    private float _currentFreezeGravityTime;
    
    [Header("Dash Config")]
    public LayerMask dashLayerMask; // อย่าลืมตั้ง Layer นี้ให้ตรงกับ Object ที่ทำลายได้
    public Vector2 knockbackForce;
    
    [Header("Hook Config")]
    public LineRenderer hookLineRenderer; // ลาก Component LineRenderer มาใส่
    public LayerMask hookLayer; // Layer ของ HookTarget
    public LayerMask obstacleLayer;       // Layer ของกำแพง/สิ่งกีดขวาง (Ground, Wall)
    public GameObject targetIndicator;    // Sprite เป้าเล็งที่จะไปโผล่บน HookTarget
    public LineRenderer rangeIndicator;   // เส้นวงกลมบอกระยะ (LineRenderer แบบ Loop)
    [Range(0, 50)] public int rangeCircleSegments = 50; // ความละเอียดวงกลม
    public float indicatorShowBuffer = 5f;
    private HookTarget _currentValidTarget; // เก็บเป้าหมายที่เล็งได้ปัจจุบัน

    [Header("Wall Jump Config")]
    public Vector2 wallJumpForce = new Vector2(10f, 15f); // แรงกระโดด (X = ถีบออก, Y = ส่งขึ้น)
    public float wallJumpDuration = 0.2f; // ระยะเวลาที่ล็อคการควบคุม (วินาที)
    private bool _isWallJumping; // สถานะว่ากำลังอยู่ในช่วงถีบตัวหรือไม่

    [Header("Other Config")] 
    public int CurrentRoomIndex = 0;
    [Header("Camera Transition")]
    public float CameraTransitionInterval = 1.5f;
    public float CurrentCameraTransitionCooldown;

    [Header("Particle Config")] 
    public ParticleSystem dustPS;
    public ParticleSystem diePS;
    public GameObject dashImpactPrefab; // ลาก Prefab ที่เพิ่งทำมาใส่ตรงนี้
    public ParticleSystem HookImpact;
    public float hitStopDuration = 0.1f;

    [Header("SFX Config")] 
    public AudioClip hookSound;
    public AudioClip dashSound;
    public AudioClip damageSound;
    public AudioClip jumpSound;
    public AudioClip landSound;
    public AudioClip changeMaskSound;

    [Header("Walk SFX")]
    public AudioClip walkSound;
    public float walkStepInterval = 0.4f; // ระยะห่างระว่างก้าว (วินาที)
    private float _currentWalkTimer;
    private AudioSource _audioSource; // แหล่งกำเนิดเสียงส่วนตัวของ Player
    private float _stopDurationTimer = 0f;

    private bool isHooking = false;
    
    private Rigidbody2D _rb;
    private GameControl _controls;
    private Vector2 _moveInput;
    private TransitionCharacter  _transition;
    private Transform _platformSpawnPos;
    private CircleCollider2D _dashCollider;
    private bool _canJump;
    private bool _facingRight = true;
    private bool isDead;
    
    private bool _isTouchingWall;
    private bool _isWallSliding;
    
    //Abilities
    private bool isDashing; // สถานะ Dash

    // ... (Awake, OnEnable, OnDisable, Start เหมือนเดิม) ...
    private void Awake()
    {
        _controls = new GameControl();
        _controls.Player.UseSkill.performed += cfx => UseSkill();
        _controls.Player.Move.performed += cfx => _moveInput = cfx.ReadValue<Vector2>();
        _controls.Player.Move.canceled += cfx => _moveInput = Vector2.zero;
        _controls.Player.Jump.performed += cfx => JumpStart();
        _controls.Player.Jump.canceled += cfx => JumpStop();
        _controls.Player.Slot1.performed += cfx => ChangeMask(1);
        _controls.Player.Slot2.performed += cfx => ChangeMask(2);
        _controls.Player.Slot3.performed += cfx => ChangeMask(3);
    }
    
    private void OnEnable() => _controls.Enable();
    private void OnDisable() => _controls.Disable();

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _platformSpawnPos = GetComponentInChildren<Transform>().Find("PlatformSpawnPos");
        _audioSource = GetComponent<AudioSource>();
        _anim = GetComponent<Animator>();
        _col = GetComponent<CapsuleCollider2D>();
        _transition = GetComponentInChildren<TransitionCharacter>();
        // หา Collider ลูก ถ้าหาไม่เจอลองลากใส่แบบ Public แทนก็ได้
        Transform dashColTransform = GetComponentInChildren<Transform>().Find("dashCollider");
        if(dashColTransform != null)
             _dashCollider = dashColTransform.GetComponent<CircleCollider2D>();
             
        if (wallMask == 0) wallMask = groundMask;
        ChangeMask((int)currentMaskType);
    }

    void Update()
    {
        if (isDead) return;
        // 1. ถ้ากำลัง Dash ให้เช็คการชน
        if (isDashing)
        {
            HandleDashCollision();
        }
        if (currentMaskType == MaskType.Hook)
        {
            HandleHookAiming();
        }
        else
        {
            // ปิด Indicator เมื่อไม่ได้ใช้หน้ากาก Hook
            if (targetIndicator != null) targetIndicator.SetActive(false);
            if (rangeIndicator != null) rangeIndicator.enabled = false;
        }
        CheckCanJump();
        CheckTouchingWall();
        
        // 2. ถ้า Dash อยู่ ห้ามขยับด้วยปุ่มเดิน (ให้ Dash คุม)
        if (!isDashing && CurrentCameraTransitionCooldown <= 0) 
        {
            PlayerMove();
            HandleWallSlide(); 
            HandleWalkSound(); // [เพิ่มใหม่] เรียกฟังก์ชันเสียงเดิน
        }

        CameraTransitionTicking();
        FreezeGravity();
        TickingDownMaskSwap();
        MaskCooldownTicking();
        UpdateAnimationParameters();
    }
    private void UpdateAnimationParameters()
    {
        if (_anim == null) return;

        // ส่งความเร็วการเดิน (ค่าสัมบูรณ์)
        _anim.SetFloat("Speed", Mathf.Abs(_moveInput.x));

        // ส่งสถานะพื้น
        _anim.SetBool("IsGrounded", _canJump);

        // ส่งความเร็วแนวดิ่ง (เพื่อแยก JumpUp / JumpDown)
        _anim.SetFloat("VerticalVelocity", _rb.linearVelocity.y);

        // ส่งสถานะเกาะกำแพง
        _anim.SetBool("IsWallSliding", _isWallSliding);
    }
    private void HandleWalkSound()
    {
        // เงื่อนไข: เดิน + อยู่บนพื้น + ไม่ไถลกำแพง
        if (_canJump && _moveInput.x != 0 && !_isWallSliding)
        {
            // ถ้ากลับมาเดินแล้ว ให้รีเซ็ตตัวนับเวลาหยุดเป็น 0
            _stopDurationTimer = 0f;

            _currentWalkTimer -= Time.deltaTime;

            if (_currentWalkTimer <= 0)
            {
                if (walkSound != null && _audioSource != null)
                {
                    _audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                    _audioSource.PlayOneShot(walkSound);
                }
                _currentWalkTimer = walkStepInterval;
            }
        }
        else
        {
            // ถ้าหยุดเดิน อย่าเพิ่งรีเซ็ตเสียงทันที!
            // ให้นับเวลาดูว่าหยุดนานหรือยัง?
            _stopDurationTimer += Time.deltaTime;

            // ถ้าหยุดนิ่งเกิน 0.15 วินาที ค่อยถือว่า "หยุดจริง" แล้วค่อยรีเซ็ต
            // (ค่า 0.15f ปรับได้ ถ้ายังรัวอยู่ให้เพิ่มขึ้นนิดหน่อย)
            if (_stopDurationTimer > 0.15f)
            {
                _currentWalkTimer = 0;
            }
        }
    }
    private void CameraTransitionTicking()
    {
        if (CurrentCameraTransitionCooldown > 0)
        {
            CurrentCameraTransitionCooldown -= Time.deltaTime;
        }
    }

    // ฟังก์ชันใหม่สำหรับเช็คการชนตอน Dash (ทำงานใน Update)
    private void HandleDashCollision()
    {
        if(_dashCollider == null) return;

        Collider2D hit = Physics2D.OverlapCircle(_dashCollider.transform.position, _dashCollider.radius, dashLayerMask);
        
        if (hit != null)
        {
            // 1. Spawn Effect ตรงจุดที่ชน
            // หาจุดกึ่งกลางระหว่างเรากับของที่ชน เพื่อความแม่นยำ
            Vector2 hitPos = (transform.position + hit.transform.position) / 2;
            
            // คำนวณมุมหมุน Effect (ให้พุ่งสวนทางกับทิศที่เราชน)
            Quaternion rot = Quaternion.LookRotation(_facingRight ? Vector3.right : Vector3.left);
            
            if (dashImpactPrefab != null)
            {
                Instantiate(dashImpactPrefab, hitPos, rot);
            }

            // 2. เรียก Hit Stop (หยุดเวลา)
            StartCoroutine(HitStopRoutine());

            // 3. Logic การทำลายของเดิม
            BreakablePlatform breakable = hit.GetComponent<BreakablePlatform>();
            if (breakable != null) breakable.Break();
            else Destroy(hit.gameObject);

            // 4. กระแทกกลับ
            int direction = _facingRight ? -1 : 1; 
            _rb.linearVelocity = Vector2.zero; 
            _rb.AddForce(new Vector2(-knockbackForce.x * direction, knockbackForce.y), ForceMode2D.Impulse);
            
            OnEndDashing();
        }
    }

    // ฟังก์ชันหยุดเวลา (Hit Stop)
    private IEnumerator HitStopRoutine()
    {
        // หยุดเวลาเกือบสนิท (อย่าใช้ 0 เพราะอาจมีปัญหากับบางระบบ ใช้ค่าน้อยๆ แทน)
        Time.timeScale = 0.01f; 
        
        // รอเวลา (ใช้ Realtime เพราะ timeScale โดนลดอยู่)
        yield return new WaitForSecondsRealtime(hitStopDuration);
        
        // คืนค่าเวลา
        Time.timeScale = 1f;
    }


    // ... (CheckTouchingWall, HandleWallSlide, MaskCooldownTicking, FreezeGravity เหมือนเดิม) ...
    private void CheckTouchingWall()
    {
        Vector2 direction = _facingRight ? Vector2.right : Vector2.left;
        _isTouchingWall = Physics2D.Raycast(transform.position, direction, wallCheckDistance, wallMask);
    }

    private void HandleWallSlide()
    {
        if (_isTouchingWall && !_canJump && _rb.linearVelocity.y < 0 && _moveInput.x != 0 && !_isWallJumping)
        {
            _isWallSliding = true;
        }
        else
        {
            _isWallSliding = false;
        }

        if (_isWallSliding)
        {
            if(allMask.Count == 0 || allMask[(int)currentMaskType] == null) return;
            float slideSpeed = allMask[(int)currentMaskType].maskData.wallFrictionSlide;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, Mathf.Clamp(_rb.linearVelocity.y, -slideSpeed, float.MaxValue));
        }
    }

    private void MaskCooldownTicking()
    {
        foreach (var var in allMask)
        {
            if (var.maskData.currentCooldown > 0)
                var.maskData.currentCooldown -= Time.deltaTime;
        }
    }
    
    private void FreezeGravity()
    {
        if (_currentFreezeGravityTime > 0)
        {
            _currentFreezeGravityTime -= Time.deltaTime;
            _rb.gravityScale = 0;
            _rb.linearVelocity = Vector2.zero;
        }
        else
        {
            _rb.gravityScale = 1;
        }
    }

    private void TickingDownMaskSwap()
    {
        if(_currentSwapCooldown > 0)
            _currentSwapCooldown -= Time.deltaTime;
    }

    private void CheckCanJump()
    {
        // 1. เก็บสถานะเก่าไว้ก่อน (ว่าเฟรมที่แล้วอยู่บนพื้นไหม)
        bool wasGrounded = _canJump;

        // 2. อัปเดตสถานะปัจจุบัน
        _canJump = Physics2D.Raycast(transform.position, Vector2.down, jumpCheckRange, groundMask);
        
        if (!wasGrounded && _canJump && _rb.linearVelocity.y <= 0.1f)
        {
            SpawnDust(); // เรียก Effect ฝุ่น
            if (landSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(landSound);
            }
        }
    }
    
    // ... (PlayerMove, FlipSprite, ChangeMask, UseSkill, JumpStart, JumpStop, OnDrawGizmos เหมือนเดิม) ...

    private void PlayerMove()
    {
        if (_isWallJumping) return;
        FlipSprite();
        float targetSpeedX = _moveInput.x * speed;
        float newSpeedX;
        if (isOnIce)
        {
            float acceleration = 2f; 
            if (_moveInput.x != 0) acceleration = 5f; 
            newSpeedX = Mathf.Lerp(_rb.linearVelocity.x, targetSpeedX, acceleration * Time.deltaTime);
        }
        else
        {
            newSpeedX = targetSpeedX;
        }

        _rb.linearVelocity = new Vector2(newSpeedX, _rb.linearVelocity.y);
    }
    
    private void FlipSprite()
    {
        if (_moveInput.x < 0 && _facingRight)
            _facingRight = false;
        else if (_moveInput.x > 0 && !_facingRight)
            _facingRight = true;
        int direction = _facingRight ? 1 : -1;
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * direction, transform.localScale.y, transform.localScale.z);
    }
    private void ChangeMask(int changeIndex)
    {
        if (_currentSwapCooldown > 0 && currentMaskType != (MaskType)changeIndex) return;
        if (changeIndex > allMask.Count - 1) return;
        currentMaskType = (MaskType)changeIndex;

        _currentFreezeGravityTime = freezeGravityTime;
        _currentSwapCooldown = cooldownInterval;

        // [ส่วนที่เพิ่มใหม่] : เปลี่ยน Animator และ Collider
        ApplyMaskVisuals(changeIndex);
        switch (currentMaskType)
        {
            case MaskType.Dash:
                _transition.TriggerDashTransition();
                break;
            case MaskType.Hook:
                _transition.TriggerHookTranstion();
                break;
            case MaskType.Jump:
                _transition.TriggerPlatformTransition();
                break;
        }
        AudioManager.instance.PlayOneShotSFX(changeMaskSound);
        StartCoroutine(hideCharacter());
    }
    private IEnumerator hideCharacter()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.enabled = false;
        yield return new WaitForSeconds(0.5f);
        sr.enabled = true;
    }
    private void ApplyMaskVisuals(int maskIndex)
    {
        if (allMask.Count <= maskIndex || allMask[maskIndex] == null) return;

        MaskBase selectedMask = allMask[maskIndex];

        // 1. เปลี่ยน Animation Set (Override Controller)
        if (selectedMask.animatorOverride != null)
        {
            _anim.runtimeAnimatorController = selectedMask.animatorOverride;
        }

        // 2. ปรับขนาดตัว (Collider)
        if (_col != null)
        {
            //_col.size = selectedMask.colliderSize;
            //_col.offset = selectedMask.colliderOffset;
        }
    }

    private void UseSkill()
    {
        if (allMask[(int)currentMaskType] == null) return;
        if (allMask[(int)currentMaskType].maskData.currentCooldown > 0) return;
        allMask[(int)currentMaskType].ActiveSkill(gameObject);
    }

    private void JumpStart()
    {
        if (allMask[(int)currentMaskType] == null || allMask.Count == 0) return;

        // 1. การกระโดดปกติจากพื้น
        if (_canJump)
        {
            float jumpHeight = allMask[(int)currentMaskType].maskData.jumpHeight;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0); // รีเซ็ตความเร็วแกน Y ก่อนโดด
            _rb.AddForce(Vector3.up * jumpHeight, ForceMode2D.Impulse);
            SpawnDust();

            if (jumpSound != null && _audioSource != null)
            {
                // Random Pitch นิดหน่อยเพื่อให้เสียงไม่ซ้ำซาก
                _audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
                _audioSource.PlayOneShot(jumpSound);
            }
        }
        // 2. [เพิ่มใหม่] การ Wall Jump (ต้องติดกำแพง และ ไม่ติดพื้น)
        else if (_isTouchingWall && !_canJump)
        {
            StartCoroutine(WallJumpRoutine());
        }
    }

    private IEnumerator WallJumpRoutine()
    {
        _isWallJumping = true;
        _anim.SetBool("IsUsingWallJump", _isWallJumping);
        // คำนวณทิศทางที่จะถีบตัวออก (ตรงข้ามกับด้านที่หันหน้าเข้ากำแพง)
        float jumpDirection = _facingRight ? -1 : 1;

        // ใส่แรงถีบตัว
        _rb.linearVelocity = Vector2.zero; // รีเซ็ตความเร็วก่อนเพื่อให้แรงสม่ำเสมอ
        _rb.AddForce(new Vector2(wallJumpForce.x * jumpDirection, wallJumpForce.y), ForceMode2D.Impulse);
        if (jumpSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(jumpSound);
        }
        // ถ้ากระโดดแล้วต้องหันหน้าไปทิศตรงข้ามทันที
        if ((jumpDirection > 0 && !_facingRight) || (jumpDirection < 0 && _facingRight))
        {
            _facingRight = !_facingRight;
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * jumpDirection, transform.localScale.y, transform.localScale.z);
        }

        SpawnDust();

        // รอเวลา Lock Input สักพักเพื่อให้ตัวละครลอยออกจากกำแพงได้จริง
        yield return new WaitForSeconds(wallJumpDuration);

        _isWallJumping = false;
        _anim.SetBool("IsUsingWallJump", _isWallJumping);

    }

    private void JumpStop()
    {
        if (allMask[(int)currentMaskType] == null) return;
        if (_rb.linearVelocity.y > 0)
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y * jumpCutMultipier);
    }
    private void HandleHookAiming()
    {
        // 1. ดึงระยะ Hook จริงๆ มาก่อน
        float realRange = 0f;
        if (allMask[(int)currentMaskType] is MaskHook hookMask)
        {
            realRange = hookMask.hookRange;
        }

        // --- [ส่วนที่เพิ่มใหม่] เช็คการแสดงผลวงกลม ---
        
        // ระยะตรวจสอบ = ระยะจริง + ระยะเผื่อ (เช่น 10 + 5 = 15)
        float checkRange = realRange + indicatorShowBuffer;

        // เช็คว่ามี HookTarget อยู่ในระยะไกลๆ บ้างไหม (แค่เช็คว่ามีไหม ไม่ต้องหาตัวที่ดีที่สุด)
        bool isAnyTargetNearby = Physics2D.OverlapCircle(transform.position, checkRange, hookLayer);

        if (isAnyTargetNearby)
        {
            // ถ้ามีของอยู่แถวๆ นี้ ให้วาดวงกลม (ขนาดเท่าระยะจริง)
            DrawRangeCircle(realRange);
        }
        else
        {
            // ถ้าไม่มีอะไรเลย ปิดวงกลมไปซะ จะได้ไม่รกตา
            if (rangeIndicator != null) rangeIndicator.enabled = false;
        }

        // ------------------------------------------

        // 3. หาเป้าหมายที่จะล็อคจริงๆ (ใช้ระยะ realRange เท่านั้น ห้ามโกงระยะ)
        _currentValidTarget = FindBestHookTarget(realRange);

        // 4. อัปเดตตัวชี้เป้า (Crosshair)
        if (_currentValidTarget != null)
        {
            if (targetIndicator != null)
            {
                targetIndicator.SetActive(true);
                targetIndicator.transform.position = _currentValidTarget.transform.position;
            }
        }
        else
        {
            if (targetIndicator != null) targetIndicator.SetActive(false);
        }
    }

    // --- ฟังก์ชันคำนวณ: หาเป้าที่ดีที่สุดและเช็คกำแพง ---
    private HookTarget FindBestHookTarget(float range)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, hookLayer);
        
        HookTarget bestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            // เช็คระยะห่าง
            float distance = Vector2.Distance(transform.position, hit.transform.position);
            
            // เงื่อนไข 1: ต้องใกล้กว่าตัวที่เคยเจอ
            if (distance < closestDistance)
            {
                // เงื่อนไข 2: ต้องไม่มีอะไรบัง (Raycast Check)
                Vector2 direction = (hit.transform.position - transform.position).normalized;
                
                // ยิง Ray จากตัวเรา ไปหาเป้าหมาย (ระยะ = distance)
                // ตรวจจับเฉพาะ obstacleLayer
                RaycastHit2D obstacleHit = Physics2D.Raycast(transform.position, direction, distance, obstacleLayer);

                // ถ้า Ray ไม่ชนอะไรเลย (null) แสดงว่าทางสะดวก
                if (obstacleHit.collider == null)
                {
                    HookTarget target = hit.GetComponent<HookTarget>();
                    if (target != null)
                    {
                        closestDistance = distance;
                        bestTarget = target;
                    }
                }
            }
        }

        return bestTarget;
    }

    // --- ฟังก์ชันวาดวงกลมด้วย LineRenderer ---
    private void DrawRangeCircle(float radius)
    {
        if (rangeIndicator == null) return;
        rangeIndicator.enabled = true;
        
        rangeIndicator.positionCount = rangeCircleSegments + 1;
        float angle = 0f;
        
        for (int i = 0; i <= rangeCircleSegments; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float y = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

            rangeIndicator.SetPosition(i, transform.position + new Vector3(x, y, 0));
            angle += (360f / rangeCircleSegments);
        }
    }
    public void StartHook(float range, float speed)
    {
        if (isHooking) return;

        // ไม่ต้อง OverlapCircle ใหม่แล้ว ใช้ค่าจาก HandleHookAiming ได้เลย
        if (_currentValidTarget != null)
        {
            StartCoroutine(HookRoutine(_currentValidTarget, _currentValidTarget.transform.position, speed));
        }
    }

    private IEnumerator HookRoutine(HookTarget target, Vector2 targetPos, float speed)
    {
        isHooking = true;
        _rb.linearVelocity = Vector2.zero;
        
        // เก็บค่า Gravity เดิมไว้
        float originalGravity = _rb.gravityScale;
        _rb.gravityScale = 0; 

        hookLineRenderer.enabled = true;
        Instantiate(HookImpact,targetPos,Quaternion.identity);
        AudioManager.instance.PlayOneShotSFX(hookSound);
        // --- กรณีที่ 1: ดึงตัวผู้เล่นไปหาจุด (Spiderman style) ---
        if (target.hookType == HookTarget.HookType.PullPlayerToPoint)
        {
            // หยุดจนกว่าจะถึงจุดหมาย (หรือใกล้มากพอ)
            while (Vector2.Distance(transform.position, targetPos) > 0.5f)
            {
                // อัปเดตเส้นเชือก
                hookLineRenderer.SetPosition(0, transform.position); // ต้นทาง: ตัวเรา
                hookLineRenderer.SetPosition(1, targetPos);          // ปลายทาง: จุดเกาะ

                // เคลื่อนที่
                transform.position = Vector2.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
                yield return null;
            }
        }
        // --- กรณีที่ 2: ดึง Platform มา (ผู้เล่นอยู่ที่เดิม Platform ขยับ) ---
        else if (target.hookType == HookTarget.HookType.PullObjectToPlayer)
        {
            // สั่งงาน Platform
            target.OnHooked(gameObject);

            // Effect: วาดเส้นเชือกค้างไว้แปปนึงให้รู้ว่าดึงแล้ว (เช่น 0.3 วินาที)
            float effectTimer = 0.3f;
            while (effectTimer > 0)
            {
                hookLineRenderer.SetPosition(0, transform.position);
                hookLineRenderer.SetPosition(1, targetPos);
                
                effectTimer -= Time.deltaTime;
                yield return null;
            }
        }

        // จบการทำงาน
        hookLineRenderer.enabled = false;
        _rb.gravityScale = originalGravity; // คืนค่า Gravity
        isHooking = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Spike") && !isDead)
        {
            diePS.Play();
            DeathCount.instance.addDeathCount();
            AudioManager.instance.PlayOneShotSFX(damageSound);
            GetComponent<SpriteRenderer>().enabled = false;
            StartCoroutine(OnPlayerDie());
        }
    }

    IEnumerator OnPlayerDie()
    {
        isDead = true;
        // หยุดการเคลื่อนที่
        _rb.linearVelocity = Vector2.zero;
        _rb.bodyType = RigidbodyType2D.Static; // ล็อคตัวไว้ก่อน

        yield return new WaitForSeconds(.75f);
        
        // --- เริ่มกระบวนการเกิดใหม่ ---

        // 1. สั่งรีเซ็ต Platform และ Orb ทั้งหมด
        if (LevelManager.instance != null) 
            LevelManager.instance.RespawnAllObjects();

        // 2. ย้ายไปจุดเกิดของห้องปัจจุบัน
        if (LevelManager.instance != null)
        {
            Transform respawnPoint = LevelManager.instance.GetSpawnPoint(CurrentRoomIndex);
            if (respawnPoint != null)
            {
                transform.position = new Vector3(respawnPoint.position.x, respawnPoint.position.y, 0f);            }
            else
            {
                Debug.LogWarning($"No Spawn Point found for Room {CurrentRoomIndex}");
            }
        }

        // 3. รีเซ็ตค่า Player
        isDead = false;
        GetComponent<SpriteRenderer>().enabled = true;
        _rb.bodyType = RigidbodyType2D.Dynamic; // ปลดล็อค
        _rb.linearVelocity = Vector2.zero;
        
        // รีเซ็ต Cooldown ต่างๆ (ถ้าจำเป็น)
        // _currentSwapCooldown = 0;
    }

    private void SpawnDust()
    {
        dustPS.Play();
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = _canJump ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * jumpCheckRange);

        Gizmos.color = _isTouchingWall ? Color.green : Color.red;
        Vector3 direction = _facingRight ? Vector3.right : Vector3.left;
        Gizmos.DrawLine(transform.position, transform.position + direction * wallCheckDistance);

        if (currentMaskType == MaskType.Hook)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 10f);
        }

        if (_dashCollider != null)
        {
            Gizmos.color = Color.lightPink;
            Gizmos.DrawWireSphere(_dashCollider.transform.position,_dashCollider.radius);
        }
    }

    // --- แก้ไขตรงนี้: ลบ while loop ออก ---
    public void OnStartDashing()
    {
        isDashing = true; 
        AudioManager.instance.PlayOneShotSFX(dashSound);
        // แค่เปิด Boolean แล้วให้ Update ทำงานต่อ
    }

    public void OnEndDashing()
    {
        isDashing = false;
        // เมื่อจบ Dash ให้รีเซ็ตความเร็วแกน X (ถ้าต้องการให้หยุดทันที)
        // _rb.linearVelocity = Vector2.zero; 
    }

    public void OnTrasitionCamera()
    {
        CurrentCameraTransitionCooldown = CameraTransitionInterval;
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        _moveInput = Vector2.zero;
    }
    public void PlaySkillAnimation()
    {
        _anim.SetTrigger("UsingSkill");
    }
    public void AddNewMask(MaskBase newMask)
    {
        if(allMask.Contains(newMask)) return;
        allMask.Add(newMask);
    }
    // Getter
    public bool GetFacingRight() => _facingRight;
    public Transform GetPlatformSpawnPos() => _platformSpawnPos;
    public bool IsDashing() => isDashing; // เผื่อ MaskDash อยากเช็ค
    
    public enum MaskType { Normal, Dash, Hook, Jump }
}