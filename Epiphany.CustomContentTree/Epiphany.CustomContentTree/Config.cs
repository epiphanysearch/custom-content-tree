using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Web;

namespace Epiphany.CustomContentTree
{
	class Config
	{
		//private backing fields
		private List<ConfigItem> _configEntries;
		private static Config _instance;
		private bool _useCustomMenus;

		//public properties
		public List<ConfigItem> ConfigEntries
		{
			get
			{
				return this._configEntries;
			}
		}

		public bool UseCustomMenus
		{
			get
			{
				return this._useCustomMenus;
			}
		}

		public static Config Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new Config();
				}
				return _instance;
			}
		}

		/// <summary>
		/// Creates the Config option, initialises collections and loads config information from the file
		/// </summary>
		private Config()
		{
			_configEntries = new List<ConfigItem>();

			LoadXmlConfig();
		}

		/// <summary>
		/// Loads the content from the config file
		/// </summary>
		private void LoadXmlConfig()
		{
			//load document
			XmlDocument document = new XmlDocument();
			document.Load(HttpContext.Current.Server.MapPath("~/config/customContentTree.config"));

			//add whether to run attackmonkey custom menus events
			XmlNode temp = document.SelectSingleNode("/customContentTree/useCustomMenus");
			_useCustomMenus = false;
			if (temp != null)
			{
				if (!string.IsNullOrEmpty(temp.InnerText))
				{
					_useCustomMenus = Convert.ToBoolean(temp.InnerText);
				}
			}

			//loop through each config item and set it up
			foreach (XmlNode node in document.SelectNodes("/customContentTree/rules/tree"))
			{
				if (node.NodeType != XmlNodeType.Element)
				{
					continue;
				}

				ConfigItem item = new ConfigItem();

				item.Users = node.Attributes["userIds"].Value.Split(',').Select(a => int.Parse(a)).ToList();

				foreach (XmlNode page in node.SelectNodes("./node"))
				{
					if (page.Attributes["id"] != null)
					{
						bool hideForDialog = false;

						int pageId = int.Parse(page.Attributes["id"].Value);

						if (page.Attributes["hideForDialog"] != null)
						{
							hideForDialog = Convert.ToBoolean(page.Attributes["hideForDialog"].Value);
						}

						if (!item.Nodes.ContainsKey(pageId))
						{
							item.Nodes.Add(pageId, hideForDialog);
						}
					}
				}

				_configEntries.Add(item);
			}
		}

		/// <summary>
		/// Clears the singleton, so that it's refreshed next time someone asks for it
		/// </summary>
		public static void RefreshInstance()
		{
			_instance = null;
		}
	}
}
