using System.Collections.Generic;
using BepInEx;
using Echodog;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SafetyRace
{
  [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
  public class Plugin : BaseUnityPlugin
  {
    static Plugin Instance;
    static bool IsFirstLoad = true;
    static bool IsReshuffle = false;

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
        SaveManager saveMan = Bootstrap.Instance.menu.save;
        if (IsReshuffle) {
          saveMan.ImportantRandomValue();
          saveMan.Save();
        }

        IsReshuffle = false;
      }

      [HarmonyPostfix]
      [HarmonyPatch("Go")]
      static void PostGo()
      {
        TextMeshPro startButtonText = TitleController.Instance.texts[0];
        if (startButtonText.text == "BEGIN STORY")
        {
          startButtonText.text = "BEGIN RACE";
        }
        else if (startButtonText.text == "CONTINUE STORY" && ReshuffleEvents.Contains(SaveManager.Instance.currentEvent))
        {
          startButtonText.text = "RETRY CONVERSATION";

          GameObject buttonsContainer = GameObject.Find("Background/Logo/Buttons");
          GameObject continueButton = GameObject.Find("Background/Logo/Buttons/Continue");
          GameObject customButton = GameObject.Find("Custom Button") ??
          	UnityEngine.Object.Instantiate(continueButton, buttonsContainer.transform);

          customButton.name = "Custom Button";
          customButton.transform.position = new Vector3(6.7f, -29.5f, 0f);
          var customTextMesh = customButton.transform.GetComponentInChildren<TMPro.TextMeshPro>();
          customTextMesh.text = "RESHUFFLE";
          customTextMesh.alpha = 1;

          var customButtonPlus = customButton.transform.GetComponentInChildren<ButtonPlus>();

          customButtonPlus.onPress += delegate (PointerEventData whatever) {
            IsReshuffle = true;
          };
        }
        else
        {
          startButtonText.text = "CONTINUE RACE";
        }

        //Only set the menu text alpha *after* the first title screen load, so that we don't stomp
        // all over their pretty animations (that cause this problem in the first place)
        if (!IsFirstLoad)
        {
          // all the TMP button text has alpha 0 by default, and updating the text redraws it transparent as a result
          foreach (TextMeshPro text in TitleController.Instance.texts) {
            text.alpha = 1;
          }
        }
        else
        {
          IsFirstLoad = false;
        }

      }
    } //hook TitleController.LoadGame & Go

  }
}
