using System;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/*
run 15 - first run that works without crashing, but negative reward
*/

public class AITank : Agent
{
    private const float speed = 20f;
    private const float range = 30f;
    private Rigidbody rbody;
    private Vector3 origin;

    private EnemyTankNew[] enemies;
    private FriendlyTankNew[] friendlies;

    private int totalScore;

    // Start is called before the first frame update
    void Start()
    {
        rbody = GetComponent<Rigidbody>();
        origin = rbody.position;
    }

    public override void OnEpisodeBegin()
    {
        totalScore = 0;
        SetReward(0);
        transform.localPosition = origin;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position.x);

        enemies = FindObjectsOfType<EnemyTankNew>();
        friendlies = FindObjectsOfType<FriendlyTankNew>();

        int nearestEnemy = -1;
        float nearestEnemyZ = 100;
        for (int i = 0; i < enemies.Length; i++)
        {   
            float enemyZ = enemies[i].transform.position.z;
            if (enemyZ < nearestEnemyZ)
            {
                nearestEnemy = i;
                nearestEnemyZ = enemyZ;
            }
        }

        if (nearestEnemy > -1)
        {
            sensor.AddObservation(enemies[nearestEnemy].transform.position.x);
            sensor.AddObservation(enemies[nearestEnemy].transform.position.z);
        }
        else
        {
            sensor.AddObservation(0);
            sensor.AddObservation(100); // pretend there is an enemy tank far away
        }

        int nearestFriendly = -1;
        float nearestFriendlyZ = 100;
        for (int i = 0; i < friendlies.Length; i++)
        {   
            float friendlyZ = friendlies[i].transform.position.z;
            if (friendlyZ < nearestFriendlyZ)
            {
                nearestFriendly = i;
                nearestFriendlyZ = friendlyZ;
            }
        }

        if (nearestFriendly > -1)
        {
            sensor.AddObservation(friendlies[nearestFriendly].transform.position.x);
            sensor.AddObservation(friendlies[nearestFriendly].transform.position.z);
        }
        else
        {
            sensor.AddObservation(0);
            sensor.AddObservation(100); // pretend there is a friendly tank far away
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
                AddScore(2, 0.02f);
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.green); 
                hit.collider.gameObject.GetComponent<EnemyTankNew>().Hit();
            }
            else if (hit.collider.CompareTag("Friendly"))
            {   
                AddScore(-1, -0.01f);
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.red); 
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
            SetReward(-1); // lose and restart game
            Debug.Log("Lost the game. Starting new episode...");
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Friendly"))
        {
            AddScore(2, 0.01f); // add 2 points for collecting friendly tank
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(-0.0005f); // penalize for not winning

        float x = actionBuffers.ContinuousActions[0];
        MoveX(x);

        int shoot = actionBuffers.DiscreteActions[0];
        if (shoot == 1)
        {
            Shoot();
        }
    }

    public int GetScore()
    {
        return totalScore;
    }

    public void AddScore(int score, float reward)
    {   
        totalScore += score;
        AddReward(reward);

        if (totalScore >= 20)
        {
            Debug.Log("Victory!!. Starting new episode...");
            SetReward(1); // win game
            EndEpisode();
        }
    }
}