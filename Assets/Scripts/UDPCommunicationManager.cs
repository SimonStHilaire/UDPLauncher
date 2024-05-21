using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using System.Text;
using System.Threading;
using System;

public class UDPCommunicationManager : MonoBehaviour
{
    public delegate void CommunicationAborted();
    public delegate void MessageReceived(string message);
   
    int ReceivePort;

    public event CommunicationAborted OnCommunicationAborted;
    public event MessageReceived OnMessageReceived;

    UdpClient ReceiveClient = null;

    private readonly Queue<string> IncomingMessagesQueue = new Queue<string>();

    Thread ReceiveThread;

    bool IsStarted = false;

    public bool StartCommunication(int receivePort)
    {
        if (IsStarted)
            return true;

        ReceivePort = receivePort;

        IsStarted = true;

        return InitializeCommunication();
    }

    bool InitializeCommunication()
    {
        try
        {
            ReceiveClient = new UdpClient(ReceivePort);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log($"Can't listen for UDP on port {ReceivePort} : {e.Message}");
            ReceiveClient = null;
            return false;
        }
        finally
        {
            if (ReceiveClient != null)
            {
                IsStarted = true;

                ReceiveThread = new Thread(() => ListenForMessages(ReceiveClient));
                ReceiveThread.IsBackground = true;
                ReceiveThread.Start();
            }
            else
            {
                IsStarted = false;
                OnCommunicationAborted?.Invoke();
            }
        }

        return true;
    }

    public void StopCommunication()
    {
        if (!IsStarted)
            return;

        IsStarted = false;
        if (ReceiveThread != null)
        {
            ReceiveThread.Abort();
            ReceiveClient.Close();
        }
    }
    private void ListenForMessages(UdpClient client)
    {
        IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, ReceivePort);

        while (IsStarted)
        {
            try
            {
                Byte[] receiveBytes = client.Receive(ref remoteIpEndPoint);
                string returnData = Encoding.UTF8.GetString(receiveBytes);

                //UnityEngine.Debug.Log($"Raw message received: {returnData}");

                lock (IncomingMessagesQueue)
                {
                    IncomingMessagesQueue.Enqueue(returnData);
                }
            }
            catch (SocketException e)
            {
                // 10004 thrown when socket is closed
                if (e.ErrorCode != 10004) UnityEngine.Debug.Log($"Socket exception while receiving data from udp client: {e.Message}");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log($"Error receiving data from udp client: {e.Message}");
            }
            Thread.Sleep(1);
        }
    }

    private void Update()
    {
        if (IsStarted)
        {
            lock (IncomingMessagesQueue)
            {
                if (IncomingMessagesQueue.Count > 0)
                {
                    try
                    {
                        string message = IncomingMessagesQueue.Dequeue();

                         OnMessageReceived?.Invoke(message);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.Log($"Invalid message format: {e.Message}");
                    }
                }
            }
        }
    }
}
