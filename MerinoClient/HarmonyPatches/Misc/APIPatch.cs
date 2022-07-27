using BestHTTP;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using VRC.Core;

namespace MerinoClient.HarmonyPatches.Misc;

internal class APIPatch : PatchObject
{
    public APIPatch()
    {
        Patch(typeof(API).GetMethod(nameof(API.SendRequest)),
            postfix: GetLocalPatch(nameof(SendRequestPatch)));
    }

    private static void SendRequestPatch(string endpoint, HTTPMethods method,
        Dictionary<string, Object> requestParams, bool authenticationRequired)
    {
        if (!Config.LogAPIRequests.Value) return;
        if (requestParams != null)
            foreach (var request in requestParams)
            {
                var str1 = Convert.ToString(request.Value);
                MerinoLogger.Msg(
                    $"Sent request type of {method} to endpoint: {endpoint} with value: {str1} and key: {request.Key}, authentication required = {authenticationRequired}");
            }
        else
            MerinoLogger.Msg(
                $"Sent request type of {method} {endpoint}, authentication required = {authenticationRequired}");
    }
}