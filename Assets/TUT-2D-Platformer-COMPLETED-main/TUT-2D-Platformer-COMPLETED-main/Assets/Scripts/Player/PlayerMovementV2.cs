using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementV2 : MonoBehaviour
{
    [Header("References")]
    public PlayerMovementStats MoveStats;
    [SerializeField] private Collider2D _feetColl;
    [SerializeField] private Collider2D _headColl;
    [SerializeField] private Collider2D _bodyColl;

    [Header("FX")]
    [SerializeField] private GameObject _jumpParticles;
    [SerializeField] private GameObject _secondJumpParticles;
    [SerializeField] private GameObject _landParticles;
    [SerializeField] private Transform _particleSpawnTransform;
    [SerializeField] private TrailRenderer _trailRenderer;
    [SerializeField] private ParticleSystem _speedParticles;
    [SerializeField] private GameObject _dashParticles;

    [Header("Height Tracker")]
    [SerializeField] private Transform _heightTracker;

    private Rigidbody2D _rb;
    private Animator _anim;

    //movement vars
    public Vector2 HorizontalVelocity { get; private set; }
    private bool _isFacingRight;

    //collision check vars
    private RaycastHit2D _groundHit;
    private RaycastHit2D _headHit;
    private RaycastHit2D _wallHit;
    private RaycastHit2D _lastWallHit;
    private bool _isGrounded;
    private bool _bumpedHead;
    private bool _isTouchingWall;

    //jump vars
    public float VerticalVelocity { get; private set; }
    private bool _isJumping;
    private bool _isFastFalling;
    public bool _isFalling;
    private float _fastFallTime;
    private float _fastFallReleaseSpeed;
    private int _numberOfJumpsUsed;
    //apex vars
    private float _apexPoint;
    private float _timePastApexThreshold;
    private bool _isPastApexThreshold;
    //jump buffer vars
    private float _jumpBufferTimer;
    private bool _jumpReleasedDuringBuffer;
    //coyote time vars
    private float _coyoteTimer;

    //wall jump vars
    public bool IsWallSliding { get; private set; }
    private bool _isWallSlideFalling;
    private bool _useWallJumpMoveStats;
    private bool _isWallJumping;
    private float _wallJumpTime;
    private bool _isWallJumpFastFalling;
    private bool _isWallJumpFalling;
    private float _wallJumpFastFallTime;
    private float _wallJumpFastFallReleaseSpeed;

    private float _wallJumpPostBufferTimer;

    private float _wallJumpApexPoint;
    private float _timePastWallJumpApexThreshold;
    private bool _isPastWallJumpApexThreshold;

    //dash vars
    private bool _isDashing;
    private bool _isAirDashing;
    private float _dashTimer;
    private float _dashOnGroundTimer;
    private int _dashDirectionMult;
    private int _numberOFDashesUsed;
    private Vector2 _dashDirection;
    private bool _isDashFastFalling;
    private float _dashFastFallTime;
    private float _dashFastFallReleaseSpeed;

    private GhostTrail _ghostTrail;

    // Add a field to track the highest point
    private float _highestPoint;
    private float _heightTrackerStartingPoint;

    private void Awake()
    {
        _isFacingRight = true;

        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        _ghostTrail = GetComponent<GhostTrail>();

        _trailRenderer.emitting = false;
    }

    private void Update()
    {
        CountTimers();

        JumpChecks();
        LandCheck();
        WallSlideCheck();
        WallJumpCheck();
        DashCheck();

        //ResetJumpedThisFrame();
    }

    private void FixedUpdate()
    {
        CollisionChecks();
        Jump();
        Fall();

        WallSlide();
        WallJump();
        Dash();

        if (_isGrounded)
        {
            Move(MoveStats.GroundAcceleration, MoveStats.GroundDeceleration, InputManager.Movement);
        }
        else
        {
            //Walljumping
            if (_useWallJumpMoveStats)
            {
                Move(MoveStats.WallJumpMoveAcceleration, MoveStats.WallJumpMoveDeceleration, InputManager.Movement);
            }

            //AIREBORNE
            else
            {
                Move(MoveStats.AirAcceleration, MoveStats.AirDeceleration, InputManager.Movement);
            }
        }

        ApplyVelocity();
    }


    private void OnDrawGizmos()
    {
        if (MoveStats.ShowWalkJumpArc)
        {
            DrawJumpArc(MoveStats.MaxWalkSpeed, Color.white);
        }

        if (MoveStats.ShowRunJumpArc)
        {
            DrawJumpArc(MoveStats.MaxRunSpeed, Color.red);
        }
    }

    private void IncrementVerticalVelocity(float incrementAmount)
    {
        VerticalVelocity += incrementAmount;
        //Debug.Log(VerticalVelocity);
    }

    private void ChangeVerticalVelocity(float velAmount)
    {
        VerticalVelocity = velAmount;
        //Debug.Log(VerticalVelocity);
    }

    private void ApplyVelocity()
    {
        //Clamp speed
        if (!_isDashing)
        {
            ChangeVerticalVelocity(Mathf.Clamp(VerticalVelocity, -MoveStats.MaxFallSpeed, 50f));
        }
        else
        {
            ChangeVerticalVelocity(Mathf.Clamp(VerticalVelocity, -50f, 50f));
        }

        _rb.linearVelocity = new Vector2(HorizontalVelocity.x, VerticalVelocity);
    }

    #region Fall and Land Checks

    private void Fall()
    {
        //NORMAL GRAVITY (without jumping)
        if (!_isGrounded && !_isJumping && !IsWallSliding && !_isWallJumping && !_isDashing && !_isDashFastFalling)
        {
            if (!_isFalling)
            {
                _isFalling = true;
                _trailRenderer.emitting = true;
                _anim.ResetTrigger("land");
                _anim.SetTrigger("fall");
            }

            IncrementVerticalVelocity(MoveStats.Gravity * Time.fixedDeltaTime);
        }
    }

    private void LandCheck()
    {
        //LANDED
        if ((_isJumping || _isFalling || _isWallJumpFalling || _isWallJumping || _isWallSlideFalling || IsWallSliding || _isDashFastFalling) && _isGrounded && VerticalVelocity <= 0f)
        {
            ResetJumpValues();
            StopWallSliding();
            ResetWallJumpValues();
            ResetDashes();

            ChangeVerticalVelocity(Physics2D.gravity.y);

            _numberOfJumpsUsed = 0;

            _trailRenderer.emitting = false;

            if (_isDashFastFalling && _isGrounded)
            {
                if (!_anim.GetBool("isAirDashFalling"))
                {
                    ResetDashValues();
                    return;                    
                }
            }

            ResetDashValues();

            //new
            _anim.SetTrigger("land");
            Instantiate(_landParticles, _particleSpawnTransform.position, Quaternion.identity);

            //height tracker
            if (MoveStats.DebugShowHeightLogOnLand)
            {
                Debug.Log("Highest Point : " + (_highestPoint - _heightTrackerStartingPoint));
            }


        }
    }

    #endregion

    #region Movement

    private void Move(float acceleration, float deceleration, Vector2 moveInput)
    {
        if (!_isDashing)
        {
            if ((Mathf.Abs(moveInput.x) < MoveStats.MoveThreshold) && InputManager.RunIsHeld)
            {
                _anim.SetBool("isWalking", false);
                HorizontalVelocity = Vector2.Lerp(HorizontalVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
                //ApplyMovementVelocity(HorizontalVelocity);
            }

            else if ((Mathf.Abs(moveInput.x) < MoveStats.MoveThreshold) && !InputManager.RunIsHeld)
            {
                _anim.SetBool("isWalking", false);
                _anim.SetBool("isRunning", false);
                if (_speedParticles.isPlaying)
                {
                    _speedParticles.Stop();
                }

                HorizontalVelocity = Vector2.Lerp(HorizontalVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
                //ApplyMovementVelocity(HorizontalVelocity);
            }

            else if ((Mathf.Abs(moveInput.x) >= MoveStats.MoveThreshold))
            {
                TurnCheck(moveInput);

                _anim.SetBool("isWalking", true);
                Vector2 targetVelocity = Vector2.zero;

                if (InputManager.RunIsHeld)
                {
                    _anim.SetBool("isRunning", true);

                    targetVelocity = new Vector2(moveInput.x, 0f) * MoveStats.MaxRunSpeed;

                    if (Mathf.Abs(HorizontalVelocity.x) >= MoveStats.MaxRunSpeed - 2f && !_isJumping && !_isFalling)
                    {
                        if (!_speedParticles.isPlaying)
                        {
                            _speedParticles.Play();
                        }
                    }
                    else
                    {
                        if (_speedParticles.isPlaying)
                        {
                            _speedParticles.Stop();
                        }
                    }
                }
                else
                {
                    targetVelocity = new Vector2(moveInput.x, 0f) * MoveStats.MaxWalkSpeed;
                }

                HorizontalVelocity = Vector2.Lerp(HorizontalVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
                //ApplyMovementVelocity(HorizontalVelocity);
            }
        }

        if (!InputManager.RunIsHeld)
        {
            _anim.SetBool("isRunning", false);
            if (_speedParticles.isPlaying)
            {
                _speedParticles.Stop();
            }
        }        
    }

    private void TurnCheck(Vector2 moveInput)
    {
        if (_isFacingRight && moveInput.x < 0)
        {
            Turn(false);
        }

        else if (!_isFacingRight && moveInput.x > 0)
        {
            Turn(true);
        }
    }

    private void Turn(bool turnRight)
    {
        if (turnRight)
        {
            _isFacingRight = true;
            transform.Rotate(0f, 180f, 0f);
        }

        else
        {
            _isFacingRight = false;
            transform.Rotate(0f, -180f, 0f);
        }
    }

    #endregion

    #region Jump

    private void JumpChecks()
    {
        if (InputManager.JumpWasPressed)
        {
            //cancel if we should jump from a post wall jump buffer
            if (_isWallSlideFalling && _wallJumpPostBufferTimer >= 0f)
            {
                return;
            }

            //cancel if we are wall sliding or touching a wall in the air
            else if (IsWallSliding || (_isTouchingWall && !_isGrounded))
            {
                return;
            }

            _jumpBufferTimer = MoveStats.JumpBufferTime;
            _jumpReleasedDuringBuffer = false; //
        }

        //START FAST FALLING
        if (InputManager.JumpWasReleased)
        {
            if (_jumpBufferTimer > 0f)
            {
                _jumpReleasedDuringBuffer = true;
            }

            if (_isJumping && VerticalVelocity > 0f)
            {
                if (_isPastApexThreshold)
                {
                    _isPastApexThreshold = false;
                    _isFastFalling = true;
                    _fastFallTime = MoveStats.TimeForUpwardsCancel;

                    //gets rid of floatiness
                    ChangeVerticalVelocity(0f);

                }
                else
                {
                    _isFastFalling = true;
                    _fastFallReleaseSpeed = VerticalVelocity;
                }
            }
        }

        //ACTUAL JUMP WITH COYOTE TIME AND JUMP BUFFER
        if (_jumpBufferTimer > 0f && !_isJumping && (_isGrounded || _coyoteTimer > 0f))
        {
            InitiateJump(1, _jumpParticles);
            if (_isDashFastFalling)
            {
                _isDashFastFalling = false;
            }

            //Debug.Log("normal jump");
            if (_jumpReleasedDuringBuffer)
            {
                _isFastFalling = true;
                _fastFallReleaseSpeed = VerticalVelocity;
            }

            _heightTrackerStartingPoint = _heightTracker.position.y;
            _highestPoint = _heightTracker.position.y;
        }

        //ACTUAL JUMP WITH DOUBLE JUMP
        else if (_jumpBufferTimer > 0f && (_isJumping || _isWallJumping || _isWallSlideFalling || _isAirDashing || _isDashFastFalling) && !_isTouchingWall && _numberOfJumpsUsed < MoveStats.NumberOfJumpsAllowed)
        {
            //Debug.Log("air jump");

            _isFastFalling = false;
            InitiateJump(1, _secondJumpParticles);

            if (_isDashFastFalling)
            {
                _isDashFastFalling= false;
            }
        }

        //handle air jump AFTER the coyote time has lapsed (take off an extra jump so we don't get a bonus jump)
        else if (_jumpBufferTimer > 0f && _isFalling && !_isWallSlideFalling && _numberOfJumpsUsed < MoveStats.NumberOfJumpsAllowed - 1)
        {
            //Debug.Log("edge case jump");

            _isFastFalling = false;
            InitiateJump(2, _jumpParticles);
        }
    }

    private void InitiateJump(int numberOfJumpsUsed, GameObject particlesToSpawn)
    {
        if (!_isJumping)
        {
            _isJumping = true;
        }

        ResetWallJumpValues();

        _jumpBufferTimer = 0f;
        _numberOfJumpsUsed += numberOfJumpsUsed;
        ChangeVerticalVelocity(MoveStats.InitialJumpVelocity);

        //FX
        _anim.SetTrigger("jump");
        _anim.ResetTrigger("land");
        _trailRenderer.emitting = true;

        Instantiate(particlesToSpawn, _particleSpawnTransform.position, Quaternion.identity);
    }

    private void Jump()
    {
        //APPLY JUMP GRAVITY
        if (_isJumping)
        {
            //HIT HEAD
            if (_bumpedHead)
            {
                _isFastFalling = true;
            }

            //GRAVITY IN ASCENDING
            if (VerticalVelocity >= 0f)
            {
                //APEX CONTROLS
                _apexPoint = Mathf.InverseLerp(MoveStats.InitialJumpVelocity, 0f, VerticalVelocity);

                if (_apexPoint > MoveStats.ApexThreshold)
                {
                    if (!_isPastApexThreshold)
                    {
                        _isPastApexThreshold = true;
                        _timePastApexThreshold = 0f;
                    }

                    if (_isPastApexThreshold)
                    {
                        _timePastApexThreshold += Time.fixedDeltaTime;
                        if (_timePastApexThreshold < MoveStats.ApexHangTime)
                        {
                            ChangeVerticalVelocity(0f);
                        }
                        else
                        {
                            ChangeVerticalVelocity(-0.01f);
                        }
                    }
                }

                //GRAVITY IN ASCENDING BUT NOT PAST APEX THRESHOLD
                else if (!_isFastFalling)
                {
                    IncrementVerticalVelocity(MoveStats.Gravity * Time.fixedDeltaTime);

                    if (_isPastApexThreshold)
                    {
                        _isPastApexThreshold = false;
                    }
                }

            }

            //GRAVITY ON DESCENDING
            else if (!_isFastFalling)
            {
                IncrementVerticalVelocity(MoveStats.Gravity * MoveStats.GravityOnReleaseMultiplier * Time.fixedDeltaTime);
            }

            else if (VerticalVelocity < 0f)
            {
                if (!_isFalling)
                    _isFalling = true;
            }

            if (_heightTracker.position.y > _highestPoint)
            {
                _highestPoint = _heightTracker.position.y;
            }
        }

        //HANDLE JUMP CUT TIME
        if (_isFastFalling)
        {
            if (_fastFallTime >= MoveStats.TimeForUpwardsCancel)
            {
                IncrementVerticalVelocity(MoveStats.Gravity * MoveStats.GravityOnReleaseMultiplier * Time.fixedDeltaTime);
            }
            else if (_fastFallTime < MoveStats.TimeForUpwardsCancel)
            {
                ChangeVerticalVelocity(Mathf.Lerp(_fastFallReleaseSpeed, 0f, (_fastFallTime / MoveStats.TimeForUpwardsCancel)));
            }

            _fastFallTime += Time.fixedDeltaTime;
        }    
    }

    private void ResetJumpValues()
    {
        _isJumping = false;
        _isFalling = false;
        _isFastFalling = false;
        _fastFallTime = 0f;
        _isPastApexThreshold = false;
    }

    #endregion

    #region Wall Slide

    private void WallSlideCheck()
    {
        if (_isTouchingWall && !_isGrounded && !_isDashing)
        {
            if (VerticalVelocity < 0f && !IsWallSliding)
            {
                ResetJumpValues();
                ResetWallJumpValues();
                ResetDashValues();

                if (MoveStats.ResetDashOnWallSlide)
                {
                    ResetDashes();
                }

                _isWallSlideFalling = false;
                IsWallSliding = true;

                _anim.SetBool("isWallSliding", true);

                if (MoveStats.ResetJumpsOnWallSlide)
                {
                    _numberOfJumpsUsed = 0;
                }
            }            
        }

        else if (IsWallSliding && !_isTouchingWall! && !_isGrounded && !_isWallSlideFalling)
        {
            _isWallSlideFalling = true;
            StopWallSliding();
        }

        else
        {
            StopWallSliding();
        }
    }

    private void WallSlide()
    {
        if (IsWallSliding)
        {
            ChangeVerticalVelocity(Mathf.Lerp(VerticalVelocity, -MoveStats.WallSlideSpeed, MoveStats.WallSlideDecelerationSpeed * Time.fixedDeltaTime));
        }
    }

    private void StopWallSliding()
    {
        if (IsWallSliding)
        {
            _numberOfJumpsUsed++;

            IsWallSliding = false;
            _anim.SetBool("isWallSliding", false);
        }
    }


    #endregion

    #region WallJump

    private void WallJumpCheck()
    {
        if (ShouldApplyPostWallJumpBuffer())
        {
            _wallJumpPostBufferTimer = MoveStats.WallJumpPostBufferTime;
        }

        //START FAST FALLING
        if (InputManager.JumpWasReleased && !IsWallSliding && !_isTouchingWall && _isWallJumping)
        {
            if (_isWallJumping && VerticalVelocity > 0f)
            {
                if (_isPastWallJumpApexThreshold)
                {
                    _isPastWallJumpApexThreshold = false;
                    _isWallJumpFastFalling = true;
                    _wallJumpFastFallTime = MoveStats.TimeForUpwardsCancel;

                    //gets rid of floatiness
                    ChangeVerticalVelocity(0f);
                }
                else
                {
                    _isWallJumpFastFalling = true;
                    _wallJumpFastFallReleaseSpeed = VerticalVelocity;
                }
            }            
        }

        //ACTUAL JUMP WITH POST WALL JUMP BUFFER TIME
        if (InputManager.JumpWasPressed && _wallJumpPostBufferTimer > 0f)
        {
            InitiateWallJump();
        }
    }

    private void WallJump()
    {
        //APPLY WALL JUMP GRAVITY
        if (_isWallJumping)
        {
            //TIME TO TAKE OVER MOVEMENT CONTROLS WHILE WALL JUMPING
            _wallJumpTime += Time.fixedDeltaTime;
            if (_wallJumpTime >= MoveStats.TimeTillJumpApex)
            {
                _useWallJumpMoveStats = false;
            }

            //HIT HEAD
            if (_bumpedHead)
            {
                _isWallJumpFastFalling = true;
                _useWallJumpMoveStats = false;
            }

            //GRAVITY IN ASCENDING
            if (VerticalVelocity >= 0f)
            {
                //APEX CONTROLS
                _wallJumpApexPoint = Mathf.InverseLerp(MoveStats.WallJumpDirection.y, 0f, VerticalVelocity);

                if (_wallJumpApexPoint > MoveStats.ApexThreshold)
                {
                    if (!_isPastWallJumpApexThreshold)
                    {
                        _isPastWallJumpApexThreshold = true;
                        _timePastWallJumpApexThreshold = 0f;
                    }

                    if (_isPastWallJumpApexThreshold)
                    {
                        _timePastWallJumpApexThreshold += Time.fixedDeltaTime;
                        if (_timePastWallJumpApexThreshold < MoveStats.ApexHangTime)
                        {
                            ChangeVerticalVelocity(0f);
                        }
                        else
                        {
                            ChangeVerticalVelocity(-0.01f);
                        }
                    }
                }

                //GRAVITY IN ASCENDING BUT NOT PAST APEX THRESHOLD
                else if (!_isWallJumpFastFalling)
                {
                    IncrementVerticalVelocity(MoveStats.WallJumpGravity * Time.fixedDeltaTime);

                    if (_isPastWallJumpApexThreshold)
                    {
                        _isPastWallJumpApexThreshold = false;
                    }
                }

            }

            //GRAVITY ON DESCENDING
            else if (!_isWallJumpFastFalling)
            {
                IncrementVerticalVelocity(MoveStats.WallJumpGravity * Time.fixedDeltaTime);
            }

            else if (VerticalVelocity < 0f)
            {
                if (!_isWallJumpFalling)
                    _isWallJumpFalling = true;
            }

        }

        //HANDLE JUMP CUT TIME
        if (_isWallJumpFastFalling)
        {
            if (_wallJumpFastFallTime >= MoveStats.TimeForUpwardsCancel)
            {
                IncrementVerticalVelocity(MoveStats.WallJumpGravity * MoveStats.WallJumpGravityOnReleaseMultiplier * Time.fixedDeltaTime);
            }
            else if (_wallJumpFastFallTime < MoveStats.TimeForUpwardsCancel)
            {
                ChangeVerticalVelocity(Mathf.Lerp(_wallJumpFastFallReleaseSpeed, 0f, (_wallJumpFastFallTime / MoveStats.TimeForUpwardsCancel)));
            }

            _wallJumpFastFallTime += Time.fixedDeltaTime;
        }
    }

    private void InitiateWallJump(GameObject particlesToSpawn = null)
    {
        if (!_isWallJumping)
        {
            _isWallJumping = true;
            _useWallJumpMoveStats = true;
        }

        StopWallSliding();

        ResetJumpValues();
        _wallJumpTime = 0f;
        ChangeVerticalVelocity(MoveStats.InitialWallJumpVelocity);

        int dirMultiplier = 0;
        Vector2 hitDir = _lastWallHit.collider.ClosestPoint(_bodyColl.bounds.center);

        if (hitDir.x > transform.position.x)
        {
            dirMultiplier = -1;
        }
        else { dirMultiplier = 1; }

        HorizontalVelocity = new Vector2((Mathf.Abs(MoveStats.WallJumpDirection.x) * dirMultiplier), 0f);

        //FX
        _anim.SetTrigger("jump");
        _anim.ResetTrigger("land");
        _trailRenderer.emitting = true;

        //Instantiate(particlesToSpawn, _particleSpawnTransform.position, Quaternion.identity);
    }

    private bool ShouldApplyPostWallJumpBuffer()
    {
        if (!_isGrounded && (_isTouchingWall || IsWallSliding))
        {
            return true;
        }
        else { return false; }
    }

    private void ResetWallJumpValues()
    {
        _isWallSlideFalling = false;
        _useWallJumpMoveStats = false;
        _isWallJumping = false;
        _isWallJumpFastFalling = false;
        _isWallJumpFalling = false;
        _isPastWallJumpApexThreshold = false;

        _wallJumpFastFallTime = 0f;
        _wallJumpTime = 0f;
    }

    #endregion

    #region Dash

    private void DashCheck()
    {
        if (InputManager.DashWasPressed)
        {
            //ground dash
            if (_isGrounded && _dashOnGroundTimer < 0 && !_isDashing)
            {
                InitiateDash();
            }

            //air dash
            else if (!_isGrounded && !_isDashing && _numberOFDashesUsed < MoveStats.NumberOfDashes)
            {
                _isAirDashing = true;
                InitiateDash();

                //you left a wallslide but dashed within the wallJumpPostBufferTimer
                if (_wallJumpPostBufferTimer > 0f)
                {
                    _numberOfJumpsUsed--;
                    if (_numberOfJumpsUsed < 0)
                    {
                        _numberOfJumpsUsed = 0;
                    }
                }
            }
        }
    }

    private void InitiateDash()
    {
        //_isJumping = false;
        //_isFastFalling = false;


        _dashDirection = InputManager.Movement;

        Vector2 closestDirection = Vector2.zero;
        float minDistance = Vector2.Distance(_dashDirection, MoveStats.DashDirections[0]);

        for (int i = 0; i < MoveStats.DashDirections.Length; i++)
        {
            //skip if we hit it bang on
            if (_dashDirection == MoveStats.DashDirections[i])
            {
                closestDirection = _dashDirection;
                break;
            }

            float distance = Vector2.Distance(_dashDirection, MoveStats.DashDirections[i]);

            // Check if this is a diagonal direction and apply bias
            bool isDiagonal = (Mathf.Abs(MoveStats.DashDirections[i].x) == 1 && Mathf.Abs(MoveStats.DashDirections[i].y) == 1);
            if (isDiagonal)
            {
                distance -= MoveStats.DashDiagonallyBias;
            }

            else if (distance < minDistance)
            {
                minDistance = distance;
                closestDirection = MoveStats.DashDirections[i];
            }
        }

        //handle direction if we have no input
        if (closestDirection == Vector2.zero)
        {
            if (_isFacingRight)
            {
                closestDirection = Vector2.right;
            }
            else { closestDirection = Vector2.left; }
        }

        _dashDirectionMult = 1; //this may not be needed
        _dashDirection = new Vector2(closestDirection.x * _dashDirectionMult, closestDirection.y * _dashDirectionMult);

        _numberOFDashesUsed++;
        _isDashing = true;
        _dashTimer = 0f;
        _dashOnGroundTimer = MoveStats.TimeBtwDashesOnGround;

        //FX
        Quaternion particleRot = Quaternion.FromToRotation(Vector2.right, -_dashDirection);
        Instantiate(_dashParticles, transform.position, particleRot);

        _anim.SetBool("isDashing", true);
        _ghostTrail.LeaveGhostTrail(MoveStats.DashTime * 1.75f);

        ResetJumpValues();
        ResetWallJumpValues();
        StopWallSliding();
    }

    private void ResetDashes()
    {
        _numberOFDashesUsed = 0;
    }

    private void ResetDashValues()
    {
        _isDashFastFalling = false;
        _dashOnGroundTimer = -0.01f;
        _anim.SetBool("isAirDashFalling", false);
    }

    private void Dash()
    {
        if (_isDashing)
        {
            //stop the dash after the timer
            _dashTimer += Time.fixedDeltaTime;
            if (_dashTimer >= MoveStats.DashTime)
            {
                if (_isGrounded)
                {
                    ResetDashes();
                }
                else { _anim.SetBool("isAirDashFalling", true); }

                _isAirDashing = false;
                _isDashing = false;

                _anim.SetBool("isDashing", false);

                //start the time for upwards cancel
                if (!_isJumping && !_isWallJumping)
                {
                    _dashFastFallTime = 0f;
                    _dashFastFallReleaseSpeed = VerticalVelocity;

                    if (!_isGrounded)
                    _isDashFastFalling = true;
                }

                return;
            }

            HorizontalVelocity = new Vector2(MoveStats.DashSpeed * _dashDirection.x, 0f);
            
            if (_dashDirection.y != 0f  || _isAirDashing)
            ChangeVerticalVelocity(MoveStats.DashSpeed * _dashDirection.y);
        }

        //HANDLE DASH CUT TIME
        else if (_isDashFastFalling)
        {
            //new
            if (VerticalVelocity > 0f)
            {
                if (_dashFastFallTime < MoveStats.DashTimeForUpwardsCancel)
                {
                    ChangeVerticalVelocity(Mathf.Lerp(_dashFastFallReleaseSpeed, 0f, (_dashFastFallTime / MoveStats.DashTimeForUpwardsCancel)));
                }
                else if (_dashFastFallTime >= MoveStats.DashTimeForUpwardsCancel)
                {
                    IncrementVerticalVelocity(MoveStats.Gravity * MoveStats.DashGravityOnReleaseMultiplier * Time.fixedDeltaTime);
                }

                _dashFastFallTime += Time.fixedDeltaTime;            
            }
            else
            {
                IncrementVerticalVelocity(MoveStats.Gravity * MoveStats.DashGravityOnReleaseMultiplier * Time.fixedDeltaTime);
            }

            //if (_dashFastFallTime >= MoveStats.DashTimeForUpwardsCancel)
            //{
            //    IncrementVerticalVelocity(MoveStats.Gravity * MoveStats.DashGravityOnReleaseMultiplier * Time.fixedDeltaTime);
            //}
            //else if (_dashFastFallTime < MoveStats.DashTimeForUpwardsCancel)
            //{
            //    ChangeVerticalVelocity(Mathf.Lerp(_dashFastFallReleaseSpeed, 0f, (_dashFastFallTime / MoveStats.DashTimeForUpwardsCancel)));
            //}

        }
    }

    #endregion

    #region Timers

    private void CountTimers()
    {
        //JUMP BUFFER TIMER
        _jumpBufferTimer -= Time.deltaTime;

        //HANDLE WALL JUMP BUFFER TIMER
        if (!ShouldApplyPostWallJumpBuffer())
        {
            _wallJumpPostBufferTimer -= Time.deltaTime;
        }

        //HANDLE COYOTE TIMER
        if (!_isGrounded)
        {
            _coyoteTimer -= Time.deltaTime;
        }
        else { _coyoteTimer = MoveStats.JumpCoyoteTime; }

        //HANDLE DASH TIMER
        if (_isGrounded)
        {
            _dashOnGroundTimer -= Time.deltaTime;
        }
    }

    #endregion

    #region Collision Checks

    private void IsGrounded()
    {
        Vector2 boxCastOrigin = new Vector2(_feetColl.bounds.center.x, _feetColl.bounds.min.y);
        Vector2 boxCastSize = new Vector2(_feetColl.bounds.size.x, MoveStats.GroundDetectionRayLength);

        _groundHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.down, MoveStats.GroundDetectionRayLength, MoveStats.GroundLayer);
        if (_groundHit.collider != null)
        {
            _isGrounded = true;
        }
        else { _isGrounded = false; }

        #region Debug Visualization
        if (MoveStats.DebugShowIsGroundedBox)
        {
            Color rayColor;
            if (_isGrounded)
            {
                rayColor = Color.green;
            }
            else { rayColor = Color.red; }

            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * MoveStats.GroundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * MoveStats.GroundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y - MoveStats.GroundDetectionRayLength), Vector2.right * boxCastSize.x, rayColor);
        }
        #endregion
    }

    private void IsTouchingWall()
    {
        float originEndPoint = 0f;
        if (_isFacingRight)
        {
            originEndPoint = _bodyColl.bounds.max.x;
        }
        else { originEndPoint = _bodyColl.bounds.min.x; }

        float adjustedHeight = _bodyColl.bounds.size.y * MoveStats.WallDetectionRayHeightMultiplier;
        //Vector2 boxCastOrigin = new Vector2(originEndPoint, _bodyColl.bounds.max.y);
        Vector2 boxCastOrigin = new Vector2(originEndPoint, _bodyColl.bounds.center.y);
        Vector2 boxCastSize = new Vector2(MoveStats.WallDetectionRayLength, adjustedHeight);

        _wallHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, transform.right, MoveStats.WallDetectionRayLength, MoveStats.GroundLayer);
        if (_wallHit.collider != null)
        {
            _lastWallHit = _wallHit;
            _isTouchingWall = true;

            //if (MoveStats.ResetJumpsOnWallTouch)
            //{
            //    _numberOfJumpsUsed = 0;
            //}
        }
        else { _isTouchingWall = false; }

        #region Debug Visualization

        if (MoveStats.DebugShowWallHitBox)
        {
            Color rayColor;
            if (_isTouchingWall)
            {
                rayColor = Color.green;
            }
            else { rayColor = Color.red; }

            Vector2 boxBottomLeft = new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y - boxCastSize.y / 2);
            Vector2 boxBottomRight = new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y - boxCastSize.y / 2);
            Vector2 boxTopLeft = new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y + boxCastSize.y / 2);
            Vector2 boxTopRight = new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y + boxCastSize.y / 2);

            Debug.DrawLine(boxBottomLeft, boxBottomRight, rayColor);
            Debug.DrawLine(boxBottomRight, boxTopRight, rayColor);
            Debug.DrawLine(boxTopRight, boxTopLeft, rayColor);
            Debug.DrawLine(boxTopLeft, boxBottomLeft, rayColor);
        }

        #endregion
    }


    private void BumpedHead()
    {
        Vector2 boxCastOrigin = new Vector2(_feetColl.bounds.center.x, _headColl.bounds.max.y);
        Vector2 boxCastSize = new Vector2(_feetColl.bounds.size.x * MoveStats.HeadWidth, MoveStats.HeadDetectionRayLength);

        _headHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.up, MoveStats.HeadDetectionRayLength, MoveStats.GroundLayer);
        if (_headHit.collider != null)
        {
            _bumpedHead = true;
        }
        else { _bumpedHead = false; }

        #region Debug Visualization

        if (MoveStats.DebugShowHeadBumpBox)
        {
            float headWidth = MoveStats.HeadWidth;

            Color rayColor;
            if (_bumpedHead)
            {
                rayColor = Color.green;
            }
            else { rayColor = Color.red; }

            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2 * headWidth, boxCastOrigin.y), Vector2.up * MoveStats.HeadDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x + (boxCastSize.x / 2) * headWidth, boxCastOrigin.y), Vector2.up * MoveStats.HeadDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2 * headWidth, boxCastOrigin.y + MoveStats.HeadDetectionRayLength), Vector2.right * boxCastSize.x * headWidth, rayColor);
        }

        #endregion
    }

    private void CollisionChecks()
    {
        IsGrounded();
        IsTouchingWall();
        BumpedHead();
    }


    #endregion

    #region Jump Visualization

    private void DrawJumpArc(float moveSpeed, Color gizmoColor)
    {
        Vector2 startPosition = new Vector2(_feetColl.bounds.center.x, _feetColl.bounds.min.y);
        Vector2 previousPosition = startPosition;
        float speed = 0f;
        if (MoveStats.DrawRight)
        {
            speed = moveSpeed;
        }
        else { speed = -moveSpeed; }
        Vector2 velocity = new Vector2(speed, MoveStats.InitialJumpVelocity);

        Gizmos.color = gizmoColor;

        float timeStep = 2 * MoveStats.TimeTillJumpApex / MoveStats.ArcResolution; // time step for the simulation
        //float totalTime = (2 * MoveStats.TimeTillJumpApex) + MoveStats.ApexHangTime; // total time of the arc including hang time

        for (int i = 0; i < MoveStats.VisualizationSteps; i++)
        {
            float simulationTime = i * timeStep;
            Vector2 displacement;
            Vector2 drawPoint;

            if (simulationTime < MoveStats.TimeTillJumpApex) // Ascending
            {
                displacement = velocity * simulationTime + 0.5f * new Vector2(0, MoveStats.Gravity) * simulationTime * simulationTime;
            }
            else if (simulationTime < MoveStats.TimeTillJumpApex + MoveStats.ApexHangTime) // Apex hang time
            {
                float apexTime = simulationTime - MoveStats.TimeTillJumpApex;
                displacement = velocity * MoveStats.TimeTillJumpApex + 0.5f * new Vector2(0, MoveStats.Gravity) * MoveStats.TimeTillJumpApex * MoveStats.TimeTillJumpApex;
                displacement += new Vector2(speed, 0) * apexTime; // No vertical movement during hang time
            }
            else // Descending
            {
                float descendTime = simulationTime - (MoveStats.TimeTillJumpApex + MoveStats.ApexHangTime);
                displacement = velocity * MoveStats.TimeTillJumpApex + 0.5f * new Vector2(0, MoveStats.Gravity) * MoveStats.TimeTillJumpApex * MoveStats.TimeTillJumpApex;
                displacement += new Vector2(speed, 0) * MoveStats.ApexHangTime; // Horizontal movement during hang time
                displacement += new Vector2(speed, 0) * descendTime + 0.5f * new Vector2(0, MoveStats.Gravity) * descendTime * descendTime;
            }

            drawPoint = startPosition + displacement;

            if (MoveStats.StopOnCollision)
            {
                RaycastHit2D hit = Physics2D.Raycast(previousPosition, drawPoint - previousPosition, Vector2.Distance(previousPosition, drawPoint), MoveStats.GroundLayer);
                if (hit.collider != null)
                {
                    // If a hit is detected, stop drawing the arc at the hit point
                    Gizmos.DrawLine(previousPosition, hit.point);
                    break;
                }
            }

            Gizmos.DrawLine(previousPosition, drawPoint);
            previousPosition = drawPoint;
        }
    }


    #endregion

}
