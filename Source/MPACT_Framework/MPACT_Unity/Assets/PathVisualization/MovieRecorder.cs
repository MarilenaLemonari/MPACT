using System.IO;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;

public class MovieRecorder : Singleton<MovieRecorder>
{
    RecorderController m_RecorderController;
    private bool m_RecordAudio = false;
    internal MovieRecorderSettings m_Settings = null;
    public RenderTexture targetTexture;

    public override void Awake()
    {
        // Video
        m_Settings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        m_Settings.name = "My Video Recorder";
        m_Settings.Enabled = true;

        // This example performs an MP4 recording
        m_Settings.EncoderSettings = new CoreEncoderSettings
        {
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            Codec = CoreEncoderSettings.OutputCodec.MP4
        };
        m_Settings.CaptureAlpha = true;
        m_Settings.ImageInputSettings = new RenderTextureInputSettings()
        {
            RenderTexture = targetTexture,
            OutputWidth = 2400,
            OutputHeight = 1800
        };
        base.Awake();
    }

    public void InitializeRecorder(string path, string name)
    {
        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        m_RecorderController = new RecorderController(controllerSettings);
        
        var mediaOutputFolder = new DirectoryInfo(path);
        
        // Simple file name (no wildcards) so that FileInfo constructor works in OutputFile getter.
        m_Settings.OutputFile = mediaOutputFolder.FullName;

        // Setup Recording
        controllerSettings.AddRecorderSettings(m_Settings);
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = 30.0f;

        RecorderOptions.VerboseMode = false;
        m_RecorderController.PrepareRecording();
        m_RecorderController.StartRecording();
    }

    public void StopCurrentRecording()
    {
        m_RecorderController.StopRecording();
    }
    
    void OnDisable()
    {
        m_RecorderController.StopRecording();
    }
}

