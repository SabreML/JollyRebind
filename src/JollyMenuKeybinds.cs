using JollyCoop.JollyMenu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Rewired;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace JollyRebind
{
	public static class JollyMenuKeybinds
	{
		public static bool MenuReady = false;

		private static readonly UIelementWrapper[] keybindWrappers = new UIelementWrapper[4];

		public static void SetupHooks()
		{
			On.JollyCoop.JollyMenu.JollySetupDialog.ctor += JollySetupDialogHK;
			On.JollyCoop.JollyMenu.JollySetupDialog.ShutDownProcess += JollySetupDialog_ShutDownProcessHK;

			On.JollyCoop.JollyMenu.JollySlidingMenu.BindButtons += JollySlidingMenu_BindButtonsHK;

			On.JollyCoop.JollyMenu.JollyPlayerSelector.ctor += JollyPlayerSelectorHK;
			On.JollyCoop.JollyMenu.JollyPlayerSelector.Update += JollyPlayerSelector_UpdateHK;
			On.JollyCoop.JollyMenu.JollyPlayerSelector.AddColorButton += JollyPlayerSelector_AddColorButtonHK;

			new ILHook(
				typeof(OpKeyBinder).GetProperty("value", BindingFlags.Public | BindingFlags.Instance).GetSetMethod(),
				new ILContext.Manipulator(OpKeyBinder_set_value)
			);
		}

		// When `JollySetupDialog` is first opened, call `InitBoundKey()` as it's needed for `OpKeyBinder` to function.
		private static void JollySetupDialogHK(On.JollyCoop.JollyMenu.JollySetupDialog.orig_ctor orig, JollySetupDialog self, SlugcatStats.Name name, ProcessManager manager, Vector2 closeButtonPos)
		{
			InitBoundKey();
			orig(self, name, manager, closeButtonPos);
			MenuReady = true;
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
			MenuReady = false;
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
				Debug.Log($"(JollyRebind) Keybind UI added for player {index + 1}");
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

		private static void JollyPlayerSelector_AddColorButtonHK(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_AddColorButton orig, JollyPlayerSelector self)
		{
			orig(self);
			self.colorConfig.pos.y -= keybindWrappers[self.index].thisElement.size.y;
			self.colorConfig.lastPos.y -= keybindWrappers[self.index].thisElement.size.y;
		}


		// Adds a check to the setter of `OpKeyBinder.value` to stop it from playing a sound before the menu is fully loaded.
		// This is needed because otherwise all four keybind buttons will try to play a sound at the same time when the menu opens, resulting in a loud noise.
		private static void OpKeyBinder_set_value(ILContext il)
		{
			ILCursor cursor = new ILCursor(il);

			// Try to move the cursor to the instruction after the sound is played.
			if (!cursor.TryGotoNext(MoveType.After,
				i => i.MatchLdsfld<SoundID>("MENU_Button_Successfully_Assigned"),
				i => i.MatchCallvirt<Menu.Menu>("PlaySound")
			))
			{
				Debug.Log("(JollyRebind) IL edit failed!");
				return;
			}

			// Set this instruction as a label target.
			ILLabel label = cursor.MarkLabel();

			// Move back 4 lines to the instruction before the sound is played.
			cursor.Index -= 4;

			// Load the `MenuReady` bool onto the stack.
			cursor.Emit(OpCodes.Ldsfld, typeof(JollyMenuKeybinds).GetField("MenuReady", BindingFlags.Public | BindingFlags.Static));
			// If it's false, jump to `label`'s target instuction. (After the sound is played)
			cursor.Emit(OpCodes.Brfalse_S, label);

			// (Otherwise if the menu is open, do everything normally.)
		}


		// Exactly the same as `OpKeyBinder._InitBoundKey()`, except with the first few lines removed since they're dependent on `ConfigContainer`.
		private static void InitBoundKey()
		{
			OpKeyBinder._BoundKey = new Dictionary<string, string>();
			for (int i = 0; i < RWCustom.Custom.rainWorld.options.controls.Length; i++)
			{
				Options.ControlSetup controlSetup = RWCustom.Custom.rainWorld.options.controls[i];
				InputMapCategory mapCategory = ReInput.mapping.GetMapCategory(0);
				if (mapCategory == null || controlSetup?.gameControlMap == null)
				{
					continue;
				}
				InputCategory actionCategory = ReInput.mapping.GetActionCategory(mapCategory.name);
				if (actionCategory == null)
				{
					continue;
				}
				int num = 0;
				foreach (InputAction action in ReInput.mapping.ActionsInCategory(actionCategory.id))
				{
					foreach (ActionElementMap elementMap in controlSetup.gameControlMap.AllMaps)
					{
						if (elementMap != null && elementMap.actionId == action.id && !OpKeyBinder._BoundKey.ContainsValue(elementMap.elementIdentifierName))
						{
							OpKeyBinder._BoundKey[$"Vanilla_{i}_{num}"] = elementMap.elementIdentifierName;
						}
					}
					num++;
				}
			}
		}
	}
}
