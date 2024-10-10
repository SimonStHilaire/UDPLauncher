using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Launcher : MonoBehaviour
{
    [SerializeField] UDPCommunicationManager Communication;
    [SerializeField] Text LogsText;

    [Serializable]
    public class CommandData
    {
        public string msg;
        public string cmd;
        public float delay = 0;
    }

    [Serializable]
    public class ConfigData
    {
        public int port;
        public List<CommandData> commands = new List<CommandData>();
    }

    const string CONFIG_FILENAME  = "config.json";

    ConfigData Data = null;
    
    StreamWriter messageStream;

    int LogsCount = 0;

    void Start()
    {
        LoadData();

        Communication.StartCommunication(Data.port);

        Communication.OnMessageReceived += OnMessage;
    }

    void OnMessage(string message)
    {
        AddLog("\n" + DateTime.Now.ToString());

        AddLog($"Message reçu: {message}");

        foreach (CommandData cmdData in Data.commands)
        {
            if(cmdData.msg == message)
            {
                if (cmdData.delay > 0f)
                    StartCoroutine(RunCommandDelayed(cmdData.cmd, cmdData.delay));
                else
                    RunCommand(cmdData.cmd);

                return;
            }    
        }

        AddLog($"Aucune commande pour le message");
    }

    IEnumerator RunCommandDelayed(string cmd, float delay)
    {
        AddLog($"La commande {cmd} sera exécutée dans {delay} secondes");

        yield return new WaitForSeconds(delay);

        RunCommand(cmd);
    }

    void RunCommand(string command)
    {
        AddLog($"Exécution de la commande {command}");

        try
        {
            Process process = new Process();
            process.EnableRaisingEvents = false;
            process.StartInfo.FileName = command;
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(command);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardError = true;
            //process.OutputDataReceived += new DataReceivedEventHandler(DataReceived);
            //process.ErrorDataReceived += new DataReceivedEventHandler(ErrorReceived);
            process.Start();
            process.BeginOutputReadLine();
            messageStream = process.StandardInput;

            //Processes.Add(process);

            UnityEngine.Debug.Log("Successfully launched app");
            AddLog($"Lancement réussi");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Unable to launch app: " + e.Message);
            AddLog($"Erreur dans le lancement: {e.Message}");
        }
    }

    //void DataReceived(object sender, DataReceivedEventArgs eventArgs)
    //{

    //}

    //void ErrorReceived(object sender, DataReceivedEventArgs eventArgs)
    //{
    //    UnityEngine.Debug.LogError(eventArgs.Data);
    //}

    void LoadData()
    {
        if (File.Exists(CONFIG_FILENAME))
        {
            Data = JsonUtility.FromJson<ConfigData>(File.ReadAllText(CONFIG_FILENAME));
        }
        else
        {
            Data = new ConfigData();
            File.WriteAllText(CONFIG_FILENAME, JsonUtility.ToJson(Data));
        }    
    }


    private void OnDestroy()
    {
        Communication.StopCommunication();
    }

    void AddLog(string message)
    {
        if (LogsCount > 20)
        {
            LogsText.text = "";
            LogsCount = 0;
        }
        LogsText.text += "\n" + message;
    }
}
