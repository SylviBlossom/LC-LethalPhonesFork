using HarmonyLib;
using Scoops.service;

namespace Scoops.patch;

public class SaveGamePhonePatch
{
    [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.SaveGameValues))]
    public static void SaveGameValues(GameNetworkManager __instance)
    {
        if (!__instance.isHostingGame || !StartOfRound.Instance.inShipPhase || StartOfRound.Instance.isChallengeFile)
        {
            return;
        }
        if (PhoneNetworkHandler.Instance == null)
        {
            return;
        }

        PhoneNetworkHandler.Instance.SaveNumbers();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    public static void LoadGameValues(StartOfRound __instance)
    {
        if (!__instance.IsServer)
        {
            return;
        }
        if (PhoneNetworkHandler.Instance == null)
        {
            return;
        }

        PhoneNetworkHandler.Instance.LoadNumbers();
    }
}
