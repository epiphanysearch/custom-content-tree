using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Epiphany.CustomContentTree
{
	/// <summary>
	/// ConfigItem class, used to store the settings from the config file
	/// </summary>
	class ConfigItem
	{
		public List<int> Users { get; set; }
		public Dictionary<int, bool> Nodes { get; set; }

		public ConfigItem()
		{
			Users = new List<int>();
			Nodes = new Dictionary<int, bool>();
		}
	}
}
