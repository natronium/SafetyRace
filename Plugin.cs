using System.Collections.Generic;
using BepInEx;
using Echodog;
using HarmonyLib;
using TMPro;

namespace SafetyRace
{
  [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
  public class Plugin : BaseUnityPlugin
  {
    static Plugin Instance;
    static bool JustLoaded;
    static bool IsFirstLoad = true;

    static List<string> ReshuffleEvents = new List<string>() {
        "gull_intro", "talia_fish", "oscar_welcome",
        "lars_intro", "maya_welcome"
    };

    private void Awake()
    {
      Instance = this;
      var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
      harmony.PatchAll(); // automatic based on HarmonyPatch &c attributes
      Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }

    [HarmonyPatch(typeof(Echodog.TitleController))]
    private static class TitleControllerPatch
    {

      [HarmonyPostfix]
      [HarmonyPatch("LoadGame")]
      static void PostLoadGame()
      {
        Instance.Logger.LogDebug("Game reload detected: watching for next event");
        JustLoaded = true;
      }

      [HarmonyPostfix]
      [HarmonyPatch("Go")]
      static void PostGo()
      {
        TextMeshPro firstButtonText = TitleController.Instance.texts[0];
        if (firstButtonText.text == "BEGIN STORY")
        {
          firstButtonText.text = "BEGIN RACE";
        }
        else if (firstButtonText.text == "CONTINUE STORY" && ReshuffleEvents.Contains(SaveManager.Instance.currentEvent))
        {
          firstButtonText.text = "RESHUFFLE";
        }
        else
        {
          firstButtonText.text = "CONTINUE STORY*";
        }

        if (!IsFirstLoad)
        {
          // all the TMP button text has alpha 0 by default, and updating the text redraws it transparent as a result
          firstButtonText.alpha = 1;
        }
        else
        {
          IsFirstLoad = false;
        }

      }
    } //hook TitleController.LoadGame & Go

    [HarmonyPatch(typeof(Echodog.GameController))]
    private static class GameControllerPatch
    {

      [HarmonyPostfix]
      [HarmonyPatch("Init")]
      static void PostInit(GameController __instance, EventData eventData)
      {
        if (JustLoaded)
        {
          JustLoaded = false;
          if (ReshuffleEvents.Contains(eventData.id))
          {
            Instance.Logger.LogDebug($"Reshuffling decks for event {eventData.id}");
            __instance.runner.game.player.deck.ShuffleInPlace();
            __instance.runner.game.npc.deck.ShuffleInPlace();
          }
        }
      }


    } //hook GameController.Init
  }
}
