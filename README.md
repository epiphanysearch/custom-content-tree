# Custom Content Tree for Umbraco

## Installation

To install the package, go to the package repository on [our.umbraco.org](http://our.umbraco.com) and download the package.

## Usage

This package gives you a way of defining multiple starting URLs for the content tree (rather than the one that you can normally add). It acts as a REPLACEMENT for the user start ID functionality. It WILL NOT work if the user has a starting node id set for content. This is because Umbraco checks that a user has the correct permissions to view a page when you edit it, and if you add a page outside of their start node, they'll see it in the list, but when they click to edit it, they'll get a permissions error screen, as they don't have access to the page. If you are worried about users hacking URLs in the back office to get at pages they shouldn't, you can use the page level permissions to stop that from happening.

The package currently has a dependency on the **AttackMonkey CustomMenus** package. Please ensure that you have downloaded and installed the [latest version of the package from Our](http://our.umbraco.org/projects/backoffice-extensions/attackmonkey-custom-menus) (at least version 1.3): 

**NOTE: if the CustomMenus package is not installed, the CustomContentTrees package will not work.**

## Configuration

Once the package has been installed, you will see that a new config file `customContentTree.config` has been added to the site config folder. Use this folder to set up the custom content rules. The file contains an example configuration. The settings are as follows:

`useCustomMenus` - can be true or false, if set to true, Custom Menu rules from the AttackMonkey custom menus package will be applied to the new root node items.

`rules` - contains the custom trees that you have defined.

`tree` - you can create as many of these as you require, a tree has the following properties:

`userIds` - a comma separated list of the back office user IDs that you want to apply the rules to

`node` - this represents a node in the content tree that will be displayed to the user in the content section, it has two properties:

`id` - the id of the document to show in the tree

`hideForDialog` - can be true or false, if set for true, this node will not be rendered if the tree is being used in a dialog (e.g. a picker)


## KNOWN ISSUES/LIMITATIONS

- if for some reason you define multiple trees for the same user, only the first one will be used.
- the dependency on the CustomMenus package.
- because of the way the tree is rendered, events cannot be fired on the new root nodes, I'm looking into a way round this.