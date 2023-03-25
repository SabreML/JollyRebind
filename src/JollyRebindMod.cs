using BepInEx;
using Rewired;
using System.Collections.Generic;
using System.Security.Permissions;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace JollyRebind
{
	[BepInPlugin("sabreml.jollyrebind", "JollyRebind", "1.1.0")]
	public class JollyRebindMod : BaseUnityPlugin
	{
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
			{ "Right Stick Button", KeyCode.JoystickButton9 }
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

			// If the player is using a controller.
			if (self.input[0].gamePad)
			{
				Options.ControlSetup controlSetup = RWCustom.Custom.rainWorld.options.controls[self.playerState.playerNumber];

				// Loop through every button on the controller which is currently bound to something in the game.
				// (I feel like there must be a method for this somewhere)
				foreach (ActionElementMap elementMap in controlSetup.gameControlMap.ButtonMaps)
				{
					// If the button is bound to the 'Map' action (11).
					if (elementMap.actionId == RewiredConsts.Action.Map)
					{
						// That's the one we're looking for.
						mapKeybind = rewiredElemToKeyCode[elementMap.elementIdentifierName];
						break;
					}
				}
			}
			// If the player is using a keyboard.
			else
			{
				mapKeybind = RWCustom.Custom.rainWorld.options.controls[self.playerState.playerNumber].KeyboardMap;
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
