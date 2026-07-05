using UnityEditor;
using UnityEngine;

public static class BuildScript
{
    public static void BuildLinuxServer()
    {
        string[] scenes = new string[]
        {
            "Assets/Scenes/GameScene.unity" // change this to your scene
        };

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/LinuxServer/MyServer.x86_64",
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.None,

        };
        options.options = BuildOptions.EnableHeadlessMode;
        BuildPipeline.BuildPlayer(options);
    }
}