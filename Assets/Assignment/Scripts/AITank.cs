using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class AITank : Agent
{
    private const float speed = 20f;
    private const float range = 30f;
    private Rigidbody rbody;
    private Vector3 origin;

    private EnemyTankNew[] enemies;
    private FriendlyTankNew[] friendlies;
    private int maxEnemies = 4;
    private int maxFriendlies = 2;

    // Start is called before the first frame update
    void Start()
    {
        rbody = GetComponent<Rigidbody>();
        origin = rbody.position;
    }

    public override void OnEpisodeBegin()
    {
        SetReward(0);
        transform.localPosition = origin;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position.x);

        enemies = FindObjectsOfType<EnemyTankNew>();
        friendlies = FindObjectsOfType<FriendlyTankNew>();
        for (int i = 0; i < maxEnemies; i++)
        {
            if (i < enemies.Length)
            {
                sensor.AddObservation(enemies[i].transform.position.x);
                sensor.AddObservation(enemies[i].transform.position.z);
            }
            else
            {
                sensor.AddObservation(0);
                sensor.AddObservation(100); // pretend the enemy tank is very far away
            }
        }

        for (int i = 0; i < friendlies.Length; i++)
        {
            if (i < friendlies.Length)
            {
                sensor.AddObservation(friendlies[i].transform.position.x);
                sensor.AddObservation(friendlies[i].transform.position.z);
            }
            else
            {
                sensor.AddObservation(0);
                sensor.AddObservation(100); // pretend the friendly tank is very far away
            }
        }
    }

    private void MoveX(float x)
    {
        x = Math.Min(1, Math.Max(-1, x)); // dir should be between -1 (left at full speed) and 1 (right at full speed)
        float targetX = rbody.position.x + x * speed * Time.fixedDeltaTime; // move take to desired position
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
                AddScore(2);
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.green); 
                Debug.Log("Hit enemy. Score = " + GetCumulativeReward().ToString());
                hit.collider.gameObject.GetComponent<EnemyTankNew>().Hit();
            }
            else if (hit.collider.CompareTag("Friendly"))
            {   
                AddScore(-1);
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.red); 
                Debug.Log("Hit friendly. Score = " + GetCumulativeReward().ToString());
                hit.collider.gameObject.GetComponent<FriendlyTankNew>().Hit();
            }
        }
        else
        { 
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * range, Color.black); 
        }
    }

    // allow arrow keys to control AI - for testing
    public override void Heuristic(in ActionBuffers actionsOut)
    {   
        // use left and right arrows to move
        ActionSegment<float> continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");

        // use space to shoot
        ActionSegment<int> discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("EnemyAI"))
        {
            SetReward(int.MinValue); // lose and restart game
            Debug.Log("Collision with enemy. Game over. New episode starting...");
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Friendly"))
        {
            AddScore(2); // add 2 points for collecting friendly tank
            Debug.Log("Collected friendly. Score = " + GetCumulativeReward().ToString());
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float x = actionBuffers.ContinuousActions[0];
        MoveX(x);

        int shoot = actionBuffers.DiscreteActions[0];
        if (shoot == 1)
        {
            Shoot();
        }
    }

    public void AddScore(int reward)
    {
        AddReward(reward);

        if (GetCumulativeReward() >= 20)
        {
            Debug.Log("Victory!!");
            EndEpisode();
        }
        
    }
}