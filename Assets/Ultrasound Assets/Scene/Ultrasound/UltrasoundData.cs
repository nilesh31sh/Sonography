using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UltrasoundData : MonoBehaviour
{
    [SerializeField] Sprite ImageData;

    public Sprite GetImageData()
    {
        return ImageData;
    }
}
