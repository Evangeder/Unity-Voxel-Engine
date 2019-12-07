using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class tat_ : MonoBehaviour
{
    // Start is called before the first frame update
    public List<Texture2D> textures;
    Texture2DArray Tarray;
    MeshFilter mf;
    MeshRenderer mr;
    Material mat;

    [Range(0, 4)]
    public int TextureUVCoord = 0;
    int LastCoord = 0;

    void Start()
    {
        mf = gameObject.GetComponent<MeshFilter>();
        mr = gameObject.GetComponent<MeshRenderer>();
        mat = gameObject.GetComponent<Material>();

        if (textures.Count > 0)
        {
            Tarray = new Texture2DArray(textures[0].width, textures[0].height, textures.Count, TextureFormat.ARGB32, true);
            for (int i = 0; i < textures.Count; i++)
            {
                Tarray.SetPixels(textures[i].GetPixels(0), i, 0);
                Tarray.Apply();
            }
            GetComponent<Renderer>().sharedMaterial.SetTexture("_Test", Tarray);
            RecalculateMesh();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (TextureUVCoord != LastCoord && textures.Count > 0)
        {
            LastCoord = TextureUVCoord;
            RecalculateMesh();
        }
    }

    void RecalculateMesh()
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector2> uvs2 = new List<Vector2>();

        int tex = 0;
        for (int i = 0; i < 2; i++)
            for (int i2 = 0; i2 < 2; i2++)
            {
                verts.Add(new Vector3(i, 0.5f, i2 +1f));
                verts.Add(new Vector3(i +1f, 0.5f, i2 +1f));
                verts.Add(new Vector3(i +1f, 0.5f, i2));
                verts.Add(new Vector3(i, 0.5f, i2));

                // Adding triangles to vertices
                tris.Add(verts.Count - 4);
                tris.Add(verts.Count - 3);
                tris.Add(verts.Count - 2);
                tris.Add(verts.Count - 4);
                tris.Add(verts.Count - 2);
                tris.Add(verts.Count - 1);

                // UV0 work normally, it aligns the texture on mesh
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(0f, 0f));

                // UV2 X coordinate = texture (block id)
                uvs2.Add(new Vector2(tex, 0));
                uvs2.Add(new Vector2(tex, 0));
                uvs2.Add(new Vector2(tex, 0));
                uvs2.Add(new Vector2(tex, 0));
                tex++;
            }
        

        mf.mesh.Clear();
        mf.mesh.vertices = verts.ToArray();
        mf.mesh.SetTriangles(tris.ToArray(), 0);
        mf.mesh.uv = uvs.ToArray();
        mf.mesh.uv2 = uvs2.ToArray();
        mf.mesh.MarkDynamic();
        mf.mesh.RecalculateNormals();
    }
}
