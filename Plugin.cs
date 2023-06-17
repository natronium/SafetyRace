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
        GameObject startButton = GameObject.Find("Background/Logo/Buttons/Continue");
        TextMeshPro startButtonText = startButton.transform.GetComponentInChildren<TextMeshPro>();
        ButtonPlus startButtonPlus = startButton.transform.GetComponentInChildren<ButtonPlus>();
        OutlineAnimation startButtonHighlight = GameObject.Find("Continue/Sprite/Highlight")
          .transform.GetComponentInChildren<OutlineAnimation>();
        if (startButtonText.text == "BEGIN STORY")
        {
          startButtonText.text = "BEGIN RACE";
          startButtonHighlight.spriteRenderer.size = new Vector2(3.6f, startButtonHighlight.spriteRenderer.size.y);
        }
        else if (startButtonText.text == "CONTINUE STORY" && ReshuffleEvents.Contains(SaveManager.Instance.currentEvent))
        {
          startButtonText.text = "RETRY CONVERSATION";
          startButtonHighlight.spriteRenderer.size = new Vector2(6.7f, startButtonHighlight.spriteRenderer.size.y);

          GameObject buttonsContainer = GameObject.Find("Background/Logo/Buttons");
          GameObject customButton = GameObject.Find("Custom Button") ??
          	UnityEngine.Object.Instantiate(startButton, buttonsContainer.transform);

          buttonsContainer.transform.position += Vector3.down * 0.6f;

          customButton.name = "Custom Button";
          customButton.transform.position = new Vector3(0.4f, -28.8f, 0f);
          var customButtonText = customButton.transform.GetComponentInChildren<TMPro.TextMeshPro>();
          customButtonText.text = "RESHUFFLE";
          customButtonText.alpha = 1;

          ButtonPlus customButtonPlus = customButton.transform.GetComponentInChildren<ButtonPlus>();
          OutlineAnimation customButtonHighlight = GameObject.Find("Custom Button/Sprite/Highlight")
            .transform.GetComponentInChildren<OutlineAnimation>();
          
          customButtonHighlight.spriteRenderer.size = new Vector2(3f, customButtonHighlight.spriteRenderer.size.y);
          
          TitleController.Instance.buttons.Insert(0, customButtonPlus);
          TitleController.Instance.texts.Insert(0, customButtonText);
          TitleController.Instance.highlights.Insert(0, customButtonHighlight);
          customButtonPlus.onHighlight += TitleController.Instance.OnHighlight;

          customButtonPlus.onPress += delegate (PointerEventData whatever) {
            IsReshuffle = true;
          };
        }
        else
        {
          startButtonText.text = "CONTINUE RACE";
          startButtonHighlight.spriteRenderer.size = new Vector2(4.7f, startButtonHighlight.spriteRenderer.size.y);
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
