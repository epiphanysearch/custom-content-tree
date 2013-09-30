using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using umbraco.cms.presentation.Trees;
using umbraco.BusinessLogic;
using umbraco.BasePages;
using umbraco.cms.businesslogic.web;
using umbraco.BusinessLogic.Actions;
using umbraco.interfaces;
using umbraco.presentation.umbracobase.library;
using umbraco;
using System.Reflection;

namespace Epiphany.CustomContentTree
{
	public class CustomContentTree : umbraco.BusinessLogic.ApplicationBase
	{
		private BaseTree tree = null;

		public CustomContentTree()
		{
			//set up event listeners, in this case after the tree has rendered
			BaseContentTree.AfterTreeRender += new EventHandler<TreeEventArgs>(SetUpCustomContentTree);

			if (HttpContext.Current.Application["customContentTree"] == null)
			{
				//code to expire config if the config file is changed
				string path = HttpContext.Current.Server.MapPath("~/config/");
				HttpContext.Current.Application.Add("customContentTree", new FileSystemWatcher(path));
				FileSystemWatcher watcher = (FileSystemWatcher)HttpContext.Current.Application["customContentTree"];
				watcher.EnableRaisingEvents = true;
				watcher.IncludeSubdirectories = true;
				watcher.Changed += new FileSystemEventHandler(this.expireConfig);
				watcher.Created += new FileSystemEventHandler(this.expireConfig);
				watcher.Deleted += new FileSystemEventHandler(this.expireConfig);
			}
		}

		/// <summary>
		/// Expires the config for the package
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void expireConfig(object sender, FileSystemEventArgs e)
		{
			Config.RefreshInstance();
		}

		private void SetUpCustomContentTree(object sender, TreeEventArgs e)
		{
			bool runEventAction = false;

			int currentUserId = CurrentUser.Id;

			if (e.Tree.treeCollection.Count > 0 && Config.Instance.ConfigEntries.Any(a => a.Users.Contains(currentUserId)))
			{
				if (e.Tree.treeCollection[0].NodeType == "content")
				{
					string sectionId = HttpContext.Current.Request.QueryString["id"];

					if (!string.IsNullOrEmpty(sectionId))
					{
						if (sectionId == "-1")
						{
							runEventAction = true;
						}
					}
				}
			}
			
			if (runEventAction == true)
			{
				//set up reflection based stuff to load custom menus
				Assembly assembly = null;
				Type type = null;

				//load in the assemblies we need
				if (Config.Instance.UseCustomMenus == true)
				{
					assembly = Assembly.Load("AttackMonkey.CustomMenus");
					type = assembly.GetType("AttackMonkey.CustomMenus.ApplicationBase");
				}
				
				//get config item and set up tree
				ConfigItem configItem = Config.Instance.ConfigEntries.First(a => a.Users.Contains(currentUserId));

				tree = TreeDefinitionCollection.Instance.FindTree<loadContent>().CreateInstance();

				//set the dialog mode and context menu attributes of the tree so that it renders correctly
				if (HttpContext.Current.Request.QueryString["isDialog"] == "true")
				{
					tree.IsDialog = true;
				}

				if (HttpContext.Current.Request.QueryString["contextMenu"] == "false")
				{
					tree.ShowContextMenu = false;
				}

				//clear the tree and replace it with the custom one 
				e.Tree.treeCollection.Clear();

				foreach(var rootNode in configItem.Nodes)
				{
					if (tree.IsDialog == true && rootNode.Value == true)
					{
						//do nothing, as we've set the node to be hidden when you are in dialog mode
					}
					else
					{
						Document document = new Document(rootNode.Key);

						if (document != null)
						{
							List<IAction> allowedUserOptions = GetUserActionsForNode(document);
							if (CanUserAccessNode(document, allowedUserOptions))
							{
								XmlTreeNode node = CreateNode(document, allowedUserOptions);
							
								if (Config.Instance.UseCustomMenus == true)
								{
									if (type != null)
									{
										MethodInfo methodInfo = type.GetMethod("ProcessMenu");
										if (methodInfo != null)
										{
											object result = null;
											
											ParameterInfo[] parameters = methodInfo.GetParameters();
											
											object classInstance = Activator.CreateInstance(type, null);
											
											object[] parametersArray = new object[] { node };
             
											result = methodInfo.Invoke(classInstance, parametersArray);
										}
									}
								}

								if (node != null)
								{
									e.Tree.Add(node);
								}
							}
						}
					}
				}
			}
		}

		//These methods are copied from the default content tree code, as they aren't publicly exposed to the outside world
		protected List<IAction> GetUserActionsForNode(Document dd)
		{
			List<IAction> actions = umbraco.BusinessLogic.Actions.Action.FromString(CurrentUser.GetPermissions(dd.Path));

			// A user is allowed to delete their own stuff
			if (dd.UserId == CurrentUser.Id && !actions.Contains(ActionDelete.Instance))
			{
				actions.Add(ActionDelete.Instance);
			}

			return actions;
		}

		protected virtual bool CanUserAccessNode(Document doc, List<IAction> allowedUserOptions)
		{
			if (allowedUserOptions.Contains(ActionBrowse.Instance))
			{
				return true;
			}

			return false;
		}

		protected XmlTreeNode CreateNode(Document dd, List<IAction> allowedUserOptions)
		{
			XmlTreeNode node = XmlTreeNode.Create(tree);
			SetMenuAttribute(ref node, allowedUserOptions);
			node.NodeID = dd.Id.ToString();
			node.Text = dd.Text;
			SetNonPublishedAttribute(ref node, dd);
			SetProtectedAttribute(ref node, dd);
			SetActionAttribute(ref node, dd);
			SetSourcesAttributes(ref node, dd);
			if (dd.ContentTypeIcon != null)
			{
				node.Icon = dd.ContentTypeIcon;
				node.OpenIcon = dd.ContentTypeIcon;
			}
			if (!dd.Published)
				node.Style.DimNode();
			return node;
		}

		protected List<IAction> RemoveDuplicateMenuDividers(List<IAction> actions)
		{
			string fullMenu = umbraco.BusinessLogic.Actions.Action.ToString(actions);
			while (fullMenu.IndexOf(",,") > 0) //remove all occurances of duplicate dividers
				fullMenu = fullMenu.Replace(",,", ",");
			fullMenu = fullMenu.Trim(new char[] { ',' }); //remove any ending dividers
			return umbraco.BusinessLogic.Actions.Action.FromString(fullMenu);
		}

		protected List<IAction> GetUserAllowedActions(List<IAction> actions, List<IAction> userAllowedActions)
		{
			return actions.FindAll(
				delegate(IAction a)
				{
					return (!a.CanBePermissionAssigned || (a.CanBePermissionAssigned && userAllowedActions.Contains(a)));
				}
			);
		}

		protected string CreateNodeLink(Document dd)
		{
			string nodeLink = umbraco.library.NiceUrl(dd.Id);
			if (nodeLink == "")
			{
				nodeLink = "/" + dd.Id;
				if (!GlobalSettings.UseDirectoryUrls)
					nodeLink += ".aspx";
			}
			return nodeLink;
		}

		#region Tree Attribute Setter Methods
		protected void SetNonPublishedAttribute(ref XmlTreeNode treeElement, Document dd)
		{
			treeElement.NotPublished = false;
			if (dd.Published)
			{
				//if (Math.Round(new TimeSpan(dd.UpdateDate.Ticks - dd.VersionDate.Ticks).TotalSeconds, 0) > 1)
				//    treeElement.NotPublished = true;
				treeElement.NotPublished = dd.HasPendingChanges();
			}
			else
			{
				treeElement.NotPublished = true;
			}
		}
		protected void SetProtectedAttribute(ref XmlTreeNode treeElement, Document dd)
		{
			if (Access.IsProtected(dd.Id, dd.Path))
			{
				treeElement.IsProtected = true;
			}
			else
			{
				treeElement.IsProtected = false;
			}
		}
		protected void SetActionAttribute(ref XmlTreeNode treeElement, Document dd)
		{
			// Check for dialog behaviour
			if (tree.DialogMode == TreeDialogModes.fulllink)
			{
				string nodeLink = CreateNodeLink(dd);
				treeElement.Action = String.Format("javascript:openContent('{0}');", nodeLink);
			}
			else if (tree.DialogMode == TreeDialogModes.locallink)
			{
				string nodeLink = string.Format("{{localLink:{0}}}", dd.Id);
				string nodeText = dd.Text.Replace("'", "\\'");
				// try to make a niceurl too
				string niceUrl = umbraco.library.NiceUrl(dd.Id).Replace("'", "\\'"); ;
				if (niceUrl != "#" || niceUrl != "")
				{
					nodeLink += "|" + niceUrl + "|" + HttpContext.Current.Server.HtmlEncode(nodeText);
				}
				else
				{
					nodeLink += "||" + HttpContext.Current.Server.HtmlEncode(nodeText);
				}

				treeElement.Action = String.Format("javascript:openContent('{0}');", nodeLink);
			}
			else if (tree.DialogMode == TreeDialogModes.id || tree.DialogMode == TreeDialogModes.none)
			{
				treeElement.Action = String.Format("javascript:openContent('{0}');", dd.Id.ToString());
			}
			else if (!tree.IsDialog || (tree.DialogMode == TreeDialogModes.id))
			{
				if (CurrentUser.GetPermissions(dd.Path).Contains(ActionUpdate.Instance.Letter.ToString()))
				{
					treeElement.Action = String.Format("javascript:openContent({0});", dd.Id);
				}
			}
		}
		protected void SetSourcesAttributes(ref XmlTreeNode treeElement, Document dd)
		{
			treeElement.HasChildren = dd.HasChildren;
			if (!tree.IsDialog)
			{
				treeElement.Source = tree.GetTreeServiceUrl(dd.Id);
			}
			else
			{
				treeElement.Source = tree.GetTreeDialogUrl(dd.Id);
			}
		}
		protected void SetMenuAttribute(ref XmlTreeNode treeElement, List<IAction> allowedUserActions)
		{
			//clear menu if we're to hide it
			if (!tree.ShowContextMenu)
			{
				treeElement.Menu = null;
			}
			else
			{
				//use this code to allow ALL menu items
				treeElement.Menu = RemoveDuplicateMenuDividers(GetUserAllowedActions(tree.AllowedActions, allowedUserActions));
			}
		}
		#endregion

		protected User CurrentUser
		{
			get
			{
				return (UmbracoEnsuredPage.CurrentUser);
			}
		}
	}
}
