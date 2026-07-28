using UnityEngine;

public class ImageTarget : MonoBehaviour
{
    public Mesh toDraw;

    [SerializeField]
    public float imageWidth = 0.22f;
    [SerializeField]
    public float imageHeight = 0.22f;

    [SerializeField]
    public int tagID = 0;

    public Texture2D imageTex;

    void OnValidate()
    {
        //transform.localScale = new Vector3(imageWidth, imageHeight, 1);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation * Quaternion.Euler(0, 0, 0);

        Gizmos.DrawMesh(toDraw, position, rotation, new Vector3(imageWidth, 1, imageHeight) / 10); // divide by 10 because the default size of a plane in Unity is 10mx10m when the scale is 1,1,1. So if we want a size matching the physical world at scale 1,1,1, we have to divide the visualization scale by 10.
        //Gizmos.Draw
    }
}
