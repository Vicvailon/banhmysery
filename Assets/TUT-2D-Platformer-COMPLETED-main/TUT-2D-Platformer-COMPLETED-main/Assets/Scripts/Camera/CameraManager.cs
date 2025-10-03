using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _mainCamera;
    [SerializeField] private CinemachineCamera _noDeadZoneCam;
    [SerializeField] private float _mainCamDeadzoneHeight = 0.4f;
    [SerializeField] private float _lerpTime = 0.25f;
    [SerializeField] private float _timeForSwitch = 0.5f;
    //private PlayerMovementV2 _playerMovement;
    private Player _player;

    private bool _isMainCamera;
    private bool _isNoDeadZoneCamera;

    private float _timer;

    private void Awake()
    {
        //_playerMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovementV2>();
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();

        SwitchToMainCamera(false);
    }

    private void FixedUpdate()
    {
        //if (_playerMovement.VerticalVelocity <= -_playerMovement.MoveStats.MaxFallSpeed)

        if (_player.VerticalVelocity <= -_player.MoveStats.MaxFallSpeed)
        {
            if (_isMainCamera)
            {
                _timer += Time.fixedDeltaTime;
                if (_timer > _timeForSwitch)
                {
                    SwitchToNoDeadZoneCamera();
                    _timer = 0f;
                }
            }
        }

        //else if (_playerMovement.IsWallSliding)
        else if (_player.IsWallSliding)
        {
            if (!_isNoDeadZoneCamera)
            {
                _timer += Time.fixedDeltaTime;
                if (_timer > _timeForSwitch)
                {
                    SwitchToNoDeadZoneCamera();
                    _timer = 0f;
                }

            }
        }

        else if (_isNoDeadZoneCamera)
        {
            SwitchToMainCamera(true);
            //_timer = 0f;
        }

    }

    public void SwitchToMainCamera(bool runTimer)
    {
        if (runTimer)
        {
            StartCoroutine(ReturnToNormalDeadZone());
        }
        else
        {
            _mainCamera.enabled = true;
            _noDeadZoneCam.enabled = false;

            _isNoDeadZoneCamera = false;
            _isMainCamera = true;
        }        
    }

    private IEnumerator ReturnToNormalDeadZone()
    {
        _mainCamera.enabled = true;
        _noDeadZoneCam.enabled = false;

        _isNoDeadZoneCamera = false;
        _isMainCamera = true;

        CinemachineComposer transposer = _mainCamera.GetComponent<CinemachineComposer>();

        transposer.m_DeadZoneHeight = 0f;
        yield return new WaitForSeconds(0.1f);

        float elapsedTime = 0f;
        while(elapsedTime < _lerpTime)
        {
            elapsedTime += Time.deltaTime;

            float lerpedAmount = Mathf.Lerp(0f, _mainCamDeadzoneHeight, (elapsedTime / _lerpTime));
            transposer.m_DeadZoneHeight = lerpedAmount;

            yield return null;
        }
    }

    public void SwitchToNoDeadZoneCamera()
    {
        _noDeadZoneCam.enabled = true;
        _mainCamera.enabled = false;

        _isNoDeadZoneCamera = true;
        _isMainCamera = false;
    }
}
