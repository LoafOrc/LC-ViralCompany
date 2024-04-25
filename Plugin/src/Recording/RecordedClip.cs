﻿using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using ViralCompany.Behaviours;
using ViralCompany.Recording;

namespace ViralCompany.Recording;
internal class RecordedClip {
    const int CHUNK_SIZE = 30_000;

    public string FilePath { get {
            return Path.Combine(Video.FolderPath, ClipID + VideoRecorder.VideoExtension);
        } }

    public bool IsValid { get; private set; }

    public string ClipID { get; private set; }

    public RecordedVideo Video { get; private set; }
    List<Texture2D> frames;

    public RecordedClip(RecordedVideo video, string clipID) {
        Video = video;
        ClipID = clipID;
    }

    public void AddFrame(Texture2D frame) {
        if(frames == null) {
            Plugin.Logger.LogDebug("Inited RecordedClip for recording.");
            frames = [];
        }

        frames.Add(frame);
    }

    public async void ClipFinished() {
        List<Texture2DVideoFrame> framesReadyForEncoding = [];

        foreach(Texture2D frame in frames) {
            framesReadyForEncoding.Add(new Texture2DVideoFrame(frame));
        }

        await FFmpegEncoder.CreateClip(framesReadyForEncoding, [], this);

        IsValid = true;
        frames.Clear(); // clear frames for memory savings
    }

    List<byte[]> BreakIntoChunks() {
        List<byte[]> chunks = [];
        byte[] data = File.ReadAllBytes(FilePath);

        int i;
        for(i = 0; i < data.Length; i += CHUNK_SIZE) {
            byte[] chunk = new byte[CHUNK_SIZE];
            Array.Copy(data, i, chunk, 0, chunk.Length);

            Plugin.Logger.LogDebug($"Created chunk {chunks.Count} with a size of {CHUNK_SIZE}");
            chunks.Add( chunk );
        }
        if(i < data.Length) {
            int finalChunkSize = data.Length - i;
            Plugin.Logger.LogDebug($"Creating a last chunk of size: {finalChunkSize}");
            byte[] chunk = new byte[finalChunkSize];
            Array.Copy(data, i, chunk, 0, chunk.Length);
            chunks.Add(chunk);
        }

        return chunks;
    }
}
