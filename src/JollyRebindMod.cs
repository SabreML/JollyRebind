using BepInEx;
using Rewired;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace JollyRebind
{
	[BepInPlugin("sabreml.jollyrebind", "JollyRebind", "1.2.3")]
	public class JollyRebindMod : BaseUnityPlugin
	{
		// The maximum number of co-op players. (Default: 4)
		// The 'Myriad of Slugcats' mod can increase this up to 16.
		public static int MaxPlayerCount { private set; get; }


		// A `HashSet` of previously logged controller element exceptions.
		// These are tracked because `JollyInputUpdate` happens once every frame, and this could very quickly spam the log file otherwise.
		// (`HashSet`s don't allow duplicate entries.)
		private static readonly HashSet<string> exceptionLogLog = new HashSet<string>();


		public void OnEnable()
		{
			On.RainWorld.OnModsInit += Init;
			On.RainWorld.PostModsInit += PostInit;
		}

		private void Init(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			orig(self);
			On.Player.JollyInputUpdate += JollyInputUpdateHK;
			JollyMenuKeybinds.SetUpHooks();
			JollyRebindConfig.SetUpHooks();

			MachineConnector.SetRegisteredOI(Info.Metadata.GUID, new JollyRebindConfig());
		}

		// 'Myriad of Slugcats' compatibility.
		private void PostInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
		{
			orig(self);
			// The `PlayerObjectBodyColors` field seems to be the most reliable way to get a max player count.
			MaxPlayerCount = RainWorld.PlayerObjectBodyColors.Length;
			if (MaxPlayerCount > 4)
			{
				// Remake these arrays with a bigger size.
				JollyMenuKeybinds.KeybindWrappers = new Menu.Remix.UIelementWrapper[MaxPlayerCount];
				JollyRebindConfig.PlayerPointInputs = new Configurable<KeyCode>[MaxPlayerCount];
			}

			// Whether it's modified or not, finish setting up the input configs.
			JollyRebindConfig.CreateInputConfigs();
		}


		// If the player has a custom keybind set (AKA: Not the map key), this method sets `jollyButtonDown` to true if the key is being held down.
		private void JollyInputUpdateHK(On.Player.orig_JollyInputUpdate orig, Player self)
		{
			orig(self);

			// The player's custom pointing keybind from the jolly menu keybinder.
			KeyCode playerKeybind = JollyRebindConfig.PlayerPointInputs[self.playerState.playerNumber].Value;

			// If the player's pointing keybind is different to their map key.
			if (playerKeybind != GetMapKey(self.playerState.playerNumber))
			{
				// Override whatever `jollyButtonDown` was set to in `orig()`.
				self.jollyButtonDown = Input.GetKey(playerKeybind);
			}
		}

		// Returns `playerNumber`'s map key from the game's settings as a `KeyCode`.
		private static KeyCode GetMapKey(int playerNumber)
		{
			Options.ControlSetup playerControls = RWCustom.Custom.rainWorld.options.controls[playerNumber];

			// If the player is using a controller.
			if (playerControls.gamePad)
			{
				// The button on the controller which is bound to the 'Map' action.
				ActionElementMap mapKeyElementMap = playerControls.gameControlMap.AllMaps
					.First(elementMap => elementMap.actionId == RewiredConsts.Action.Map);

				// If that button's name is in the `rewiredNameToKeyCode` dictionary for the controller.
				try
				{
					return rewiredNameToKeyCode[playerControls.GetActivePreset()][mapKeyElementMap.elementIdentifierName];
				}
				catch (KeyNotFoundException)
				{
					// If it's /not/ in the dictionary, make an exception log with the name and ID of the button the player is trying to use.
					string exceptionString = string.Format(
						"(JollyRebind) Unknown map button element '{0} ({1})'! [GUID: {2}]",
						mapKeyElementMap.elementIdentifierName,
						mapKeyElementMap.elementIdentifierId,
						mapKeyElementMap.controllerMap.hardwareGuid
					);
					LogExceptionOnce(exceptionString);

					// Return `KeyCode.None` so that it defaults to using their map key.
					// (Their keybind isn't going to be 'None', so `jollyButtonDown` won't be overridden by it)
					return KeyCode.None;
				}
			}
			// If the player is using a keyboard.
			else
			{
				return playerControls.KeyboardMap;
			}
		}

		// Sends an exception to `exceptionLog.txt` as long has it hasn't already been logged before in this session.
		private static void LogExceptionOnce(string text)
		{
			// If this exceptionString hasn't been logged already. (`HashSet`s only allow unique objects)
			if (exceptionLogLog.Add(text))
			{
				Debug.LogException(new System.Exception(text));
			}
		}


		// A 2D dictionary containing Rewired `elementIdentifierName`s to their corresponding Unity `KeyCode`s,
		// categorised by the controller preset they belong to.
		// (Button names taken from the 'RewiredControllerElementIdentifiers.csv' file from the Rewired documentation)
		private static readonly Dictionary<Options.ControlSetup.Preset, Dictionary<string, KeyCode>> rewiredNameToKeyCode =
			new Dictionary<Options.ControlSetup.Preset, Dictionary<string, KeyCode>>
		{
			[Options.ControlSetup.Preset.PS4DualShock] = new Dictionary<string, KeyCode>
			{
				{ "Cross",				KeyCode.JoystickButton0 },
				{ "Circle",				KeyCode.JoystickButton1 },
				{ "Square",				KeyCode.JoystickButton2 },
				{ "Triangle",			KeyCode.JoystickButton3 },
				{ "L1",					KeyCode.JoystickButton4 },
				{ "R1",					KeyCode.JoystickButton5 },
				{ "Share",				KeyCode.JoystickButton6 },
				{ "Options",			KeyCode.JoystickButton7 },
				{ "Left Stick Button",	KeyCode.JoystickButton8 },
				{ "Right Stick Button",	KeyCode.JoystickButton9 }
			},
			[Options.ControlSetup.Preset.PS5DualSense] = new Dictionary<string, KeyCode>
			{
				{ "Cross",				KeyCode.JoystickButton0 },
				{ "Circle",				KeyCode.JoystickButton1 },
				{ "Square",				KeyCode.JoystickButton2 },
				{ "Triangle",			KeyCode.JoystickButton3 },
				{ "L1",					KeyCode.JoystickButton4 },
				{ "R1",					KeyCode.JoystickButton5 },
				{ "Create",				KeyCode.JoystickButton6 },
				{ "Options",			KeyCode.JoystickButton7 },
				{ "Left Stick Button",	KeyCode.JoystickButton8 },
				{ "Right Stick Button",	KeyCode.JoystickButton9 }
			},
			[Options.ControlSetup.Preset.XBox] = new Dictionary<string, KeyCode>
			{
				{ "A",					KeyCode.JoystickButton0 },
				{ "B",					KeyCode.JoystickButton1 },
				{ "X",					KeyCode.JoystickButton2 },
				{ "Y",					KeyCode.JoystickButton3 },
				{ "Left Shoulder",		KeyCode.JoystickButton4 },
				{ "Right Shoulder",		KeyCode.JoystickButton5 },
				{ "Back",				KeyCode.JoystickButton6 },
				{ "Start",				KeyCode.JoystickButton7 },
				{ "Left Stick Button",	KeyCode.JoystickButton8 },
				{ "Right Stick Button",	KeyCode.JoystickButton9 }
			},
			[Options.ControlSetup.Preset.SwitchProController] = new Dictionary<string, KeyCode>
			{
				{ "B",					KeyCode.JoystickButton0 },
				{ "A",					KeyCode.JoystickButton1 },
				{ "Y",					KeyCode.JoystickButton2 },
				{ "X",					KeyCode.JoystickButton3 },
				{ "L",					KeyCode.JoystickButton4 },
				{ "R",					KeyCode.JoystickButton5 },
				{ "- Button",			KeyCode.JoystickButton6 },
				{ "+ Button",			KeyCode.JoystickButton7 },
				{ "Left Stick",			KeyCode.JoystickButton8 },
				{ "Right Stick",		KeyCode.JoystickButton9 }
			}
		};
		// (Having all of these hardcoded in the mod is pretty awful, but this is genuinely the best way I've been able to find after over a week.)
	}
}
