using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class bomb : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void OnCollisionEnter(Collision collision)
    {
        if(collision.transform.name.Contains("Chunk"))
        {
            World world = GameObject.Find("World").GetComponent<World>();
            BlockMetadata WorkerBlock = default;
            int3 coords = new int3((int)math.floor(transform.localPosition.x), (int)math.floor(transform.localPosition.y), (int)math.floor(transform.localPosition.z));
            float R = math.sqrt(math.pow(3, 2) + math.pow(3, 2) + math.pow(3, 2));

            for (float ix = -R; ix <= R; ix++)
            {
                for (float iy = -R; iy <= R; iy++)
                {
                    for (float iz = -R; iz <= R; iz++)
                    {
                        if ((math.pow(ix, 2) + math.pow(iy, 2) + math.pow(iz, 2)) < (math.pow(R, 2)))
                        {
                            WorkerBlock.ID = 0;
                            world.SetBlock(coords.x + (int)ix, coords.y + (int)iy, coords.z + (int)iz, WorkerBlock);
                        }
                    }
                }
            }
            Destroy(this.gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.localPosition.y < 10f)
        {
            Destroy(this.gameObject);
        }
    }
}
