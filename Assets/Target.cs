using System;
using Unity.VisualScripting;
using UnityEngine;

public class Target : MonoBehaviour
{
    public Player m_player;
    public enum eState : int
    {
        kIdle,
        kHopStart,
        kHop,
        kCaught,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
   {
        new Color(255, 0,   0),
        new Color(0,   255, 0),
        new Color(0,   0,   255),
        new Color(255, 255, 255)
   };

    // External tunables.
    public float m_fHopTime = 0.2f;
    public float m_fHopSpeed = 6.5f;
    public float m_fScaredDistance = 3.0f;
    public int m_nMaxMoveAttempts = 50;

    // Internal variables.
    public eState m_nState;
    public float m_fHopStart;
    public Vector3 m_vHopStartPos;
    public Vector3 m_vHopEndPos;

    void Start()
    {
        // Setup the initial state and get the player GO.
        m_nState = eState.kIdle;
        m_player = GameObject.FindObjectOfType(typeof(Player)) as Player;
    }

    void FixedUpdate()
    {
        switch (m_nState)
        {
            case eState.kIdle:
                // "stay in one place until the player gets close"
                if (Vector3.Distance(transform.position, m_player.transform.position) <= m_fScaredDistance)
                {
                    Debug.Log("Too close!");
                    m_nState = eState.kHopStart;
                }
                break;
            case eState.kHopStart:
                // "hop... without going off the screen, AND avoid the player"

                // set hop start values
                //Debug.Log(String.Format("Starting hop from ({0},{1})", transform.position.x, transform.position.y));
                m_vHopStartPos = transform.position;
                m_fHopStart = Time.time;
                float fHopLength = m_fHopTime * m_fHopSpeed;

                // set initial hop end/loop values based on player position
                Vector3 vOffset = transform.position - m_player.transform.position;
                m_vHopEndPos = transform.position; // placeholder in case we make it through all our attempts without being in bounds
                float fTryAngle;
                float fMaxDistance = 0; // not accurate, but means it's harder to get trapped against a wall (hopping is better than staying in one spot)
                int nAttempts = 0;
                do
                {
                    // increment here for easier continues
                    nAttempts++;

                    // try a new angle
                    fTryAngle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    Vector3 vTryPos = (new Vector3(Mathf.Cos(fTryAngle), Mathf.Sin(fTryAngle), 0) * fHopLength) + transform.position;
                    //Debug.Log(String.Format("Attempt #{0}: {1} degrees, {2} radians -> ({3},{4})",nAttempts, fTryAngle * Mathf.Rad2Deg, fTryAngle, vTryPos.x, vTryPos.y));

                    // is the position in bounds?
                    Vector2 vScreenPos = Camera.main.WorldToScreenPoint(vTryPos);
                    if (0 > vScreenPos.x || vScreenPos.x > Camera.main.pixelWidth 
                        || 0 > vScreenPos.y || vScreenPos.y > Camera.main.pixelHeight)
                    {
                        //Debug.Log("Out of Bounds!");
                        continue;
                    }

                    // is the position better than our best attempt so far?
                    float fTryDistance = Vector3.Distance(vTryPos, m_player.transform.position);
                    if (fTryDistance > fMaxDistance)
                    {
                        //Debug.Log(String.Format("Better! New distance: {0}", fTryDistance));
                        fMaxDistance = fTryDistance;
                        m_vHopEndPos = vTryPos;
                    }
                } while (nAttempts < m_nMaxMoveAttempts && fMaxDistance <= m_fScaredDistance);

                Debug.Log(String.Format("Initiating hop after {0} attempts", nAttempts));
                transform.up = m_vHopEndPos - m_vHopStartPos;
                m_nState = eState.kHop;
                break;
            case eState.kHop:
                // move along hop vector
                float fStep = Time.deltaTime * m_fHopSpeed;
                transform.position = Vector3.MoveTowards(transform.position, m_vHopEndPos, fStep);

                // check for end of hop & state change
                if (Time.time >= m_fHopStart + m_fHopTime) m_nState = eState.kIdle;
                break;
            case eState.kCaught:
                // "attaches itself to the player when it is caught"
                // already handled by OnTriggerStay2D
                break;
            default:
                Debug.Log("Unknown Target state reached: " + m_nState.ToString());
                break;
        }
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        // Check if this is the player (in this situation it should be!)
        if (collision.gameObject == GameObject.Find("Player"))
        {
            // If the player is diving, it's a catch!
            if (m_player.IsDiving())
            {
                m_nState = eState.kCaught;
                transform.parent = m_player.transform;
                transform.localPosition = new Vector3(0.0f, -0.5f, 0.0f);
            }
        }
    }
}