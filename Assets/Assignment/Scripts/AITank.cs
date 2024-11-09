using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.Sentis.Layers;
using UnityEngine;

/*
run 15 - first run that works without crashing, but negative reward
run 21 - decouple reward and score, penalize for time, but still not winning
run 31 - observe 2 tanks. less negative scores but the tank tends to stay in the corner, and also ends up shooting a lot of friendlies
run 32 - behavioral cloning. results in a lot more movement from the tank but it is letting a lot of enemies through
run 44 - finally working. can win games. but it tends to shoot the friendlies. mean reward also does not converge too well.
run 46 - lower the learning rate and epsilon. also increase reward for collecting friendly and penalize more for shooting friendly. 
run 47 - behavioral cloning. quite useless since my strategy is to wait in front of the enemy and shoot it, but the agent ends up always kiling itself
*/

public class AITank : Agent
{
    [SerializeField] private TankNewSpawn tankNewSpawn;
    private const float speed = 20f;
    private const float range = 30f;
    private const int enemiesObserved = 2;
    private const int friendliesObserved = 1;
    private Rigidbody rbody;
    private Vector3 origin;

    private int totalScore;
    private bool isPaused;

    // Start is called before the first frame update
    void Start()
    {
        rbody = GetComponent<Rigidbody>();
        origin = rbody.position;
    }

    public override void OnEpisodeBegin()
    {
        isPaused = false;
        totalScore = 0;
        SetReward(0);
        transform.localPosition = origin;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position.x); // observe agent's own x position. no need to observe y and z because those are fixed.

        EnemyTankNew[] enemies = FindObjectsOfType<EnemyTankNew>();
        EnemyTankNew[] sortedEnemies = enemies.OrderBy(enemy => enemy.transform.position.z).ToArray();
        for (int i = 0; i < enemiesObserved; i++)
        {
            if (i < sortedEnemies.Length)
            {
                sensor.AddObservation(sortedEnemies[i].transform.position.x);
                sensor.AddObservation(sortedEnemies[i].transform.position.z);
            }
            else
            {
                sensor.AddObservation(0);
                sensor.AddObservation(30);
            }
        }

        // reward if agent is positioned between the 2 nearest enemies
        if (sortedEnemies.Length >= 2)
        {
            float maxEnemyX = Math.Max(sortedEnemies[0].transform.position.x, sortedEnemies[1].transform.position.x);
            float minEnemyX = Math.Min(sortedEnemies[0].transform.position.x, sortedEnemies[1].transform.position.x);

            if (transform.position.x >= minEnemyX && transform.position.x <= maxEnemyX)
            {
                AddReward(0.01f);
            }
        }
        

        FriendlyTankNew[] friendlies = FindObjectsOfType<FriendlyTankNew>();
        FriendlyTankNew[] sortedFriendlies = friendlies.OrderBy(friendly => friendly.transform.position.z).ToArray();
        for (int i = 0; i < friendliesObserved; i++)
        {
            if (i < sortedFriendlies.Length)
            {
                sensor.AddObservation(sortedFriendlies[i].transform.position.x);
                sensor.AddObservation(sortedFriendlies[i].transform.position.z);
            }
            else
            {
                sensor.AddObservation(0);
                sensor.AddObservation(30);
            }
        }
    }

    private void MoveX(float x)
    {
        float targetX = rbody.position.x + (x-1) * speed * Time.fixedDeltaTime; // move tank to desired position
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
                AddScore(2, Math.Min(2 * hit.distance / range, 1)); // reward for further kills
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.green); 
                hit.collider.gameObject.GetComponent<EnemyTankNew>().Hit();
            }
            else if (hit.collider.CompareTag("Friendly"))
            {   
                AddScore(-1, -0.3f);
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
        // // use left and right arrows to move
        // ActionSegment<float> continuousActionsOut = actionsOut.ContinuousActions;
        // continuousActionsOut[0] = Input.GetAxis("Horizontal");

        // use space to shoot
        ActionSegment<int> discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;

        // use arrow keys to move, but only have discrete actions
        if (Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(KeyCode.RightArrow))
        {
            discreteActionsOut[1] = 1;
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            discreteActionsOut[1] = 0;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            discreteActionsOut[1] = 2;
        }
        else
        {
            discreteActionsOut[1] = 1;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("EnemyAI"))
        {
            AddReward(-0.1f); // lose and restart game
            totalScore = int.MinValue; // for showing game over text
            StartCoroutine(PauseAndEndEpisode());
        }
        else if (collision.gameObject.CompareTag("Friendly"))
        {
            AddScore(2, 1);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {   
        if (isPaused) return;

        float x = actionBuffers.DiscreteActions[1];
        MoveX(x);

        int shoot = actionBuffers.DiscreteActions[0];
        if (shoot == 1)
        {
            Shoot();
        }
    }

    public string GetScore()
    {
        if (totalScore >= 20)
        {
            return "Victory! New round starting soon...";
        }
        else if (totalScore == int.MinValue)
        {
            return "Game Over! New round starting soon...";
        }

        return "Score: " + totalScore.ToString();
    }

    public void OnEnemyPassFrontline(float enemyX)
    {
        AddScore(-1, -Math.Min(0.5f * Math.Abs(enemyX - transform.position.x), 0.1f)); // penalize not getting close to the enemy and just letting it pass
    }

    public void OnFriendlyPassFrontline()
    {
        AddScore(0, -0.05f); // penalize for letting friendlies pass
    }

    public void AddScore(int score, float reward)
    {   
        totalScore += score;
        AddReward(reward);

        if (totalScore >= 20)
        {
            AddReward(5); // win game
            StartCoroutine(PauseAndEndEpisode());
        }
    }

    private IEnumerator PauseAndEndEpisode()
    {
        isPaused = true;
        tankNewSpawn.DestroyAllTanks();
        yield return new WaitForSeconds(3);
        EndEpisode();
    }
}