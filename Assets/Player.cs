using Unity.VisualScripting;
using UnityEngine;

public class Player : MonoBehaviour
{
    // External tunables.
    static public float m_fMaxSpeed = 0.10f;
    public float m_fSlowSpeed = m_fMaxSpeed * 0.66f;
    public float m_fIncSpeed = 0.0025f;
    public float m_fMagnitudeFast = 0.6f;
    public float m_fMagnitudeSlow = 0.06f;
    public float m_fFastRotateSpeed = 0.2f;
    public float m_fFastRotateMax = 10.0f;
    public float m_fDiveTime = 0.3f;
    public float m_fDiveRecoveryTime = 0.5f;
    public float m_fDiveDistance = 3.0f;

    // Internal variables.
    public Vector3 m_vDiveStartPos;
    public Vector3 m_vDiveEndPos;
    public float m_fAngle;
    public float m_fSpeed;
    public float m_fTargetSpeed;
    public float m_fTargetAngle;
    public eState m_nState;
    public float m_fDiveStartTime;

    public enum eState : int
    {
        kMoveSlow,
        kMoveFast,
        kDiving,
        kRecovering,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
    {
        new Color(0,     0,   0),
        new Color(255, 255, 255),
        new Color(0,     0, 255),
        new Color(0,   255,   0),
    };

    public bool IsDiving()
    {
        return (m_nState == eState.kDiving);
    }

    void CheckForDive()
    {
        if (Input.GetMouseButton(0) && (m_nState != eState.kDiving && m_nState != eState.kRecovering))
        {
            // Start the dive operation
            m_nState = eState.kDiving;
            m_fSpeed = 0.0f;

            // Store starting parameters.
            m_vDiveStartPos = transform.position;
            m_vDiveEndPos = m_vDiveStartPos - (transform.right * m_fDiveDistance);
            m_fDiveStartTime = Time.time;
        }
    }

    void Start()
    {
        // Initialize variables.
        m_fAngle = 0;
        m_fSpeed = 0;
        m_nState = eState.kMoveSlow;
    }

    void UpdateDirectionAndSpeed()
    {
        // Get relative positions between the mouse and player
        Vector3 vScreenPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 vScreenSize = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
        Vector2 vOffset = new Vector2(transform.position.x - vScreenPos.x, transform.position.y - vScreenPos.y);

        // Find the target angle being requested.
        m_fTargetAngle = Mathf.Atan2(vOffset.y, vOffset.x) * Mathf.Rad2Deg;

        // Calculate how far away from the player the mouse is.
        float fMouseMagnitude = vOffset.magnitude / vScreenSize.magnitude;

        // Based on distance, calculate the speed the player is requesting.
        if (fMouseMagnitude > m_fMagnitudeFast)
        {
            m_fTargetSpeed = m_fMaxSpeed;
        }
        else if (fMouseMagnitude > m_fMagnitudeSlow)
        {
            m_fTargetSpeed = m_fSlowSpeed;
        }
        else
        {
            m_fTargetSpeed = 0.0f;
        }
    }

    void FixedUpdate()
    {
        CheckForDive(); // check here so we can start the dive immediately, the method already restricts which states are allowed
        switch (m_nState)
        {
            case eState.kMoveSlow:
                // "moving slowly until the speed reaches the fast threshold"
                // make updates based on mouse position
                UpdateDirectionAndSpeed();
                m_fSpeed = Mathf.MoveTowards(m_fSpeed, m_fTargetSpeed, m_fIncSpeed);
                transform.eulerAngles = new Vector3(0, 0, m_fTargetAngle);
                transform.position = Vector3.MoveTowards(transform.position, transform.position - transform.right, m_fSpeed);

                // check for state change based on speed
                if (m_fSpeed > m_fSlowSpeed) m_nState = eState.kMoveFast;
                break;
            case eState.kMoveFast:
                break;
            case eState.kDiving:
                // "similar to hop this should be a visible but quick movement...recovery afterwards"
                // move along dive vector
                float fStep = Time.deltaTime * m_fDiveDistance / m_fDiveTime;
                transform.position = Vector3.MoveTowards(transform.position, m_vDiveEndPos, fStep);

                // check for end of dive & state change
                if (Time.time >= m_fDiveStartTime + m_fDiveTime) m_nState = eState.kRecovering;
                break;
            case eState.kRecovering:
                // "no movement is possible, followed by transitioning to the slow move state"
                if (Time.time >= m_fDiveStartTime + m_fDiveTime + m_fDiveRecoveryTime) m_nState = eState.kMoveSlow;
                break;
            default:
                Debug.Log("Unknown Player state reached: " + m_nState.ToString());
                break;
        }
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
    }
}
