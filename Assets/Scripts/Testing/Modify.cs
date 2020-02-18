using UnityEngine;
using System.Collections;
using System.IO;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AudioSource))]
public class Modify : MonoBehaviour
{
    private World_Network worldnetwork;

    public GameObject CameraParent;

    Vector2 rot;
    public GameObject Cam;
    public GameObject CanvasObject;
    public GameObject PlaceBlockGO;
    public GameObject HandGO;
    public GameObject BuildMode_GO;
    public GameObject PlayMode_GO;
    public GameObject Options_GO;
    public GameObject IngameUI;
    public GameObject BuildModeCanvas;
    public GameObject SelectionVisualisationGO;
    public GameObject SchemaSelector1;
    public GameObject SchemaSelector2;
    public GameObject SchemaSelectorOrigin;
    public GameObject Bomb;
    public UnityEngine.UI.Text BuildModeInfo;
    public UnityEngine.UI.Text ErrorLogBuildmode;
    public Material PlaceBlockMat;

    public AudioSource _audioSource;
    
    public World world;

    public AttunementAnimations attunementAnimations;

    ushort BlockID = 1;

    bool playmode = false;

    int buildmode = 0;
    int buildmode2brushsize = 0;

    float timer, doubleTapSpacebarTimer, doubleTapWTimer, flyspeed = 5f;
    bool flymode = true, Loaded = false;

    bool nouimode = false;

    bool dontfocus = false;

    int3 SchemaSelection1, SchemaSelection2, SchemaSelectionOrigin;

    void Awake()
    {
        attunementAnimations = GameObject.Find("Attunements").GetComponent<AttunementAnimations>();
        IngameUI = GameObject.Find("Ingame UI");
        BuildModeInfo = GameObject.Find("BuildModeInfo").GetComponent<UnityEngine.UI.Text>();
        ErrorLogBuildmode = GameObject.Find("ErrorLog").GetComponent<UnityEngine.UI.Text>();
        Options_GO = GameObject.Find("Settings UI");
        Schematics.ErrorLogBuildmode = ErrorLogBuildmode;
        world = GameObject.Find("World").GetComponent<World>();
        SelectionVisualisationGO = GameObject.Find("Selection");
        SchemaSelector1 = GameObject.Find("SchematicSelectionCorner1");
        SchemaSelector2 = GameObject.Find("SchematicSelectionCorner2");
        SchemaSelectorOrigin = GameObject.Find("SchematicSelectionOrigin");
        BuildModeCanvas = GameObject.Find("BuildMode UI");
        Options_GO.SetActive(false);
        IngameUI.SetActive(false);
        SelectionVisualisationGO.SetActive(false);
        SchemaSelector1.SetActive(false);
        SchemaSelector2.SetActive(false);
        SchemaSelectorOrigin.SetActive(false);
        CanvasObject = GameObject.Find("Canvas");
        CanvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        _audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        worldnetwork = GameObject.Find("World").GetComponent<World_Network>();

        IniFile Screen_INI = new IniFile("/Mods/Screen.ini");
        if (Screen_INI.KeyExists("Resolution")) {
            string[] temp = Screen_INI.Read("Resolution").Split('x');
            Screen.SetResolution(int.Parse(temp[0]), int.Parse(temp[1]), bool.Parse(Screen_INI.Read("Fullscreen")));
        } else {
            Screen_INI.Write("Resolution", "854x480");
            Screen_INI.Write("Fullscreen", "False");
            Screen.SetResolution(854, 480, false);
        }
        Application.targetFrameRate = 600;
        QualitySettings.vSyncCount = 1;
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (world.Loaded && !Loaded)
        {
            Loaded = true;
            flymode = false;
            CameraParent.GetComponent<Rigidbody>().isKinematic = false;
            CameraParent.GetComponent<Rigidbody>().useGravity = true;
        }
        
        if (timer > 0f) timer -= Time.deltaTime * 10f;
        doubleTapSpacebarTimer += Time.deltaTime;
        doubleTapWTimer += Time.deltaTime;
        if (!Options_GO.activeSelf && dontfocus) dontfocus = false;

        if (Input.GetKeyDown(KeyCode.O))
        {
            if (dontfocus)
            {
                Options_GO.SetActive(false);
                dontfocus = false;
            }
            else
            {
                dontfocus = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Options_GO.SetActive(true);
            }
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            if (CanvasObject.activeSelf)
            {
                nouimode = true;
                CanvasObject.SetActive(false);
                HandGO.SetActive(false);
                PlaceBlockGO.SetActive(false);
                BuildMode_GO.SetActive(false);
                PlayMode_GO.SetActive(false);
                BuildModeCanvas.SetActive(false);
                IngameUI.SetActive(false);
                playmode = false;
                CameraParent.GetComponent<Rigidbody>().isKinematic = true;
                CameraParent.GetComponent<Rigidbody>().useGravity = false;
                CanvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            }
            else
            {
                nouimode = false;
                CanvasObject.SetActive(true);
                buildmode = 0;
                HandGO.SetActive(false);
                PlaceBlockGO.SetActive(true);

                BuildMode_GO.SetActive(true);
                PlayMode_GO.SetActive(false);
                BuildModeCanvas.SetActive(true);
                IngameUI.SetActive(false);
                playmode = false;
                CameraParent.GetComponent<Rigidbody>().isKinematic = true;
                CameraParent.GetComponent<Rigidbody>().useGravity = false;
                CanvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }

        if (!nouimode)
        {
            if (!playmode)
            {

                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    if (Input.GetKeyDown(KeyCode.F1))
                    {
                        HandGO.SetActive(false);
                        PlaceBlockGO.SetActive(true);
                        buildmode = 0;
                        BuildModeInfo.text = "<color=red><b><i>Mode: Block placement.</i></b></color>\nHold shift to place smooth blocks. [LMB] to delete [RMB] to place.\nMousewheel to change material.";
                    }
                    else if (Input.GetKeyDown(KeyCode.F2))
                    {
                        HandGO.SetActive(true);
                        PlaceBlockGO.SetActive(false);
                        buildmode = 1;
                        SelectionVisualisationGO.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                        BuildModeInfo.text = "<color=green><b><i>Mode: Editing smooth blocks.</i></b></color>\n 0.05f by default, 0.15f with shift, 0.25f with ctrl. [LMB] to decrease smoothness value [RMB] to increase.";
                    }
                    else if (Input.GetKeyDown(KeyCode.F3))
                    {
                        HandGO.SetActive(true);
                        PlaceBlockGO.SetActive(false);
                        buildmode = 2;
                        BuildModeInfo.text = "<color=blue><b><i>Mode: Smooth block brush.</i></b></color>\n 0.05f by default, 0.15f with shift, 0.25f with ctrl. [LMB] to decrease smoothness value [RMB] to increase.\nMousewheel to change size (for removal only)";
                    }
                    else if (Input.GetKeyDown(KeyCode.F4))
                    {
                        if (buildmode == 3)
                        {
                            SchemaSelector1.SetActive(false);
                            SchemaSelector2.SetActive(false);
                            SchemaSelectorOrigin.SetActive(false);
                        }
                        HandGO.SetActive(true);
                        PlaceBlockGO.SetActive(false);
                        buildmode = 3;
                        BuildModeInfo.text = "<color=yellow><b><i>Mode: Schematic exporting</i></b></color>\n [LMB] To select first corner [RMB] to select second corner\n[MMB] to select point of origin.";
                    }
                    else if (Input.GetKeyDown(KeyCode.F5))
                    {
                        SchemaSelector1.SetActive(false);
                        SchemaSelector2.SetActive(false);
                        SchemaSelectorOrigin.SetActive(false);
                        HandGO.SetActive(true);
                        PlaceBlockGO.SetActive(false);
                        buildmode = 4;
                        BuildModeInfo.text = "<color=yellow><b><i>Mode: Schematic importing</i></b></color>\n [RMB] To paste schematic.";
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
                            int3 test = EditTerrain.GetBlockPos(hit, false);
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
                                    BlockMetadata WorkerBlock = new BlockMetadata(BlockID, BlockSwitches.Marched, 254);
                                    if (BlockData.BlockSounds[BlockID].Count > 0)
                                        _audioSource.PlayOneShot(BlockData.BlockSounds[BlockID][Random.Range(0, BlockData.BlockSounds[BlockID].Count - 1)]);
                                    EditTerrain.SetBlock(hit, WorkerBlock, false, false);
                                }
                                else
                                {
                                    if (BlockData.BlockSounds[BlockID].Count > 0)
                                        _audioSource.PlayOneShot(BlockData.BlockSounds[BlockID][Random.Range(0, BlockData.BlockSounds[BlockID].Count - 1)]);
                                    EditTerrain.SetBlock(hit, new BlockMetadata(BlockID, BlockSwitches.None, 0), false, false);
                                }
                            }

                        }

                        if (buildmode == 3)
                            if (Input.GetMouseButtonDown(0))
                            {
                                SchemaSelector1.SetActive(true);
                                int3 test = EditTerrain.GetBlockPos(hit, false);
                                SchemaSelector1.transform.position = new Vector3(test.x, test.y, test.z);
                                SchemaSelection1 = new int3(test.x, test.y, test.z);
                            }
                            else if (Input.GetMouseButtonDown(1))
                            {
                                SchemaSelector2.SetActive(true);
                                int3 test = EditTerrain.GetBlockPos(hit, false);
                                SchemaSelector2.transform.position = new Vector3(test.x, test.y, test.z);
                                SchemaSelection2 = new int3(test.x, test.y, test.z);
                            }
                            else if (Input.GetMouseButtonDown(2))
                            {
                                SchemaSelectorOrigin.SetActive(true);
                                int3 test = EditTerrain.GetBlockPos(hit, false);
                                SchemaSelectorOrigin.transform.position = new Vector3(test.x, test.y, test.z);
                                SchemaSelectionOrigin = new int3(test.x, test.y, test.z);
                            }
                            else if (Input.GetKeyDown(KeyCode.Return))
                            {
                                if (!SchemaSelector1.activeSelf)
                                {
                                    ErrorLogBuildmode.text = "Missing corner selector (red).";
                                    Color col = ErrorLogBuildmode.color;
                                    col.r = 1f; col.g = 0f; col.b = 0f;
                                    col.a = 1f;
                                    ErrorLogBuildmode.color = col;
                                }
                                if (!SchemaSelector2.activeSelf)
                                {
                                    ErrorLogBuildmode.text = "Missing corner selector (green)";
                                    Color col = ErrorLogBuildmode.color;
                                    col.r = 1f; col.g = 0f; col.b = 0f;
                                    col.a = 1f;
                                    ErrorLogBuildmode.color = col;
                                }
                                if (!SchemaSelectorOrigin.activeSelf)
                                {
                                    ErrorLogBuildmode.text = "Missing origin selector (blue)";
                                    Color col = ErrorLogBuildmode.color;
                                    col.r = 1f; col.g = 0f; col.b = 0f;
                                    col.a = 1f;
                                    ErrorLogBuildmode.color = col;
                                }

                                if (SchemaSelector1.activeSelf && SchemaSelector2.activeSelf && SchemaSelectorOrigin.activeSelf)
                                {
                                    StartCoroutine(Schematics.CopySchematicCoroutine(SchemaSelection1, SchemaSelection2, SchemaSelectionOrigin));
                                }
                            }

                        if (buildmode == 4)
                            if (Input.GetMouseButtonDown(1))
                            {
                                StartCoroutine(Schematics.PasteSchematicCoroutine(hit));
                            }

                        if (Input.GetMouseButton(1) && buildmode == 1 && timer <= 0f)
                        {
                            if (Cam.transform.localRotation.x < 0.5f)
                            {
                                Vector3 position = hit.point;
                                position += (hit.normal * 0.5f);
                                BlockMetadata WorkerBlock = EditTerrain.GetBlock(hit, false);

                                
                                if (WorkerBlock.ID != 0 && (WorkerBlock.Switches & BlockSwitches.Marched) == BlockSwitches.Marched)
                                {
                                    if (WorkerBlock.GetMarchedValue() < 1f)
                                    {
                                        byte newmarchval = WorkerBlock.MarchedValue;
                                        if (Input.GetKey(KeyCode.LeftShift))
                                            WorkerBlock.MarchedValue = (byte)(WorkerBlock.MarchedValue + 13 > 254 ? 254 : WorkerBlock.MarchedValue + 13);
                                        else if (Input.GetKey(KeyCode.LeftControl))
                                            WorkerBlock.MarchedValue = (byte)(WorkerBlock.MarchedValue + 38 > 254 ? 254 : WorkerBlock.MarchedValue + 38);
                                        else
                                            WorkerBlock.MarchedValue = (byte)(WorkerBlock.MarchedValue + 1 > 254 ? 254 : WorkerBlock.MarchedValue + 1);

                                        EditTerrain.SetBlock(hit, WorkerBlock, false, false);
                                        timer = 1f;
                                    }
                                }
                            }
                        }

                        if (Input.GetMouseButton(0) && buildmode == 1 && timer <= 0f)
                        {
                            BlockMetadata WorkerBlock = EditTerrain.GetBlock(hit, false);
                            if (WorkerBlock.ID != 0 && (WorkerBlock.Switches & BlockSwitches.Marched) == BlockSwitches.Marched)
                            {
                                if (WorkerBlock.GetMarchedValue() > 0.501f)
                                {
                                    if (Input.GetKey(KeyCode.LeftShift))
                                        WorkerBlock.MarchedValue = (byte)(WorkerBlock.MarchedValue - 13 < 130 ? 130 : WorkerBlock.MarchedValue - 13);
                                    else if (Input.GetKey(KeyCode.LeftControl))
                                        WorkerBlock.MarchedValue = (byte)(WorkerBlock.MarchedValue - 38 < 130 ? 130 : WorkerBlock.MarchedValue - 38);
                                    else
                                        WorkerBlock.MarchedValue = (byte)(WorkerBlock.MarchedValue - 1 < 130 ? 130 : WorkerBlock.MarchedValue - 1);

                                    if (WorkerBlock.MarchedValue < 128) WorkerBlock.MarchedValue = 128;
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
                                BlockMetadata WorkerBlock = EditTerrain.GetBlock(hit, false);

                                if ((WorkerBlock.Switches & BlockSwitches.Marched) == BlockSwitches.Marched || WorkerBlock.ID == 0)
                                {
                                    if (WorkerBlock.GetMarchedValue() < 1f)
                                    {
                                        if (Input.GetKey(KeyCode.LeftShift))
                                            WorkerBlock.MarchedValue = (byte)(WorkerBlock.MarchedValue + 38 > 254 ? 254 : WorkerBlock.MarchedValue + 38);
                                        else if (Input.GetKey(KeyCode.LeftControl))
                                            WorkerBlock.MarchedValue = (byte)(WorkerBlock.MarchedValue + 64 > 254 ? 254 : WorkerBlock.MarchedValue + 64);
                                        else
                                            WorkerBlock.MarchedValue = (byte)(WorkerBlock.MarchedValue + 13 > 254 ? 254 : WorkerBlock.MarchedValue + 13);

                                        EditTerrain.SetBlock(hit, WorkerBlock, false, false);
                                        timer = 1f;
                                    }
                                    else if (WorkerBlock.GetMarchedValue() > 0.5f && WorkerBlock.ID == 0)
                                    {
                                        WorkerBlock = new BlockMetadata(1, BlockSwitches.Marched, 0.501f);
                                        EditTerrain.SetBlock(hit, WorkerBlock, false, false);
                                    }
                                    else
                                    {
                                        position += (hit.normal * 0.5f);
                                        hit.point = position;
                                        WorkerBlock.MarchedValue = 128;
                                        EditTerrain.SetBlock(hit, WorkerBlock, false, false);
                                    }
                                }
                                else if (WorkerBlock.ID == 0)
                                {
                                    position += (hit.normal * 0.5f);
                                    hit.point = position;
                                    WorkerBlock.MarchedValue = 128;
                                    EditTerrain.SetBlock(hit, WorkerBlock, false, false);
                                }
                            }
                        }

                        if (Input.GetMouseButton(0) && buildmode == 2 && timer <= 0f)
                        {
                            BlockMetadata WorkerBlock = EditTerrain.GetBlock(hit, false);
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

                                            if (WorkerBlock.ID == 0 || (WorkerBlock.Switches & BlockSwitches.Marched) == BlockSwitches.Marched)
                                            {

                                                if (WorkerBlock.MarchedValue > 0)
                                                {
                                                    if (Input.GetKey(KeyCode.LeftShift))
                                                        WorkerBlock.MarchedValue = (byte)(WorkerBlock.MarchedValue - 38 < 0 ? 0 : WorkerBlock.MarchedValue - 38);
                                                    else if (Input.GetKey(KeyCode.LeftControl))
                                                        WorkerBlock.MarchedValue = (byte)(WorkerBlock.MarchedValue - 64 < 0 ? 0 : WorkerBlock.MarchedValue - 64);
                                                    else
                                                        WorkerBlock.MarchedValue = (byte)(WorkerBlock.MarchedValue - 13 < 0 ? 0 : WorkerBlock.MarchedValue - 13);

                                                    if (WorkerBlock.MarchedValue < 128)
                                                    {
                                                        float tempmarch = WorkerBlock.MarchedValue;
                                                        WorkerBlock = new BlockMetadata(0, BlockSwitches.Marched, tempmarch);
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
                                if (WorkerBlock.ID != 0 && (WorkerBlock.Switches & BlockSwitches.Marched) == BlockSwitches.Marched)
                                {
                                    if (WorkerBlock.GetMarchedValue() > 0.501f)
                                    {
                                        if (Input.GetKey(KeyCode.LeftShift))
                                            WorkerBlock.MarchedValue = (byte)(WorkerBlock.MarchedValue - 38 < 128 ? 128 : WorkerBlock.MarchedValue - 38);
                                        else if (Input.GetKey(KeyCode.LeftControl))
                                            WorkerBlock.MarchedValue = (byte)(WorkerBlock.MarchedValue - 64 < 128 ? 128 : WorkerBlock.MarchedValue - 64);
                                        else
                                            WorkerBlock.MarchedValue = (byte)(WorkerBlock.MarchedValue - 13 < 128 ? 128 : WorkerBlock.MarchedValue - 13);

                                        EditTerrain.SetBlock(hit, WorkerBlock, false, false);
                                        timer = 1f;
                                    }
                                    else
                                    {
                                        EditTerrain.SetBlock(hit, new BlockMetadata(0, BlockSwitches.None, 0), false, false);
                                    }
                                }
                            }
                        }

                        if (Input.GetMouseButtonDown(0) && buildmode == 0)
                        {
                            int3 blockpos = EditTerrain.GetBlockPos(hit);
                            if (BlockData.byID[world.GetBlock(blockpos.x, blockpos.y + 1, blockpos.z).ID].Foliage)
                                world.SetBlock(blockpos.x, blockpos.y + 1, blockpos.z, new BlockMetadata(0, BlockSwitches.None, 0));
                            else
                            {
                                BlockMetadata b = EditTerrain.GetBlock(hit);
                                if (BlockData.BlockSounds[b.ID].Count > 0)
                                    _audioSource.PlayOneShot(BlockData.BlockSounds[b.ID][Random.Range(0, BlockData.BlockSounds[b.ID].Count - 1)]);
                                EditTerrain.SetBlock(hit, new BlockMetadata(0, BlockSwitches.None, 0), false, true);
                            }
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

                        //if (Input.GetKey(KeyCode.LeftShift)) speed = 20f;
                        //if (Input.GetKey(KeyCode.LeftControl)) speed = 30f;
                        if (flyspeed < 5f) flyspeed = 5f;
                        
                        if (Input.GetKeyDown(KeyCode.W))
                        {
                            if (doubleTapWTimer < 0.5f)
                            {
                                flyspeed = 20f;
                                GetComponent<Camera>().fieldOfView = 80f;
                            }
                            else
                                doubleTapWTimer = 0f;
                        }

                        if (Input.GetKeyUp(KeyCode.W))
                        {
                            flyspeed = 5f;
                            GetComponent<Camera>().fieldOfView = 60f;
                        }
                        
                        if (Input.GetKey(KeyCode.W)) CameraParent.transform.Translate(Vector3.forward * Time.deltaTime * flyspeed);
                        if (Input.GetKey(KeyCode.S)) CameraParent.transform.Translate(Vector3.back * Time.deltaTime * flyspeed);
                        if (Input.GetKey(KeyCode.A)) CameraParent.transform.Translate(Vector3.left * Time.deltaTime * flyspeed);
                        if (Input.GetKey(KeyCode.D)) CameraParent.transform.Translate(Vector3.right * Time.deltaTime * flyspeed);
                        if (Input.GetKey(KeyCode.Space) && flymode) CameraParent.transform.Translate(Vector3.up * Time.deltaTime * flyspeed);
                            
                        if (Input.GetKeyDown(KeyCode.Space))
                        {
                            if (flymode)
                            {
                                if (doubleTapSpacebarTimer < 0.5f)
                                {
                                    flymode = false;
                                    CameraParent.GetComponent<Rigidbody>().isKinematic = false;
                                    CameraParent.GetComponent<Rigidbody>().useGravity = true;
                                }
                                else
                                    doubleTapSpacebarTimer = 0f;
                            }
                            else
                            {
                                if (doubleTapSpacebarTimer < 0.5f)
                                {
                                    flymode = true;
                                    CameraParent.GetComponent<Rigidbody>().isKinematic = true;
                                    CameraParent.GetComponent<Rigidbody>().useGravity = false;
                                }
                                else
                                {
                                    CameraParent.GetComponent<Rigidbody>().AddForce(Vector3.up * 150f, ForceMode.Impulse);
                                    doubleTapSpacebarTimer = 0f;
                                }
                            }
                        }
                        if (Input.GetKey(KeyCode.LeftShift) && flymode) CameraParent.transform.Translate(Vector3.down * Time.deltaTime * flyspeed);
                    }

                    if (Input.GetAxis("Mouse ScrollWheel") > 0f && buildmode == 0)
                    { // forward
                        if (BlockID == 1)
                            BlockID = (ushort)(BlockData.byID.Count - 1);
                        else
                            BlockID -= 1;
                        PlaceBlockMat.SetTextureScale("_BaseColorMap", new Vector2(BlockData.BlockTileSize, BlockData.BlockTileSize));
                        PlaceBlockMat.SetTextureOffset("_UnlitColorMap", new Vector2(BlockData.byID[BlockID].Texture_Marched.x * BlockData.BlockTileSize, BlockData.byID[BlockID].Texture_Marched.y * BlockData.BlockTileSize));
                    }
                    else if (Input.GetAxis("Mouse ScrollWheel") < 0f && buildmode == 0)
                    { // backwards
                        if (BlockID == BlockData.byID.Count - 1)
                            BlockID = 1;
                        else
                            BlockID += 1;
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
                    IngameUI.SetActive(true);
                    SelectionVisualisationGO.SetActive(false);
                    playmode = true;
                    CameraParent.GetComponent<Rigidbody>().isKinematic = false;
                    CameraParent.GetComponent<Rigidbody>().useGravity = true;
                    CanvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
                    CanvasObject.GetComponent<Canvas>().worldCamera = gameObject.GetComponent<Camera>();
                }
            }
            else
            {
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

                    if (Input.GetKeyDown(KeyCode.F1)) attunementAnimations.SelectedAttunement = AttunementAnimations.Attunements.Fire;
                    if (Input.GetKeyDown(KeyCode.F2)) attunementAnimations.SelectedAttunement = AttunementAnimations.Attunements.Water;
                    if (Input.GetKeyDown(KeyCode.F3)) attunementAnimations.SelectedAttunement = AttunementAnimations.Attunements.Earth;
                    if (Input.GetKeyDown(KeyCode.F4)) attunementAnimations.SelectedAttunement = AttunementAnimations.Attunements.Air;
                }

                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    BuildMode_GO.SetActive(true);
                    PlayMode_GO.SetActive(false);
                    BuildModeCanvas.SetActive(true);
                    IngameUI.SetActive(false);
                    playmode = false;
                    flymode = true;
                    CameraParent.GetComponent<Rigidbody>().isKinematic = true;
                    CameraParent.GetComponent<Rigidbody>().useGravity = false;
                    CanvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                }
            }
        } else
        {
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
        }
        if (!dontfocus)
        {
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
}