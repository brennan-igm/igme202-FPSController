using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A modifation of the depreicated Standard Asset Package FPSControler
/// </summary>

public class FPSController : MonoBehaviour
{
    private CharacterController m_CharacterController;
    private CollisionFlags m_CollisionFlags;
    private bool m_PreviouslyGrounded;
    [SerializeField] private float m_StickToGroundForce = 10;
    [SerializeField] private float m_GravityMultiplier = 2;
    


    private Vector2 m_LookInput;
    [SerializeField] private FPSMouseLook m_MouseLook = null;
    private Camera m_Camera;


    private Vector2 m_MoveInput;
    [SerializeField] private bool m_IsWalking = false;
    [SerializeField] private float m_WalkSpeed = 5;
    [SerializeField] private float m_RunSpeed = 10;
    private Vector3 m_MoveDir = Vector3.zero;
    [SerializeField] private float m_StepInterval = 5;
    [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten = 0.7f;
    private float m_StepCycle;
    private float m_NextStep;


    private bool m_Jump;
    [SerializeField] private float m_JumpSpeed = 10;
    private bool m_Jumping;


    // Start is called before the first frame update
    void Start()
    {
        m_CharacterController = GetComponent<CharacterController>();
        m_Camera = Camera.main;
        m_StepCycle = 0f;
        m_NextStep = m_StepCycle / 2f;
        m_Jumping = false;

        m_MouseLook.Init(transform, m_Camera.transform);
    }

    // Update is called once per frame
    void Update()
    {
        if(m_LookInput != Vector2.zero)
            m_MouseLook.LookRotation(transform, m_Camera.transform, m_LookInput);

        if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
        {
            m_MoveDir.y = 0f;
            m_Jumping = false;
        }
        if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
        {
            m_MoveDir.y = 0f;
        }

        m_PreviouslyGrounded = m_CharacterController.isGrounded;
    }

    private void FixedUpdate()
    {
        float speed;
        // set the desired speed to be walking or running
        speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;

        // normalize input if it exceeds 1 in combined length:
        if (m_MoveInput.sqrMagnitude > 1)
        {
            m_MoveInput.Normalize();
        }
        // always move along the camera forward as it is the direction that it being aimed at
        Vector3 desiredMove = transform.forward * m_MoveInput.y + transform.right * m_MoveInput.x;

        // get a normal for the surface that is being touched to move along it
        RaycastHit hitInfo;
        Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                           m_CharacterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

        m_MoveDir.x = desiredMove.x * speed;
        m_MoveDir.z = desiredMove.z * speed;


        if (m_CharacterController.isGrounded)
        {
            m_MoveDir.y = -m_StickToGroundForce;

            if (m_Jump)
            {
                m_MoveDir.y = m_JumpSpeed;
                m_Jump = false;
                m_Jumping = true;
            }
        }
        else
        {
            m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
        }
        m_CollisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);

        ProgressStepCycle(speed);
    }
    
    private void ProgressStepCycle(float speed)
    {
        if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_MoveInput.x != 0 || m_MoveInput.y != 0))
        {
            m_StepCycle += (m_CharacterController.velocity.magnitude + (speed * (m_IsWalking ? 1f : m_RunstepLenghten))) *
                         Time.fixedDeltaTime;
        }

        if (!(m_StepCycle > m_NextStep))
        {
            return;
        }

        m_NextStep = m_StepCycle + m_StepInterval;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        //dont move the rigidbody if the character is on top of it
        if (m_CollisionFlags == CollisionFlags.Below)
        {
            return;
        }

        if (body == null || body.isKinematic)
        {
            return;
        }
        body.AddForceAtPosition(m_CharacterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        m_MoveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        m_LookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!m_Jump && context.phase == InputActionPhase.Started)
        {
            m_Jump = true;
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        switch(context.phase)
        {
            case InputActionPhase.Started:
            case InputActionPhase.Performed:
                m_IsWalking = false;
                break;
            case InputActionPhase.Canceled:
            case InputActionPhase.Disabled:
                m_IsWalking = true;
                break;
        }
    }
}
