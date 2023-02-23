using BepInEx;
using System.Security.Permissions;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace JollyRebind
{
	[BepInPlugin("sabreml.jollyrebind", "JollyRebind", "1.0.1")]
	public class JollyRebindMod : BaseUnityPlugin
	{
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
		private static void JollyInputUpdateHK(On.Player.orig_JollyInputUpdate orig, Player self)
		{
			orig(self);

			KeyCode playerKeybind = JollyRebindConfig.PlayerPointInputs[self.playerState.playerNumber].Value;
			KeyCode defaultKeybind;

			if (self.input[0].gamePad)
			{
				defaultKeybind = RWCustom.Custom.rainWorld.options.controls[self.playerState.playerNumber].GamePadMap;
			}
			else
			{
				defaultKeybind = RWCustom.Custom.rainWorld.options.controls[self.playerState.playerNumber].KeyboardMap;
			}

			// If the player has a custom keybind.
			if (playerKeybind != defaultKeybind)
			{
				self.jollyButtonDown = Input.GetKey(playerKeybind);
			}
		}
	}
}
