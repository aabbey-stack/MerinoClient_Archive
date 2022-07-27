using VRC;

// ReSharper disable InconsistentNaming

namespace MerinoClient.HarmonyPatches.Protection;

internal class YoutubeDLPatch : PatchObject
{
    public YoutubeDLPatch()
    {
        Patch(typeof(YoutubeDL).GetMethod(nameof(YoutubeDL.Method_Private_Void_String_String_0)),
            GetLocalPatch(nameof(ParseOutputPatch)));
    }

    //public unsafe void ParseOutput(string stdOut, string stdErr)
    private static bool ParseOutputPatch()
    {
        return Config.VideoPlayers.Value;
    }
}