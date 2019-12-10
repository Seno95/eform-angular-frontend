export class InstalledPluginsModel {
  total: number;
  pluginsList: Array<InstalledPluginModel> = [];
}

export class InstalledPluginModel {
  id: number;
  pluginId: number;
  name: string;
  version: string;
  baseUrl: string;
  status: number;
}
