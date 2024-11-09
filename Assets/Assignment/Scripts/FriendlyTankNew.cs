using UnityEngine;

public class FriendlyTankNew : MonoBehaviour
{
    public float speed;
    public GameObject AI;

    private AITank aiTank;

    // private Rigidbody rbody;
    // private GameObject enemy;

    private const float posThreshold = -37f;

    void Start()
    {
        // rbody = GetComponent<Rigidbody>();
        // enemy = GetComponent<GameObject>();
        aiTank = FindObjectOfType<AITank>();
    }

    void Update()
    {
        transform.localPosition += transform.forward * Time.deltaTime * speed;

        if (transform.localPosition.z < posThreshold)
        {
            aiTank.OnFriendlyPassFrontline();
            Destroy(gameObject);
        }
    }

    public void Hit()
    {
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "Tank")
        {
            Destroy(gameObject);
        }
    }
}
