﻿using HarmonyLib;
using NebulaClient.MonoBehaviours;
using NebulaModel.Logger;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NebulaPatcher.Patches.Dynamic
{
    [HarmonyPatch(typeof(UIMainMenu), "_OnOpen")]
    class UIMainMenu_Patch
    {
        private static RectTransform multiplayerButton;
        private static RectTransform multiplayerMenu;
        private static InputField hostIPAdressInput;

        public static void Postfix()
        {
            GameObject overlayCanvas = GameObject.Find("Overlay Canvas");
            if (overlayCanvas == null)
            {
                Log.Warn("'Overlay Canvas' not found!");
                return;
            }

            if (overlayCanvas.transform.Find("Main Menu") == null)
            {
                Log.Warn("'Overlay Canvas/Main Menu' not found!");
                return;
            }

            if (multiplayerButton)
            {
                multiplayerButton.GetComponentInChildren<Text>().text = "Multiplayer";
                return;
            }

            AddMultiplayerButton();
            AddMultiplayerJoinMenu();
        }

        private static void AddMultiplayerButton()
        {
            RectTransform buttonGroup = GameObject.Find("Main Menu/button-group").GetComponent<RectTransform>();
            RectTransform buttonTemplate = GameObject.Find("Main Menu/button-group/button-new").GetComponent<RectTransform>();

            multiplayerButton = Object.Instantiate(buttonTemplate, buttonGroup, false);
            multiplayerButton.name = "button-multiplayer";
            multiplayerButton.anchoredPosition = new Vector2(multiplayerButton.anchoredPosition.x, multiplayerButton.anchoredPosition.y + multiplayerButton.sizeDelta.y + 10);
            multiplayerButton.GetComponentInChildren<Text>().text = "Multiplayer";

            multiplayerButton.GetComponent<Button>().onClick.RemoveAllListeners();
            multiplayerButton.GetComponent<Button>().onClick.AddListener(new UnityAction(OnMultiplayerButtonClick));
        }

        private static void AddMultiplayerJoinMenu()
        {
            GameObject overlayCanvasGo = GameObject.Find("Overlay Canvas");
            Transform galaxySelectGo = overlayCanvasGo.transform.Find("Galaxy Select");
            if (galaxySelectGo == null)
            {
                Log.Warn("'Overlay Canvas/Galaxy Select' not found!");
                return;
            }

            RectTransform galaxySelectTemplate = galaxySelectGo.GetComponent<RectTransform>();

            multiplayerMenu = Object.Instantiate(galaxySelectTemplate, galaxySelectTemplate.parent);
            Object.Destroy(multiplayerMenu.gameObject.GetComponent<UIGalaxySelect>());

            multiplayerMenu.gameObject.name = "Nebula - Multiplayer Menu";
            multiplayerMenu.Find("random-button").gameObject.SetActive(false);
            multiplayerMenu.Find("star-count").gameObject.SetActive(false);
            multiplayerMenu.Find("resource-multiplier").gameObject.SetActive(false);
            multiplayerMenu.Find("right-group").gameObject.SetActive(false);

            var topTitle = multiplayerMenu.Find("top-title");
            topTitle.GetComponent<Localizer>().enabled = false;
            topTitle.GetComponent<Text>().text = "Multiplayer";

            var hostIpField = multiplayerMenu.Find("galaxy-seed");
            hostIpField.GetComponent<Localizer>().enabled = false;
            hostIpField.GetComponent<Text>().text = "Host IP Address";
            hostIPAdressInput = hostIpField.GetComponentInChildren<InputField>();
            hostIPAdressInput.onEndEdit.RemoveAllListeners();
            hostIPAdressInput.onValueChanged.RemoveAllListeners();
            hostIPAdressInput.characterLimit = 30;
            hostIPAdressInput.text = "127.0.0.1";

            var connectButton = multiplayerMenu.Find("start-button").GetComponent<Button>();
            connectButton.GetComponentInChildren<Localizer>().enabled = false;
            connectButton.GetComponentInChildren<Text>().text = "Join Game";
            connectButton.onClick.RemoveAllListeners();
            connectButton.onClick.AddListener(new UnityAction(OnJoinGameButtonClick));

            var backButton = multiplayerMenu.Find("cancel-button").GetComponent<Button>();
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(new UnityAction(OnBackButtonClick));

            multiplayerMenu.gameObject.SetActive(false);
        }

        private static void OnMultiplayerButtonClick()
        {
            UIRoot.instance.CloseMainMenuUI();
            multiplayerMenu.gameObject.SetActive(true);
            hostIPAdressInput.characterLimit = 30;
        }

        private static void OnJoinGameButtonClick()
        {
            string[] parts = hostIPAdressInput.text.Split(':');
            string ip = parts[0];
            int port;

            if (parts.Length == 1)
            {
                // Use default port
                port = 8469;
            }
            else if (!int.TryParse(parts[1], out port))
            {
                Log.Info($"Port must be a valid number above 1024");
                return;
            }

            Log.Info($"Connecting to server... {ip}:{port}");

            Object.FindObjectOfType<MultiplayerSession>().Connect(ip, port);

            // TODO: Should display a loader during the connection and only open the game once the player as connected to the server.
            // TODO: We should also wait to receive the actual game seed to make sure that we can load the same map and wait for the InitialGameState packet to update the game world properly. 
            
            multiplayerMenu.gameObject.SetActive(false);
            GameDesc gameDesc = new GameDesc();
            gameDesc.SetForNewGame(UniverseGen.algoVersion, 1, 64, 1, 1f);
            DSPGame.StartGameSkipPrologue(gameDesc);
        }

        private static void OnBackButtonClick()
        {
            multiplayerMenu.gameObject.SetActive(false);
            UIRoot.instance.OpenMainMenuUI();
        }
    }
}
