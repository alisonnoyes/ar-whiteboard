using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadImage : MonoBehaviour
{
    public Material[] materials;
    Renderer renderer;

    // Images of other boards to project over
    private string[] history = new string[5];

    // Start is called before the first frame update
    void Start()
    {
        renderer = GetComponent<Renderer>();
        renderer.enabled = true;
        renderer.sharedMaterial = materials[0];

        history[0] = "test";
    }

    // Update is called once per frame
    void Update()
    {
        // Overlay an image on top of the board
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.tag == "load_image")
                {
                    // Resize the board to match the locations of the QR codes
                    GameObject topLeft = GameObject.FindGameObjectWithTag("top_left");
                    GameObject bottomLeft = GameObject.FindGameObjectWithTag("bottom_left");
                    GameObject bottomRight = GameObject.FindGameObjectWithTag("bottom_right");

                    Vector3 topLeftPosition = topLeft.transform.position;
                    Vector3 bottomRightPosition = bottomRight.transform.position;
                    Vector3 bottomLeftPosition = bottomLeft.transform.position;

                    float boardWidth = bottomRightPosition.x - topLeftPosition.x;
                    float boardHeight = topLeftPosition.y - bottomLeftPosition.y;

                    Vector3 boardCentroid = new Vector3(
                        boardWidth / 2 + topLeftPosition.x,
                        boardHeight / 2 + bottomLeftPosition.y,
                        topLeftPosition.z);

                    GameObject board = GameObject.FindGameObjectWithTag("blackboard_image");
                    board.transform.position = boardCentroid;

                    // Project the image
                    renderer.sharedMaterial = materials[1];
                    Texture2D texture = Resources.Load(history[0]) as Texture2D;
                    renderer.material.mainTexture = texture;
                }
            }
        }
    }
}
