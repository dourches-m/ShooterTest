using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using System.Collections;

public class Movement : MonoBehaviour
{

    public CharacterController controller;
    Animator animator;
    public float jumpHeight;
    public float gravity = 9.81f;
    public GameObject RigAim;
    public Vector2 _move;
    public Vector2 _look;

    public GameObject mainCamera;
    public float speed;

    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("For locking the camera position on all axis")]
    public bool LockCameraPosition = false;

    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 70.0f;

    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30.0f;

    [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
    public float CameraAngleOverride = 0.0f;

    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;
    private const float _threshold = 0.01f;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;

    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    RaycastWeapon weapon;
    public ParticleSystem muzzleFlash;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
        weapon = GetComponentInChildren<RaycastWeapon>();
    }

    public void Fire(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            animator.SetBool("isFiring", true);
            weapon.StartFiring();
            StartCoroutine(RigAnimation(1));
        }
        else if (context.canceled)
        {
            animator.SetBool("isFiring", false);
            weapon.StopFiring();
            StartCoroutine(RigAnimation(0));
        }  

    }

    IEnumerator RigAnimation(float targetWeight)
    {
        float weightBase = RigAim.GetComponent<Rig>().weight;
        if (targetWeight < weightBase)
        {
            for (float fade = weightBase; fade > targetWeight; fade -= 0.01f)
            {
                weightBase = fade;
                RigAim.GetComponent<Rig>().weight = weightBase;
                yield return null;
            }
        }
        else
        {
            for (float fade = weightBase; fade < targetWeight; fade += 0.01f)
            {
                weightBase = fade;
                RigAim.GetComponent<Rig>().weight = weightBase;
                yield return null;
            }
        }

    }

    public void Jump(InputAction.CallbackContext context)
    {

        if (controller.isGrounded)
        {
            animator.SetTrigger("Jump");
            //moveVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        _look = context.ReadValue<Vector2>();
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (context.started)
            animator.SetBool("isWalking", true);
        else if (context.canceled)
            animator.SetBool("isWalking", false);

        _move = context.ReadValue<Vector2>();
    }

    public void MoveUpdate()
    {
        Vector3 inputDirection = new Vector3(_move.x, 0.0f, _move.y).normalized;

        if (_move != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                              mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                RotationSmoothTime);

            // rotate to face input direction relative to camera position
            //transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            controller.Move(targetDirection.normalized * (speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        }

    }

    private void CameraRotation()
    {
        // if there is an input and camera position is not fixed
        if (_look.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            //Don't multiply mouse input by Time.deltaTime;
            float deltaTimeMultiplier = Time.deltaTime;

            _cinemachineTargetYaw += _look.x * deltaTimeMultiplier * 100f;
            _cinemachineTargetPitch += _look.y * deltaTimeMultiplier * 100f;
        }

        // clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw, 0.0f);

        transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw, 0.0f);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }


    // Update is called once per frame
    void Update()
    {
        MoveUpdate();
        isFiring();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void isFiring()
    {
        if (weapon.isFiring)
            muzzleFlash.Emit(1);
    }
}
