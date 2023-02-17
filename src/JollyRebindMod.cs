using BepInEx;
using System.Security.Permissions;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace JollyRebind
{
	[BepInPlugin("sabreml.jollyrebind", "JollyRebind", "1.0.0")]
	public class JollyRebindMod : BaseUnityPlugin
	{
		// The current mod version. (Stored here as a variable so that I don't have to update it in as many places.)
		public static string Version;

		public void OnEnable()
		{
			Version = Info.Metadata.Version.ToString();

			On.RainWorld.OnModsInit += Init;
		}

		private void Init(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			orig(self);
			On.Player.JollyInputUpdate += Player_JollyInputUpdateHK;
			JollyMenuKeybinds.SetupHooks();

			MachineConnector.SetRegisteredOI(Info.Metadata.GUID, new JollyRebindConfig());
		}

		private void Player_JollyInputUpdateHK(On.Player.orig_JollyInputUpdate orig, Player self)
		{
			orig(self); // TODO: Check if it still works when stunned
			KeyCode keybind = JollyRebindConfig.PlayerPointInputs[self.playerState.playerNumber].Value;
			if (keybind != KeyCode.Space) // If it's not just the default.
			{
				self.jollyButtonDown = Input.GetKey(keybind);
			}
		}
	}
}
