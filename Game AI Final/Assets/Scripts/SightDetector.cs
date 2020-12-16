using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SightDetector : MonoBehaviour
{
    public bool canSeePlayer;

    public float viewConeAngle;
    public float viewConeDistance;
    public float viewPeripheralRadius;
    public LayerMask selfFilter;
    public LayerMask playerLayer;
    public Transform peripheralCircle;
    public Image viewCone;

    public GameObject player;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        peripheralCircle.localScale *= viewPeripheralRadius * 2;
        peripheralCircle.localPosition *= viewPeripheralRadius;
        viewCone.rectTransform.localScale *= viewConeDistance * 2;
        viewCone.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, viewConeAngle*.5f);
        viewCone.fillAmount = viewConeAngle/360;
    }

    // Update is called once per frame
    void Update()
    {
        TestForLineOfSight();   
    }

    void TestForLineOfSight()
    {
        canSeePlayer = false;

        Vector3 dir = player.transform.position - transform.position;

        Collider2D test = Physics2D.OverlapCircle(transform.up * viewPeripheralRadius + transform.position, viewPeripheralRadius, playerLayer);
        if (test != null)
        {
            /*RaycastHit2D[] results = new RaycastHit2D[5];
            int size = player.GetComponent<CircleCollider2D>().Cast(-dir, results);
            for (int i = 0; i < size; i++)
            {
                if (results[i].transform != null && results[i].transform.gameObject == transform.gameObject)
                {
                    canSeePlayer = true;
                    break;
                }
            }*/
            RaycastHit2D hit = Physics2D.Linecast(transform.position, player.transform.position, selfFilter);
            if (hit != null && hit.transform.gameObject == player) canSeePlayer = true;
        }

        if (!canSeePlayer)
        {
            float angle = Vector2.Angle(transform.up, dir);
            float altAngle = angle - 90;
            if (angle < 0) altAngle = + 180;
            altAngle = Vector2.Angle(transform.up, dir + new Vector3(Mathf.Cos(altAngle), Mathf.Sin(altAngle), 0));
            if (dir.magnitude <= viewConeDistance + .45f 
                && (Mathf.Abs(angle) <= viewConeAngle/2 || Mathf.Abs(altAngle) <= viewConeAngle / 2))
            {
                /*RaycastHit2D[] results = new RaycastHit2D[5];
                int size = player.GetComponent<CircleCollider2D>().Cast(-dir, results);
                for(int i = 0; i <size; i++)
                {
                    if(results[i].transform != null && results[i].transform.gameObject == transform.gameObject)
                    {
                        Debug.Log(size);
                        canSeePlayer = true;
                        break;
                    }
                }*/
                RaycastHit2D hit = Physics2D.Linecast(transform.position, player.transform.position, selfFilter);
                if (hit != null && hit.transform.gameObject == player) canSeePlayer = true;
                
            }
        }
    }
}
