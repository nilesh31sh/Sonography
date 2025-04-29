using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public SerialReceiverJSON receiverScript; // << Assign SerialReceiverJSON here!

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("ESC Pressed - Preparing to reset connection and close application");

            if (receiverScript != null)
            {
                receiverScript.SendResetMessage(); // call on receiver!
            }
            else
            {
                Debug.LogWarning("No SerialReceiverJSON assigned to GameManager!");
            }

            StartCoroutine(QuitApplication());
        }
    }

    private IEnumerator QuitApplication()
    {
        yield return new WaitForSeconds(0.2f); // Small wait so RESET can be sent

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // In editor
#else
        Application.Quit(); // In build
#endif
    }
}
