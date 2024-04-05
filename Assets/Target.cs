using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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
                m_vHopStartPos = transform.position;
                m_fHopStart = Time.time;
                float fHopLength = m_fHopTime * m_fHopSpeed;

                int nAttempts = 0;
                do
                {
                    // try a new jump position
                    // check to see if it's in bounds
                    //      no -> skip to next try
                    // check to see if it's too close to the player
                    //      yes -> save as an in bounds option (max distance maybe?)
                    //      no -> hop!
                } while (nAttempts < m_nMaxMoveAttempts);

                // calculate safe direction ?
                //Vector3 vTryDirection = (m_vHopStartPos - m_player.transform.position).normalized;

                m_nState = eState.kHop;
                break;
            case eState.kHop:
                // move along hop vector
                float fStep = Time.deltaTime * m_fHopSpeed;
                transform.position = Vector3.MoveTowards(transform.position, m_vHopEndPos, fStep);

                // check for end of hop & state change
                if (Vector3.Distance(transform.position, m_vHopEndPos) < 0.001f) m_nState = eState.kIdle;
                break;
            default: // handles eState.kCaught
                // "attaches itself to the player when it is caught"
                // already parented to player by OnTriggerStay2D, player's movement should handle this
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