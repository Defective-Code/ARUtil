using UnityEngine;

public class ARSpinningSignsConfig : MonoBehaviour
{

    public float radius;

    public float speed = 1.0f; // amount of degrees to rotate around by per second

    public GameObject sign1;
    public GameObject sign2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // set signs either side of anchor
        var originalLPosition1 = sign1.transform.localPosition;
        var originalLPosition2 = sign2.transform.localPosition;

        //sign1.transform.localPosition = new Vector3(-radius, 0, 0);
        //sign2.transform.localPosition = new Vector3(radius, 0, 0);
        sign1.transform.localPosition = new Vector3(-radius, originalLPosition1.y, originalLPosition1.z);
        sign2.transform.localPosition = new Vector3(radius, originalLPosition2.y, originalLPosition2.z);
        
    }

    // Update is called once per frame
    void Update()
    {
        sign1.transform.RotateAround(transform.position, Vector3.up, speed * Time.deltaTime);
        sign2.transform.RotateAround(transform.position, Vector3.up, speed * Time.deltaTime);

        //Vector3 targetDirection1 = Camera.main.transform.position - sign1.transform.position;
        //Vector3 newDirection1 = Vector3.RotateTowards(sign1.transform.forward, targetDirection1, -0.1f * Time.deltaTime, 0.0f);
        //sign1.transform.rotation = Quaternion.LookRotation(newDirection1);


        //Vector3 targetDirection2 = Camera.main.transform.position - sign2.transform.position;
        //Vector3 newDirection2 = Vector3.RotateTowards(sign2.transform.forward, targetDirection2, -0.1f * Time.deltaTime, 0.0f);
        //sign2.transform.rotation = Quaternion.LookRotation(newDirection2);

        Transform toLookAt = Camera.main.transform;
        toLookAt.position = new Vector3(toLookAt.position.x, transform.position.y, toLookAt.position.z); // set this prefab and the to look at positions y the same so the signs are not look up or down

        sign1.transform.LookAt(toLookAt);
        sign2.transform.LookAt(toLookAt);


        // so the signs face the correct way and are visible. If this is not here, the normals face the other direction away from the camera 
        sign1.transform.Rotate(0f, 180f, 0, Space.Self);
        sign2.transform.Rotate(0f, 180f, 0, Space.Self);
    }
}
