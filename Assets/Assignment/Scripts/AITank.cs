using System;
using Unity.MLAgents.Extensions.Sensors;
using Unity.Sentis.Layers;
using UnityEngine;

public class AITank : MonoBehaviour
{
    private const float speed = 20f;
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

    void FixedUpdate()
    {
        MoveX(-0.5f);
    }
}