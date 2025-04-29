using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System;

public class SerialReceiverJSON : MonoBehaviour
{
    SerialPort serialPort;
    public int baudRate = 115200;
    public float scanTimeout = 2f; // seconds to wait per port
    public bool isTesting = false; // << New toggle
    public string testPortName = "COM7"; // << Set manually if testing

    private bool connected = false;
    public Transform modelToMove;

    [System.Serializable]
    public class PositionData
    {
        public float x;
        public float y;
        public float z;
    }

    private PositionData lastPos = new PositionData();

    private void Start()
    {
        if (isTesting)
        {
            Debug.Log("Receiver: Testing mode enabled. Connecting directly to " + testPortName);
            ConnectDirectly(testPortName);
        }
        else
        {
            StartCoroutine(ScanAndConnectCoroutine());
        }
    }

    void ConnectDirectly(string portName)
    {
        try
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.Open();
            connected = true;
            Debug.Log($"Receiver connected directly to {portName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Receiver failed to open {portName}: {e.Message}");
        }
    }


    public void SendResetMessage()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                serialPort.WriteLine("RESET");
                Debug.Log("SerialReceiver: Sent RESET message to Arduino!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"SerialReceiver: Failed to send RESET: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("SerialReceiver: SerialPort not open, cannot send RESET.");
        }
    }



    IEnumerator ScanAndConnectCoroutine()
    {
        yield return new WaitForSeconds(0.25f);
        string[] ports = SerialPort.GetPortNames();

        foreach (string port in ports)
        {
            Debug.Log($"Trying port {port}...");
            SerialPort tryPort = new SerialPort(port, baudRate);
            tryPort.ReadTimeout = 100;
            tryPort.WriteTimeout = 100;
            tryPort.DtrEnable = true;
            tryPort.RtsEnable = true;

            bool opened = false;

            try
            {
                tryPort.Open();
                opened = true;
                Debug.Log($"Opened port {port}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not open port {port}: {e.Message}");
            }

            if (opened)
            {
                float timer = 0f;
                while (timer < scanTimeout)
                {
                    try
                    {
                        string incoming = tryPort.ReadExisting();

                        if (!string.IsNullOrEmpty(incoming))
                        {
                            Debug.Log($"Data from {port}: {incoming}");

                            if (incoming.Contains("ARDUINO_READY"))
                            {
                                serialPort = tryPort; // <<<<< FIRST assign
                                try
                                {
                                    serialPort.WriteLine("RECEIVER_READY"); // now it's safe
                                    Debug.Log($"Sent RECEIVER_READY to {port}");
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError($"Failed to send RECEIVER_READY: {e.Message}");
                                }

                                connected = true;
                                Debug.Log($"Found Arduino on {port} and connected successfully");
                                yield break; // Exit coroutine
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Read error: {e.Message}");
                    }

                    timer += Time.deltaTime;
                    yield return null;
                }

                tryPort.Close();
            }

            yield return null;
        }

        Debug.LogError("Arduino not found on any port.");
    }

    private void Update()
    {
        if (connected && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                if (serialPort.BytesToRead > 0)
                {
                    string json = serialPort.ReadLine();
                    Debug.Log("Received: " + json);

                    PositionData newPos = JsonUtility.FromJson<PositionData>(json);

                    float deltaX = newPos.x - lastPos.x;
                    float deltaY = newPos.y - lastPos.y;
                    float deltaZ = newPos.z - lastPos.z;

                    modelToMove.position += new Vector3(deltaX, deltaY, deltaZ);

                    lastPos = newPos;
                }
            }
            catch (TimeoutException)
            {
                // Ignore
            }
            catch (Exception e)
            {
                Debug.LogError("Serial Read Error: " + e.Message);
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (serialPort != null && serialPort.IsOpen)
            serialPort.Close();
    }
}
