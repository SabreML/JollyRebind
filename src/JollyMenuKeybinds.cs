﻿using JollyCoop.JollyMenu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using System.Collections.Generic;
using UnityEngine;

namespace JollyRebind
{
	public static class JollyMenuKeybinds
	{
		private static readonly UIelementWrapper[] keybindWrappers = new UIelementWrapper[4];

		public static void SetupHooks()
		{
			On.JollyCoop.JollyMenu.JollySetupDialog.ctor += JollySetupDialogHK;
			On.JollyCoop.JollyMenu.JollySetupDialog.ShutDownProcess += JollySetupDialog_ShutDownProcessHK;

			On.JollyCoop.JollyMenu.JollySlidingMenu.BindButtons += JollySlidingMenu_BindButtonsHK;

			On.JollyCoop.JollyMenu.JollyPlayerSelector.ctor += JollyPlayerSelectorHK;
			On.JollyCoop.JollyMenu.JollyPlayerSelector.Update += JollyPlayerSelector_UpdateHK;
		}

		// When `JollySetupDialog` is first opened, call `InitBoundKey()` as it's needed for `OpKeyBinder` to function.
		private static void JollySetupDialogHK(On.JollyCoop.JollyMenu.JollySetupDialog.orig_ctor orig, JollySetupDialog self, SlugcatStats.Name name, ProcessManager manager, Vector2 closeButtonPos)
		{
			InitBoundKey();
			orig(self, name, manager, closeButtonPos);
		}

		// When the menu is unloaded, save all of the keybinds and empty the array.
		private static void JollySetupDialog_ShutDownProcessHK(On.JollyCoop.JollyMenu.JollySetupDialog.orig_ShutDownProcess orig, JollySetupDialog self)
		{
			orig(self);
			for (int i = 0; i < keybindWrappers.Length; i++)
			{
				if (keybindWrappers[i] != null)
				{
					keybindWrappers[i].SaveConfig();
					keybindWrappers[i] = null;
				}
			}
		}


		// Fix some errors introduced in moving around the menu with directional inputs, caused by adding new buttons.
		private static void JollySlidingMenu_BindButtonsHK(On.JollyCoop.JollyMenu.JollySlidingMenu.orig_BindButtons orig, JollySlidingMenu self)
		{
			orig(self);
			// Pressing left with the leftmost class button selected selects the rightmost pup button.
			self.playerSelector[0].classButton.nextSelectable[0] = self.playerSelector[self.playerSelector.Length - 1].pupButton;

			// Pressing right with the rightmost pup button selected selects the leftmost class button.
			self.playerSelector[self.playerSelector.Length - 1].pupButton.nextSelectable[2] = self.playerSelector[0].classButton;

			// Same as above but with the keybind buttons.
			keybindWrappers[0].nextSelectable[0] = keybindWrappers[keybindWrappers.Length - 1];
			keybindWrappers[keybindWrappers.Length - 1].nextSelectable[2] = keybindWrappers[0];
		}


		// Create a keybind interface with each player selector.
		private static void JollyPlayerSelectorHK(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_ctor orig, JollyPlayerSelector self, JollySetupDialog menu, Menu.MenuObject owner, Vector2 pos, int index)
		{
			orig(self, menu, owner, pos, index);
			if (keybindWrappers[index] == null)
			{
				OpKeyBinder keyBinder = new OpKeyBinder(
					JollyRebindConfig.PlayerPointInputs[index],
					pos - new Vector2(10f, 73f),
					new Vector2(120f, 35f),
					false
				);
				keyBinder.description = $"Click to change Player {index + 1}'s point button";
				keyBinder.OnValueUpdate += KeybindValueUpdated;

				keybindWrappers[index] = new UIelementWrapper(menu.tabWrapper, keyBinder);
			}
		}

		// Called by the `OpKeyBinder.OnValueUpdate` event.
		// This is used to block setting the button to 'none' when the escape key is pressed.
		private static void KeybindValueUpdated(UIconfig config, string newValue, string oldValue)
		{
			// If the value been has changed to 'none'.
			if (newValue != oldValue && newValue == OpKeyBinder.NONE)
			{
				// Set the value back to whatever it was previously.
				config._value = oldValue;
			}
		}

		// Make the keybind configs greyed out if their parent `JollyPlayerSelector` is.
		private static void JollyPlayerSelector_UpdateHK(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_Update orig, JollyPlayerSelector self)
		{
			orig(self);
			keybindWrappers[self.index].ThisConfig.greyedOut = !self.Joined;
		}


		// Exactly the same as `OpKeyBinder._InitBoundKey()`, except with the first few lines removed since they're dependent on `ConfigContainer`.
		private static void InitBoundKey()
		{
			OpKeyBinder._BoundKey = new Dictionary<string, string>();
			for (int i = 0; i < RWCustom.Custom.rainWorld.options.controls.Length; i++)
			{
				Options.ControlSetup controlSetup = RWCustom.Custom.rainWorld.options.controls[i];
				if (controlSetup.preset == Options.ControlSetup.Preset.KeyboardSinglePlayer)
				{
					for (int j = 0; j < controlSetup.keyboardKeys.Length; j++)
					{
						if (!OpKeyBinder._BoundKey.ContainsValue(controlSetup.keyboardKeys[j].ToString()))
						{
							OpKeyBinder._BoundKey.Add($"Vanilla_{i}_{j}", controlSetup.keyboardKeys[j].ToString());
						}
					}
				}
				else
				{
					for (int j = 0; j < controlSetup.gamePadButtons.Length; j++)
					{
						string button = controlSetup.gamePadButtons[j].ToString();
						if (button.Length <= 9 || !int.TryParse(button.Substring(8, 1), out _))
						{
							button = button.Substring(0, 8) + i + button.Substring(8);
						}
						if (!OpKeyBinder._BoundKey.ContainsValue(button))
						{
							OpKeyBinder._BoundKey.Add($"Vanilla_{i}_{j}", button);
						}
					}
				}
			}
		}
	}
}
