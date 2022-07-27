using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using MerinoClient.Core.VRChat;
using UnhollowerBaseLib;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;
using ConsoleColor = System.ConsoleColor;
using Exception = System.Exception;

// ReSharper disable InconsistentNaming

namespace MerinoClient.Features.QoL;

/*
 * server side code source that has helped me to make this: https://github.com/information-redacted/naoka-ng/blob/881fd629cacd2e3f87f0c8bbe855b0a779c2112c/Moderation.cs
 */

internal class ModerationsHandler : FeatureComponent
{
    public override string FeatureName => GetType().Name;
    public override string OriginalAuthor => "abbey";

    public static bool ResolveModeration(Dictionary<byte, Object> moderationsDictionary)
    {
        try
        {
            var type = (ExecutiveActionTypes)moderationsDictionary[0].Unbox<byte>();
            switch (type)
            {
                case ExecutiveActionTypes.Enforce_Moderation:
                    break;
                case ExecutiveActionTypes.Alert:
                    var typeOfAlert = moderationsDictionary[5].ToString();
                    var reason = moderationsDictionary[2].ToString();
                    MerinoLogger.Warning($"Received an alert type of: {typeOfAlert} with reason: {reason}");
                    return false;
                case ExecutiveActionTypes.Warn:
                    MerinoLogger.Warning($"Received warn moderation, {moderationsDictionary[245].ToString()}");
                    break;
                case ExecutiveActionTypes.Kick:
                    var target = moderationsDictionary[254].Unbox<int>();
                    MerinoLogger.Warning(
                        $"Received kick moderation for {target.GetPlayer().GetDisplayName()}, reason: {moderationsDictionary[245].ToString()}");
                    break;
                case ExecutiveActionTypes.Vote_Kick:
                    MerinoLogger.Warning($"Vote kick has been instantiated: {moderationsDictionary[245].ToString()}");
                    break;
                case ExecutiveActionTypes.Public_Ban:
                    break;
                case ExecutiveActionTypes.Ban:
                    break;
                case ExecutiveActionTypes.Mic_Off:
                    MerinoLogger.Warning(
                        "Received force mic off moderation from either a moderator or an instance creator");
                    return false;
                case ExecutiveActionTypes.Mic_Volume_Adjust:
                    break;
                case ExecutiveActionTypes.Friend_Change:
                    break;
                case ExecutiveActionTypes.Warp_To_Instance:
                    break;
                case ExecutiveActionTypes.Teleport_User:
                    break;
                case ExecutiveActionTypes.Query:
                    var queryDetails = moderationsDictionary[2].ToString();
                    /*
                     * 2022.05.25 01:02:34 Log        -  [Network Data] OnEvent: SYSTEM 33
                        {
                       "Code": 33,
                       "Parameters": [
                       {
                       "Key": 245,
                       "Value": {
                       "0": 13,
                       "2": "A vote kick has been initiated against ｜OPERATOR｜, do you agree?",
                       "3": "96b7bd2e-2a7f-4c09-bb3a-e26ea027e119"
                       }
                       },
                       {
                       "Key": 254,
                       "Value": 0
                       }
                       ],
                     */
                    MerinoLogger.Warning($"Query has been called: {queryDetails}");
                    break;
                case ExecutiveActionTypes.Request_PlayerMods:
                    //https://github.com/information-redacted/naoka-ng/blob/881fd629cacd2e3f87f0c8bbe855b0a779c2112c/Moderation.cs#L36
                    var ids = Il2CppArrayBase<string>.WrapNativeGenericArrayPointer(moderationsDictionary[3].Pointer);
                    if (ids.Count != 0)
                        MerinoLogger.Msg(ConsoleColor.Cyan, $"Requested player mods for {ids.Count} player(s)");
                    break;
                case ExecutiveActionTypes.Reply_PlayerMods:
                    if (moderationsDictionary.ContainsKey(1))
                    {
                        var sender = moderationsDictionary[1].Unbox<int>();
                        if (moderationsDictionary[10].Unbox<bool>())
                        {
                            MerinoLogger.Warning(
                                $"[Moderation] {sender.GetPlayer().GetDisplayName()} has you blocked!");
                            return false;
                        }

                        if (moderationsDictionary[11].Unbox<bool>())
                            MerinoLogger.Warning(
                                $"[Moderation] {sender.GetPlayer().GetDisplayName()} has you muted!");
                    }

                    break;
                case ExecutiveActionTypes.Block_User:
                    break;
                case ExecutiveActionTypes.Mute_User:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch (Exception e)
        {
            MerinoLogger.Error(
                $"An exception occurred while trying to resolve a moderation type {moderationsDictionary[0].Unbox<byte>()}:\n{e}");
        }

        return true;
    }

    private enum ExecutiveActionTypes : byte
    {
        Enforce_Moderation = 1,
        Alert = 2,
        Warn = 3,
        Kick = 4,
        Vote_Kick = 5,
        Public_Ban = 6,
        Ban = 7,
        Mic_Off = 8,
        Mic_Volume_Adjust = 9,
        Friend_Change = 10,
        Warp_To_Instance = 11,
        Teleport_User = 12,
        Query = 13,
        Request_PlayerMods = 20,
        Reply_PlayerMods = 21,
        Block_User = 22,
        Mute_User = 23
    }
}