using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct movementData
{
    public float movementSpeed;
    public float audioVolume;
}

public class playerControls : MonoBehaviour
{
    public movementData[] movementTypes;
    public int currentType;
    public LayerMask hearingLayers;
    public GameObject soundShowing;
    Rigidbody2D rb;
    float prevScale = -1.0f;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        //Handle Inputs
        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.Space))
            currentType = 0;
        if (Input.GetKey(KeyCode.Space))
            currentType = 1;
        if (Input.GetKey(KeyCode.LeftShift))
            currentType = 2;
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * movementTypes[currentType].movementSpeed;
        if (rb.velocity.magnitude > 0)
        {
            if (prevScale != movementTypes[currentType].audioVolume)
            {
                soundShowing.transform.localScale = new Vector3(1, 1, 1) * movementTypes[currentType].audioVolume * 2 * (1.0f/0.9f);
                prevScale = movementTypes[currentType].audioVolume;
            }
            //soundShowing.Play();

            Collider2D[] enemies = Physics2D.OverlapCircleAll(new Vector2(transform.position.x, transform.position.y), movementTypes[currentType].audioVolume, hearingLayers);
            for (int i = 0; i < enemies.Length; i++)
            {
                //Debug.LogError(Mathf.Lerp(movementTypes[currentType].audioVolume, 0, (enemies[i].transform.position - transform.position).magnitude / movementTypes[currentType].audioVolume) + " " + (enemies[i].transform.position - transform.position).magnitude / movementTypes[currentType].audioVolume);
                enemies[i].transform.GetComponent<InfectedBrain>().hearNoise(
                    Mathf.Lerp(movementTypes[currentType].audioVolume, 0, (enemies[i].transform.position - transform.position).magnitude / movementTypes[currentType].audioVolume),
                    transform.position,
                    gameObject);
            }
        }
        else if (prevScale != -1)
        {
            soundShowing.transform.localScale = new Vector3(1, 1, 1);
            prevScale = -1;
        }
    }
}
