using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildGameAudio
{
    const string Folder = "Assets/Resources/GameAudio/";
    public static void BuildAndPreview()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string finalName = GameAudio.FileNames[(int)GameAudio.Sound.FinalMusic];
            string original = Folder + finalName + ".mp3";
            if (File.Exists(original)) TrimFinalMusic(original, Folder + finalName + ".wav");
            for (int i = 0; i < GameAudio.FileNames.Length; i++)
            {
                string name = GameAudio.FileNames[i];
                string path = Folder + name + (File.Exists(Folder + name + ".wav") ? ".wav" : ".mp3");
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) throw new Exception("Missing sound: " + path);
                bool streamed = i == (int)GameAudio.Sound.EarlyMusic || i == (int)GameAudio.Sound.LateMusic || i == (int)GameAudio.Sound.FinalMusic || i == (int)GameAudio.Sound.NecromancerMusic || i == (int)GameAudio.Sound.HunterBreath;
                var settings = importer.defaultSampleSettings;
                settings.loadType = streamed ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = 0.75f;
                importer.defaultSampleSettings = settings;
                importer.loadInBackground = streamed; importer.SaveAndReimport();
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null || clip.length <= 0) throw new Exception("Invalid sound: " + path);
                Debug.Log("SOUND_IMPORTED " + name + " seconds=" + clip.length);
            }
            BuildTutorialCanvas.ApplyAll(); AssetDatabase.SaveAssets();
            Debug.Log("GAME_AUDIO_BUILD_OK: " + GameAudio.FileNames.Length + " sounds; final music trimmed to at most 40 seconds");
            PreviewTutorialCanvas.Render();
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static void TrimFinalMusic(string original, string destination)
    {
        var importer = (AudioImporter)AssetImporter.GetAtPath(original);
        var settings = importer.defaultSampleSettings; settings.loadType = AudioClipLoadType.DecompressOnLoad;
        importer.defaultSampleSettings = settings; importer.loadInBackground = false; importer.SaveAndReimport();
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(original); clip.LoadAudioData();
        int frames = Mathf.Min(clip.samples, clip.frequency * 40);
        var data = new float[frames * clip.channels];
        if (!clip.GetData(data, 0)) throw new Exception("Could not read final music samples");
        using (var writer = new BinaryWriter(File.Create(destination)))
        {
            int bytes = data.Length * 2;
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + bytes);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
            writer.Write((short)1); writer.Write((short)clip.channels); writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2); writer.Write((short)(clip.channels * 2)); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(bytes);
            foreach (float sample in data) writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1, 1) * 32767));
        }
        AssetDatabase.DeleteAsset(original);
        AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
    }
}
