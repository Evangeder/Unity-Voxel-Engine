using UnityEngine;
using UnityEngine.UI;

public class AddCanvasToList : MonoBehaviour
{
    void Awake()
    {
        CanvasList.canvas.Add(GetComponent<CanvasScaler>());   
    }
}
