using System;
using Unity.MLAgents.Extensions.Sensors;
using Unity.Sentis.Layers;
using UnityEngine;

public class AITank : MonoBehaviour
{
    private const float speed = 20f;
    private const float range = 30f;
    private int score = 0;
    private Rigidbody rbody;
    private Vector3 origin;

    // Start is called before the first frame update
    void Start()
    {
        rbody = GetComponent<Rigidbody>();
        origin = rbody.position;
    }

    private void MoveX(float dir)
    {
        dir = Math.Min(1, Math.Max(-1, dir)); // dir should be between -1 (left at full speed) and 1 (right at full speed)
        float targetX = rbody.position.x + dir * speed * Time.fixedDeltaTime; // move take to desired position
        float newX = Math.Min(30f, Math.Max(-30f, targetX)); // ensure tank is within the game boundary

        Vector3 newPos = new Vector3(newX, origin.y, origin.z);
        rbody.MovePosition(newPos);
    }

    private void Shoot()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, range))
        {
            if (hit.collider.CompareTag("EnemyAI"))
            {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.green); 
                Debug.Log("Hit Enemy");
                score += 2;
            }
            else if (hit.collider.CompareTag("Friendly"))
            {   
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.red); 
                Debug.Log("Hit Friendly");
                score -= 1;
            }
        }
        else
        { 
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * range, Color.black); 
        }
    }

    private int i = -200;
    void FixedUpdate()
    {
        if (i > 0)
        {
            MoveX(-0.5f);
        }
        else
        {
            MoveX(0.5f);
        }
        i++;

        if (i == 200)
        {
            i = -200;
        }

        Shoot();
    }
}