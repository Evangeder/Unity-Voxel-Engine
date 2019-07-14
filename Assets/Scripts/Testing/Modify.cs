using UnityEngine;
using System.Collections;

public class Modify : MonoBehaviour
{

    public GameObject CameraParent;


    Vector2 rot;
    public GameObject Cam;
    public Material PlaceBlockMat;
    float textureval = 0.25f;

    public GameObject PlaceBlockGO;
    public GameObject HandGO;

    public GameObject BuildMode_GO;
    public GameObject PlayMode_GO;
    public GameObject Minimap;
    public GameObject BuildModeCanvas;

    public World world;

    public UnityEngine.UI.Text BuildModeInfo;

    public GameObject SelectionVisualisationGO;

    public GameObject Bomb;

    int BlockID = 1;

    bool playmode = false;

    int buildmode = 0;
    int buildmode2brushsize = 0;

    float timer;

    void Start()
    {
        IniFile Screen_INI = new IniFile("/Mods/Screen.ini");
        if (Screen_INI.KeyExists("Resolution")) {
            string[] temp = Screen_INI.Read("Resolution").Split('x');
            Screen.SetResolution(int.Parse(temp[0]), int.Parse(temp[1]), bool.Parse(Screen_INI.Read("Fullscreen")));
        } else {
            Screen_INI.Write("Resolution", "854x480");
            Screen_INI.Write("Fullscreen", "False");
            Screen.SetResolution(854, 480, false);
        }

        //Application.targetFrameRate = 600;
        //QualitySettings.vSyncCount = 0;
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (timer > 0f) timer -= Time.deltaTime * 10f;

        if (!playmode)
        {

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                if (Input.GetKey(KeyCode.F1))
                {
                    HandGO.SetActive(false);
                    PlaceBlockGO.SetActive(true);
                    buildmode = 0;
                    BuildModeInfo.text = "<color=red><b><i>Mode: Block placement.</i></b></color>\nHold shift to place smooth blocks. [LMB] to delete [RMB] to place.\nMousewheel to change material.";
                }
                else if (Input.GetKey(KeyCode.F2))
                {
                    HandGO.SetActive(true);
                    PlaceBlockGO.SetActive(false);
                    buildmode = 1;
                    SelectionVisualisationGO.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                    BuildModeInfo.text = "<color=green><b><i>Mode: Editing smooth blocks.</i></b></color>\n 0.05f by default, 0.15f with shift, 0.25f with ctrl. [LMB] to decrease smoothness value [RMB] to increase.";
                }
                else if (Input.GetKey(KeyCode.F3))
                {
                    HandGO.SetActive(true);
                    PlaceBlockGO.SetActive(false);
                    buildmode = 2;
                    BuildModeInfo.text = "<color=blue><b><i>Mode: Smooth block brush.</i></b></color>\n 0.05f by default, 0.15f with shift, 0.25f with ctrl. [LMB] to decrease smoothness value [RMB] to increase.\nMousewheel to change size (for removal only)";
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    GameObject newBomb = Instantiate(Bomb);
                    newBomb.transform.localPosition = transform.localPosition;
                    newBomb.transform.rotation = transform.rotation;
                    newBomb.transform.Translate(Vector3.forward * 4f);
                    newBomb.transform.localRotation = transform.localRotation;
                    //newBomb.GetComponent<Rigidbody>().AddForce(new Vector3(5f, 5f, 0f), ForceMode.Impulse);
                    newBomb.GetComponent<Rigidbody>().AddRelativeForce(transform.forward * 20f, ForceMode.Impulse);
                }

                RaycastHit hit;
                if (Physics.Raycast(Cam.transform.position, Cam.transform.forward, out hit, 100))
                {
                    if (hit.transform.tag != "MapBorder")
                    {
                        SelectionVisualisationGO.SetActive(true);
                        WorldPos test = EditTerrain.GetBlockPos(hit, false);
                        SelectionVisualisationGO.transform.position = new Vector3(test.x, test.y, test.z);
                    }
                    else
                    {
                        SelectionVisualisationGO.SetActive(false);
                    }

                    if (Input.GetMouseButtonDown(1) && buildmode == 0)
                    {
                        if (Cam.transform.localRotation.x < 0.5f)
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

                    if (Input.GetMouseButton(1) && buildmode == 1 && timer <= 0f)
                    {
                        if (Cam.transform.localRotation.x < 0.5f)
                        {
                            Vector3 position = hit.point;
                            position += (hit.normal * 0.5f);
                            Block WorkerBlock = EditTerrain.GetBlock(hit, false);

                            if (WorkerBlock.GetID != 0 && WorkerBlock.Marched)
                            {
                                if (WorkerBlock.MarchedValue < 1f)
                                {
                                    if (Input.GetKey(KeyCode.LeftShift))
                                        WorkerBlock.MarchedValue += 0.15f;
                                    else if (Input.GetKey(KeyCode.LeftControl))
                                        WorkerBlock.MarchedValue += 0.25f;
                                    else
                                        WorkerBlock.MarchedValue += 0.05f;

                                    if (WorkerBlock.MarchedValue > 1f) WorkerBlock.MarchedValue = 1f;

                                    EditTerrain.SetBlock(hit, WorkerBlock, false, false);
                                    timer = 1f;
                                }
                            }
                        }
                    }

                    if (Input.GetMouseButton(0) && buildmode == 1 && timer <= 0f)
                    {
                        Block WorkerBlock = EditTerrain.GetBlock(hit, false);
                        if (WorkerBlock.GetID != 0 && WorkerBlock.Marched)
                        {
                            if (WorkerBlock.MarchedValue > 0.501f)
                            {
                                if (Input.GetKey(KeyCode.LeftShift) && WorkerBlock.MarchedValue > 0.75f)
                                    WorkerBlock.MarchedValue -= 0.15f;
                                else if (Input.GetKey(KeyCode.LeftControl))
                                    WorkerBlock.MarchedValue -= 0.25f;
                                else
                                    WorkerBlock.MarchedValue -= 0.05f;

                                if (WorkerBlock.MarchedValue < 0.501f) WorkerBlock.MarchedValue = 0.501f;
                                EditTerrain.SetBlock(hit, WorkerBlock, false, false);
                                timer = 1f;
                            }
                        }
                    }


                    if (Input.GetMouseButton(1) && buildmode == 2 && timer <= 0f)
                    {
                        if (Cam.transform.localRotation.x < 0.5f)
                        {
                            Vector3 position = hit.point;
                            Block WorkerBlock = EditTerrain.GetBlock(hit, false);

                            if (WorkerBlock.Marched || WorkerBlock.GetID == 0)
                            {
                                if (WorkerBlock.MarchedValue < 1f)
                                {
                                    if (Input.GetKey(KeyCode.LeftShift))
                                        WorkerBlock.MarchedValue += 0.15f;
                                    else if (Input.GetKey(KeyCode.LeftControl))
                                        WorkerBlock.MarchedValue += 0.25f;
                                    else
                                        WorkerBlock.MarchedValue += 0.05f;

                                    if (WorkerBlock.MarchedValue > 1f) WorkerBlock.MarchedValue = 1f;

                                    EditTerrain.SetBlock(hit, WorkerBlock, false, false);
                                    timer = 1f;
                                }
                                else if (WorkerBlock.MarchedValue > 0.5f && WorkerBlock.GetID == 0)
                                {
                                    WorkerBlock = BlockData.byID[1];
                                    WorkerBlock.Marched = true;
                                    WorkerBlock.MarchedValue = 0.501f;
                                    EditTerrain.SetBlock(hit, WorkerBlock, false, false);
                                }
                                else
                                {
                                    position += (hit.normal * 0.5f);
                                    hit.point = position;
                                    WorkerBlock.MarchedValue = 0.501f;
                                    EditTerrain.SetBlock(hit, WorkerBlock, false, false);
                                }
                            }
                            else if (WorkerBlock.GetID == 0)
                            {
                                position += (hit.normal * 0.5f);
                                hit.point = position;
                                WorkerBlock.MarchedValue = 0.501f;
                                EditTerrain.SetBlock(hit, WorkerBlock, false, false);
                            }
                        }
                    }

                    if (Input.GetMouseButton(0) && buildmode == 2 && timer <= 0f)
                    {
                        Block WorkerBlock = EditTerrain.GetBlock(hit, false);
                        Vector3 position = hit.point;
                        //position += (hit.normal * 0.5f);

                        if (buildmode2brushsize > 0)
                        {
                            for (int x = -buildmode2brushsize; x <= buildmode2brushsize; x++)
                            {
                                for (int y = -buildmode2brushsize; y <= buildmode2brushsize; y++)
                                {
                                    for (int z = -buildmode2brushsize; z <= buildmode2brushsize; z++)
                                    {
                                        hit.point = new Vector3(position.x + x, position.y + y, position.z + z);
                                        WorkerBlock = EditTerrain.GetBlock(hit, false);

                                        float calculatedmarch = 0.15f - x / 50 - y / 50 - z / 50;
                                        if (calculatedmarch < 0.1f) calculatedmarch = 0.1f;

                                        if (WorkerBlock.GetID == 0 || WorkerBlock.Marched)
                                        {

                                            if (WorkerBlock.MarchedValue > 0f)
                                            {
                                                if (Input.GetKey(KeyCode.LeftShift) && WorkerBlock.MarchedValue > 0.75f)
                                                    WorkerBlock.MarchedValue -= calculatedmarch;
                                                else if (Input.GetKey(KeyCode.LeftControl))
                                                    WorkerBlock.MarchedValue -= 0.25f;
                                                else
                                                    WorkerBlock.MarchedValue -= 0.05f;

                                                if (WorkerBlock.MarchedValue < 0f) WorkerBlock.MarchedValue = 0f;

                                                if (WorkerBlock.MarchedValue < 0.501f)
                                                {
                                                    float tempmarch = WorkerBlock.MarchedValue;
                                                    WorkerBlock = BlockData.byID[0];
                                                    WorkerBlock.MarchedValue = tempmarch;
                                                    EditTerrain.SetBlock(hit, WorkerBlock, false, false);
                                                }
                                                else
                                                {
                                                    EditTerrain.SetBlock(hit, WorkerBlock, false, false);
                                                }

                                                timer = 1f;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (WorkerBlock.GetID != 0 && WorkerBlock.Marched)
                            {
                                if (WorkerBlock.MarchedValue > 0.501f)
                                {
                                    if (Input.GetKey(KeyCode.LeftShift) && WorkerBlock.MarchedValue > 0.75f)
                                        WorkerBlock.MarchedValue -= 0.15f;
                                    else if (Input.GetKey(KeyCode.LeftControl))
                                        WorkerBlock.MarchedValue -= 0.25f;
                                    else
                                        WorkerBlock.MarchedValue -= 0.05f;

                                    if (WorkerBlock.MarchedValue < 0.501f) WorkerBlock.MarchedValue = 0.501f;
                                    EditTerrain.SetBlock(hit, WorkerBlock, false, false);
                                    timer = 1f;
                                }
                                else
                                {
                                    EditTerrain.SetBlock(hit, BlockData.byID[0], false, false);
                                }
                            }
                        }
                    }


                    if (Input.GetMouseButtonDown(0) && buildmode == 0)
                    {
                        WorldPos blockpos = EditTerrain.GetBlockPos(hit);
                        if (world.GetBlock(blockpos.x, blockpos.y + 1, blockpos.z).CustomBlock)
                            world.SetBlock(blockpos.x, blockpos.y + 1, blockpos.z, BlockData.byID[0]);
                        else
                            EditTerrain.SetBlock(hit, BlockData.byID[0], false, true);
                    }
                }
                else
                {
                    SelectionVisualisationGO.SetActive(false);
                }



                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    rot = new Vector2(
                        rot.x + Input.GetAxis("Mouse X") * 3,
                        rot.y + Input.GetAxis("Mouse Y") * 3);

                    float MouseSensitivity = 3f;

                    CameraParent.transform.localRotation *= Quaternion.AngleAxis(Input.GetAxis("Mouse X") * MouseSensitivity, Vector3.up);
                    transform.localRotation *= Quaternion.AngleAxis(Input.GetAxis("Mouse Y") * MouseSensitivity, Vector3.left);

                    float speed = 10f;
                    if (Input.GetKey(KeyCode.LeftShift)) speed = 20f;
                    if (Input.GetKey(KeyCode.LeftControl)) speed = 30f;

                    if (Input.GetKey(KeyCode.W)) CameraParent.transform.Translate(Vector3.forward * Time.deltaTime * speed);
                    if (Input.GetKey(KeyCode.S)) CameraParent.transform.Translate(Vector3.back * Time.deltaTime * speed);
                    if (Input.GetKey(KeyCode.A)) CameraParent.transform.Translate(Vector3.left * Time.deltaTime * speed);
                    if (Input.GetKey(KeyCode.D)) CameraParent.transform.Translate(Vector3.right * Time.deltaTime * speed);
                    if (Input.GetKey(KeyCode.E)) CameraParent.transform.Translate(Vector3.up * Time.deltaTime * speed);
                    if (Input.GetKey(KeyCode.Q)) CameraParent.transform.Translate(Vector3.down * Time.deltaTime * speed);
                }

                if (Input.GetAxis("Mouse ScrollWheel") > 0f && buildmode == 0)
                { // forward
                    if (BlockID == 1)
                    {
                        BlockID = BlockData.byID.Count - 1;
                    }
                    else
                    {
                        BlockID -= 1;
                    }
                    PlaceBlockMat.SetTextureScale("_BaseColorMap", new Vector2(BlockData.BlockTileSize, BlockData.BlockTileSize));
                    PlaceBlockMat.SetTextureOffset("_UnlitColorMap", new Vector2(BlockData.byID[BlockID].Texture_Marched.x * BlockData.BlockTileSize, BlockData.byID[BlockID].Texture_Marched.y * BlockData.BlockTileSize));
                }
                else if (Input.GetAxis("Mouse ScrollWheel") < 0f && buildmode == 0)
                { // backwards
                    if (BlockID == BlockData.byID.Count - 1)
                    {
                        BlockID = 1;
                    }
                    else
                    {
                        BlockID += 1;
                    }
                    PlaceBlockMat.SetTextureScale("_UnlitColorMap", new Vector2(BlockData.BlockTileSize, BlockData.BlockTileSize));
                    PlaceBlockMat.SetTextureOffset("_UnlitColorMap", new Vector2(BlockData.byID[BlockID].Texture_Marched.x * BlockData.BlockTileSize, BlockData.byID[BlockID].Texture_Marched.y * BlockData.BlockTileSize));
                }

                if (Input.GetAxis("Mouse ScrollWheel") > 0f && buildmode == 2)
                { // forward
                    if (buildmode2brushsize > 0)
                    {
                        buildmode2brushsize -= 1;
                        if (buildmode2brushsize == 0)
                            SelectionVisualisationGO.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                        else
                            SelectionVisualisationGO.transform.localScale = new Vector3(buildmode2brushsize * 3 + 0.2f, buildmode2brushsize * 3 + 0.2f, buildmode2brushsize * 3 + 0.2f);
                    }
                }
                else if (Input.GetAxis("Mouse ScrollWheel") < 0f && buildmode == 2)
                { // backwards
                    if (buildmode2brushsize < 4)
                    {
                        buildmode2brushsize += 1;
                        SelectionVisualisationGO.transform.localScale = new Vector3(buildmode2brushsize * 3 + 0.2f, buildmode2brushsize * 3 + 0.2f, buildmode2brushsize * 3 + 0.2f);
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                BuildMode_GO.SetActive(false);
                PlayMode_GO.SetActive(true);
                BuildModeCanvas.SetActive(false);
                Minimap.SetActive(true);
                SelectionVisualisationGO.SetActive(false);
                playmode = true;
                CameraParent.GetComponent<Rigidbody>().isKinematic = false;
                CameraParent.GetComponent<Rigidbody>().useGravity = true;
            }
        } else {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                rot = new Vector2(
                    rot.x + Input.GetAxis("Mouse X") * 3,
                    rot.y + Input.GetAxis("Mouse Y") * 3);

                float MouseSensitivity = 3f;

                CameraParent.transform.localRotation *= Quaternion.AngleAxis(Input.GetAxis("Mouse X") * MouseSensitivity, Vector3.up);
                transform.localRotation *= Quaternion.AngleAxis(Input.GetAxis("Mouse Y") * MouseSensitivity, Vector3.left);

                float speed = 5f;
                if (Input.GetKey(KeyCode.LeftShift)) speed = 10f;

                if (Input.GetKey(KeyCode.W)) CameraParent.transform.Translate(Vector3.forward * Time.deltaTime * speed);
                if (Input.GetKey(KeyCode.S)) CameraParent.transform.Translate(Vector3.back * Time.deltaTime * speed);
                if (Input.GetKey(KeyCode.A)) CameraParent.transform.Translate(Vector3.left * Time.deltaTime * speed);
                if (Input.GetKey(KeyCode.D)) CameraParent.transform.Translate(Vector3.right * Time.deltaTime * speed);

                if (Input.GetKeyDown(KeyCode.Space)) CameraParent.GetComponent<Rigidbody>().AddForce(Vector3.up * 150f, ForceMode.Impulse);
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                BuildMode_GO.SetActive(true);
                PlayMode_GO.SetActive(false);
                BuildModeCanvas.SetActive(true);
                Minimap.SetActive(false);
                playmode = false;
                CameraParent.GetComponent<Rigidbody>().isKinematic = true;
                CameraParent.GetComponent<Rigidbody>().useGravity = false;
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