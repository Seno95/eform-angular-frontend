export class NavigationMenuModel {
  menuTemplates: NavigationMenuTemplateModel[];
  actualMenu: NavigationMenuItemModel[];
}

export class NavigationMenuTemplateModel {
  id: number;
  name: string;
  pluginId: number | null;
  collapsed: boolean;
  items: NavigationMenuTemplateItemModel[];
}

export class NavigationMenuTemplateItemModel {
  id: number;
  name: string;
  link: string;
}

export class NavigationMenuItemModel {
  id: number;
  name: string;
  isDropdown: boolean;
  collapsed = true;
  link: string | null;
  children: NavigationMenuItemModel[] | null;
  relatedPluginId: number | null;
  relatedTemplateItemId: number | null;
  parentId: number | null;
  position: number;
}
