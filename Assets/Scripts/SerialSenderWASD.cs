using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System;

public class SerialSenderWASD : MonoBehaviour
{
    SerialPort serialPort;
    public string portName;
    public int baudRate = 115200;
    public bool isTesting = false;
    public string testPortName = "COM6";

    [System.Serializable]
    public class PositionData
    {
        public float x;
        public float y;
        public float z;
    }

    private PositionData pos = new PositionData();
    private bool receiverConfirmed = false; // << New flag
    private bool portOpened = false;

    private void Start()
    {
        if (isTesting)
        {
            Debug.Log("Sender: Testing mode enabled. Connecting directly to " + testPortName);
            ConnectDirectly(testPortName);
        }
        else
        {
            StartCoroutine(DelayedOpen());
        }
    }

    private void ConnectDirectly(string portName)
    {
        try
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = 100;
            serialPort.WriteTimeout = 100;
            serialPort.DtrEnable = true;
            serialPort.RtsEnable = true;

            serialPort.Open();
            Debug.Log($"Sender connected directly to {portName}");
            portOpened = true;

            if (isTesting)
            {
                receiverConfirmed = true;
                Debug.Log("Testing Mode: Receiver assumed ready immediately.");

                //Wait for 1 second before starting SendDataLoop
                StartCoroutine(DelayedStartSendDataLoop(1f));
            }
            else
            {
                StartCoroutine(SendDataLoop());
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Sender failed to open {portName}: {e.Message}");
        }
    }

    private IEnumerator DelayedStartSendDataLoop(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        StartCoroutine(SendDataLoop());
    }


    private IEnumerator DelayedOpen()
    {
        yield return new WaitForSeconds(1f);

        string[] availablePorts = SerialPort.GetPortNames();
        bool portExists = false;

        foreach (string port in availablePorts)
        {
            if (port == portName)
            {
                portExists = true;
                break;
            }
        }

        if (!portExists)
        {
            Debug.LogError($"Port {portName} not found! Available ports: {string.Join(", ", availablePorts)}");
            yield break;
        }

        serialPort = new SerialPort(portName, baudRate);
        serialPort.ReadTimeout = 100;
        serialPort.WriteTimeout = 100;
        serialPort.DtrEnable = true;
        serialPort.RtsEnable = true;

        try
        {
            serialPort.Open();
            Debug.Log($"Serial Sender: Opened {portName} successfully");
            portOpened = true;
            StartCoroutine(SendDataLoop());
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to open port {portName}: {e.Message}");
        }
    }

    private void Update()
    {
        if (portOpened && serialPort != null && serialPort.IsOpen && receiverConfirmed)
        {
            if (Input.GetKeyDown(KeyCode.W))
                pos.z += 1f;
            if (Input.GetKeyDown(KeyCode.S))
                pos.z -= 1f;
            if (Input.GetKeyDown(KeyCode.A))
                pos.x -= 1f;
            if (Input.GetKeyDown(KeyCode.D))
                pos.x += 1f;
        }
    }

    private IEnumerator SendDataLoop()
    {
        if (isTesting)
        {
            Debug.Log("Testing Mode: Directly starting movement data sending");

            // First, try sending a dummy message to confirm connection
            bool testSendSuccess = false;

            try
            {
                serialPort.WriteTimeout = 500; // 0.5 second timeout
                serialPort.WriteLine("{\"x\":0,\"y\":0,\"z\":0}"); // Dummy JSON
                Debug.Log("Initial test message sent successfully.");
                testSendSuccess = true;
            }
            catch (TimeoutException)
            {
                Debug.LogWarning("Testing Mode: Initial write timeout. Receiver may not be ready yet.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Testing Mode: Initial write failed: {e.Message}");
            }

            if (!testSendSuccess)
            {
                Debug.LogError("Testing Mode: Connection test failed. Stopping sender.");
                yield break;
            }

            // Now confirmed port is working. Start sending normally
            while (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    string json = JsonUtility.ToJson(pos);
                    serialPort.WriteLine(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error sending position JSON: {e.Message}");
                }

                yield return new WaitForSeconds(0.01f); // 100Hz
            }
        }
        else
        {
            // (Same as your previous normal non-testing flow)
            float readyResendInterval = 2f;
            float readyTimer = 0f;

            while (serialPort != null && serialPort.IsOpen)
            {
                if (!receiverConfirmed)
                {
                    readyTimer += Time.deltaTime;

                    if (readyTimer >= readyResendInterval)
                    {
                        try
                        {
                            serialPort.WriteLine("ARDUINO_READY");
                            Debug.Log("Sent: ARDUINO_READY");
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"Failed sending ARDUINO_READY: {e.Message}");
                        }

                        readyTimer = 0f;
                    }

                    try
                    {
                        if (serialPort.BytesToRead > 0)
                        {
                            string incoming = serialPort.ReadLine();
                            Debug.Log("Sender Received: " + incoming);

                            if (incoming.Trim() == "RECEIVER_READY")
                            {
                                receiverConfirmed = true;
                                Debug.Log("RECEIVER_READY received. Starting data transfer.");
                            }
                        }
                    }
                    catch (TimeoutException)
                    {
                        // Ignore
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Error reading receiver confirmation: {e.Message}");
                    }
                }
                else
                {
                    try
                    {
                        string json = JsonUtility.ToJson(pos);
                        serialPort.WriteLine(json);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error sending position JSON: {e.Message}");
                    }

                    yield return new WaitForSeconds(0.01f);
                }

                yield return null;
            }
        }
    }



    private void OnApplicationQuit()
    {
        if (serialPort != null && serialPort.IsOpen)
            serialPort.Close();
    }
}
