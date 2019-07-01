using UnityEngine;
using System.Collections;

public class Modify : MonoBehaviour
{

    Vector2 rot;
    public GameObject Cam;
    public Material PlaceBlockMat;
    float textureval = 0.25f;

    public GameObject PlaceBlockGO;
    public World world;

    int BlockID = 1;

    void Start()
    {
        Screen.SetResolution(854, 480, false);
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {

            if (Input.GetMouseButtonDown(1))
            {
                RaycastHit hit;
                if (Cam.transform.localRotation.x < 0.5f)
                {
                    if (Physics.Raycast(Cam.transform.position, Cam.transform.forward, out hit, 100))
                    {
                        Vector3 position = hit.point;
                        position += (hit.normal * 0.5f);
                        hit.point = position;

                        if (Input.GetKey(KeyCode.LeftShift))
                        {
                            Block WorkerBlock = BlockData.byID[BlockID];
                            WorkerBlock.Marched = true;
                            WorkerBlock.Solid = false;
                            EditTerrain.SetBlock(hit, WorkerBlock, false, false);
                        }
                        else
                        {
                            EditTerrain.SetBlock(hit, BlockData.byID[BlockID], false, false);

                        }

                    }
                }

            }

            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                if (Physics.Raycast(Cam.transform.position, Cam.transform.forward, out hit, 100))
                {
                    EditTerrain.SetBlock(hit, BlockData.byID[0], false, true);
                }
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                rot = new Vector2(
                    rot.x + Input.GetAxis("Mouse X") * 3,
                    rot.y + Input.GetAxis("Mouse Y") * 3);

                transform.localRotation = Quaternion.AngleAxis(rot.x, Vector3.up);
                transform.localRotation *= Quaternion.AngleAxis(rot.y, Vector3.left);

                float speed = 0.2f;
                if (Input.GetKey(KeyCode.LeftShift)) speed = 0.7f;
                if (Input.GetKey(KeyCode.LeftControl)) speed = 1.2f;

                transform.position += transform.forward * speed * Input.GetAxis("Vertical");
                transform.position += transform.right * speed * Input.GetAxis("Horizontal");
            }
            
            if (Input.GetAxis("Mouse ScrollWheel") > 0f)
            { // forward
                if (BlockID == 1)
                {
                    BlockID = BlockData.byID.Count -1;
                }
                else
                {
                    BlockID -= 1;
                }
                PlaceBlockMat.SetTextureScale("_BaseColorMap", new Vector2(BlockData.BlockTileSize, BlockData.BlockTileSize));
                PlaceBlockMat.SetTextureOffset("_BaseColorMap", new Vector2(BlockData.byID[BlockID].Texture_Up.x * BlockData.BlockTileSize, BlockData.byID[BlockID].Texture_Up.y * BlockData.BlockTileSize));
            }
            else if (Input.GetAxis("Mouse ScrollWheel") < 0f)
            { // backwards
                if (BlockID == BlockData.byID.Count - 1)
                {
                    BlockID = 1;
                }
                else
                {
                    BlockID += 1;
                }
                PlaceBlockMat.SetTextureScale("_BaseColorMap", new Vector2(BlockData.BlockTileSize, BlockData.BlockTileSize));
                PlaceBlockMat.SetTextureOffset("_BaseColorMap", new Vector2(BlockData.byID[BlockID].Texture_Up.x * BlockData.BlockTileSize, BlockData.byID[BlockID].Texture_Up.y * BlockData.BlockTileSize));
            }
        }

        if (Input.GetKeyDown("escape"))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Input.GetMouseButton(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}