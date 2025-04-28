using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UltraSoundDisplay : MonoBehaviour
{
     [SerializeField] Image Display;


    private void OnTriggerEnter(Collider other)
    {
        UltrasoundData data = other.GetComponent<UltrasoundData>();

        if(data != null )
        {
            Display.sprite = data.GetImageData();
        }
    }
}
