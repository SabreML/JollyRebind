using UnityEngine;

namespace JollyRebind
{
	public class JollyRebindConfig : OptionInterface
	{
		public static readonly Configurable<KeyCode>[] PlayerPointInputs = new Configurable<KeyCode>[4];

		public JollyRebindConfig()
		{
			for (int i = 0; i < PlayerPointInputs.Length; i++)
			{
				PlayerPointInputs[i] = config.Bind($"pointInputP{i}", KeyCode.Space, new ConfigurableInfo("placeholder 1", tags: new string[]
				{
					"placeholder 2"
				}));
			}
		}
	}
}
