using UnityEngine;
using TMPro;

public class Scorecount : MonoBehaviour
{
    [SerializeField] private TMP_Text score;
    [SerializeField] private AITank aiTank;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        score.text = "Score: " + aiTank.GetScore().ToString();
    }
}