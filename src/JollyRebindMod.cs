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
	[BepInPlugin("sabreml.jollyrebind", "JollyRebind", "1.1.1")]
	public class JollyRebindMod : BaseUnityPlugin
	{
		// A `HashSet` of previously logged controller element exceptions.
		// These are tracked because `JollyInputUpdate` happens once every frame, and this could very quickly spam the log file otherwise.
		private static readonly HashSet<string> exceptionLogLog = new HashSet<string>();

		// A dictionary of rewired `elementIdentifierName`s to their corresponding Unity `KeyCode`s.
		private static readonly Dictionary<string, KeyCode> rewiredElemToKeyCode = new Dictionary<string, KeyCode>
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
		};

		public void OnEnable()
		{
			On.RainWorld.OnModsInit += Init;
		}

		private void Init(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			orig(self);
			On.Player.JollyInputUpdate += JollyInputUpdateHK;
			JollyMenuKeybinds.SetupHooks();
			JollyRebindConfig.SetupHooks();

			MachineConnector.SetRegisteredOI(Info.Metadata.GUID, new JollyRebindConfig());
		}

		// If the player has a custom keybind set (i.e, not the map key), this method sets `jollyButtonDown` to `true` depending on if the key is being held down.
		private void JollyInputUpdateHK(On.Player.orig_JollyInputUpdate orig, Player self)
		{
			orig(self);

			// The player's pointing keybind from the jolly menu keybinder.
			KeyCode playerKeybind = JollyRebindConfig.PlayerPointInputs[self.playerState.playerNumber].Value;
			// The game's map keybind.
			KeyCode? mapKeybind = null;

			// The player's `Options.ControlSetup`, containing their Rewired data.
			Options.ControlSetup playerControls = RWCustom.Custom.rainWorld.options.controls[self.playerState.playerNumber];

			// If the player is using a controller.
			if (self.input[0].gamePad)
			{
				// Get the button on the controller which is bound to the 'Map' action.
				ActionElementMap mapKeyElementMap = playerControls.gameControlMap.ButtonMaps
					.First(elementMap => elementMap.actionId == RewiredConsts.Action.Map);

				// If that button's name is in the `rewiredElemToKeyCode` dictionary, 
				if (rewiredElemToKeyCode.TryGetValue(mapKeyElementMap.elementIdentifierName, out KeyCode keyCode))
				{
					// Set `mapKeybind` to the Rewired element's corresponding Unity `KeyCode`.
					mapKeybind = keyCode;
				}
				// Otherwise, make an exception log with the name and ID of the button the player is trying to use.
				else
				{
					string exceptionString = $"(JollyRebind) Unknown controller element '{mapKeyElementMap.elementIdentifierName} ({mapKeyElementMap.elementIdentifierId})'!";

					// If this exceptionString hasn't been logged already.
					if (exceptionLogLog.Add(exceptionString))
					{
						Debug.LogException(new System.Exception(exceptionString));
					}
				}
			}
			// If the player is using a keyboard.
			else
			{
				mapKeybind = playerControls.KeyboardMap;
			}

			// If the player's keybind is different to the game's map key.
			if (playerKeybind != mapKeybind)
			{
				// Override whatever `jollyButtonDown` was set to in `orig()`.
				self.jollyButtonDown = Input.GetKey(playerKeybind);
			}
		}
	}
}
