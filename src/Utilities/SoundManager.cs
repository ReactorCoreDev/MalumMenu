using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace MalumMenu;

// ZenithX's Sound Manager
public static class SoundManager
{
    public static readonly Dictionary<string, AudioClip> LoadedSounds = [];
    private static CoroutineRunner Runner;
    
    private static readonly string BaseUrl = "https://github.com/scp222thj/MalumMenu/tree/main/src/Sounds";
    private static readonly string[] SoundFiles = [
        "Click.wav", // You can also add more sound files.
    ];

    public static void Initialize()
    {
        if (Runner == null)
        {
            GameObject RunnerObject = new GameObject("CoroutineRunner");
            Runner = RunnerObject.AddComponent<CoroutineRunner>();
            DontDestroyOnLoad(RunnerObject);
        }

        if (CoroutineRunner.Instance == null)
        {
            GameObject Audio = new GameObject("Audio");
            Audio.AddComponent<CoroutineRunner>();
        }

        string MalumMenu = Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "MalumMenu");
        string Sounds = Path.Combine(MalumMenu, "Sounds");

        if (!Directory.Exists(MalumMenu))
        {
            Directory.CreateDirectory(MalumMenu);
        }
            
        if (!Directory.Exists(Sounds))
        {
            Directory.CreateDirectory(Sounds);
        }

        using HttpClient client = new();

        foreach (string fileName in SoundFiles)
        {
            string localPath = Path.Combine(Sounds, fileName);
            string url = BaseUrl + fileName;

            if (!File.Exists(localPath))
            {
                try
                {
                    byte[] data = client.GetByteArrayAsync(url).Result;
                    File.WriteAllBytes(localPath, data);
                }
                catch (Exception e)
                {
                   Debug.LogError($"[SoundManager] Failed to download {fileName}: {e.Message}");
                }
            }
        }

        string[] files = Directory.GetFiles(Sounds);

        foreach (string file in files)
        {
            string ext = Path.GetExtension(file).ToLower();

            if (ext != ".wav" && ext != ".mp3" && ext != ".ogg")
            {
                continue;
            }

            if (ext == ".wav") // Using .wav audio files is highly recommended.
                LoadWav(file);
            else
            {
                Runner.ClipPath = file;
                Runner.ClipExt = ext;
                Runner.StartCoroutine("LoadClipCoroutine");
            }
        }
    }

    private static void LoadWav(string path)
    {
        try
        {
            byte[] data = File.ReadAllBytes(path);
            WAV wav = new(data);
            AudioClip clip = AudioClip.Create(Path.GetFileNameWithoutExtension(path), wav.SampleCount, wav.ChannelCount, wav.Frequency, false);
            clip.SetData(wav.LeftChannel, 0);

            string key = Path.GetFileNameWithoutExtension(path);
            if (!LoadedSounds.ContainsKey(key)) LoadedSounds.Add(key, clip);

            CoroutineRunner.Instance.AddClip(key, clip);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SoundManager] Failed to load WAV {path}: {ex}");
        }
    }

    public static void PlaySound(string name, float volume)
    {
        if (!LoadedSounds.ContainsKey(name))
        {
            return;
        }

        CoroutineRunner.Instance.AddClip(name, LoadedSounds[name]);
        CoroutineRunner.Instance.PlayClip(name, volume);
    }

    public struct WAV
    {
        public float[] LeftChannel;
        public int ChannelCount;
        public int SampleCount;
        public int Frequency;

        public WAV(byte[] wav)
        {
            ChannelCount = wav[22];
            Frequency = wav[24] | (wav[25] << 8);
            int pos = 12;

            while (!(wav[pos] == 100 && wav[pos + 1] == 97 && wav[pos + 2] == 116 && wav[pos + 3] == 97))
            {
                int chunkSize = wav[pos + 4] | (wav[pos + 5] << 8);
                pos += 8 + chunkSize;
            }

            pos += 8;
            SampleCount = (wav.Length - pos) / 2 / ChannelCount;
            LeftChannel = new float[SampleCount * ChannelCount];

            int i = 0;
            while (pos < wav.Length)
            {
                for (int ch = 0; ch < ChannelCount; ch++)
                {
                    short sample = (short)(wav[pos] | (wav[pos + 1] << 8));
                    LeftChannel[i++] = sample / 32768f;
                    pos += 2;
                }
            }
        }
    }
}
