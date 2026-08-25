import * as vscode from 'vscode';
import { CliClient, defaultCliPath } from './cliClient';
import { RiderSettingsService } from './riderSettingsService';

export class LayoutEngineService {
  private cachedLayout?: string;
  private client?: CliClient;

  constructor(
    private readonly context: vscode.ExtensionContext,
    private readonly output: vscode.OutputChannel,
    private readonly settings: RiderSettingsService
  ) {}

  async reload(): Promise<void> {
    this.cachedLayout = undefined;
  }

  async pickLayoutFile(): Promise<string | undefined> {
    const options: vscode.OpenDialogOptions = {
      canSelectMany: false,
      canSelectFolders: false,
      openLabel: 'Select Rider Layout File',
      filters: {
        'Rider / ReSharper layout': ['xml', 'DotSettings', 'dotSettings'],
        'All files': ['*']
      }
    };

    const picked = await vscode.window.showOpenDialog(options);
    const file = picked?.[0];
    if (!file) return undefined;

    const xml = await this.settings.loadLayoutFromFile(file.fsPath);
    if (!xml) throw new Error('The selected file contains no <Patterns> layout block.');

    this.cachedLayout = xml;
    await vscode.workspace.getConfiguration('riderLayout').update('settingsPath', file.fsPath, vscode.ConfigurationTarget.Workspace);
    return file.fsPath;
  }

  async dispose(): Promise<void> {
    this.client?.dispose();
    this.client = undefined;
  }

  async rearrange(document: vscode.TextDocument): Promise<string> {
    const folder = vscode.workspace.getWorkspaceFolder(document.uri);
    if (!folder) throw new Error('Open a C# file inside a workspace so Rider settings can be discovered.');

    const layoutXml = this.cachedLayout ?? await this.settings.findLayoutXml(folder);
    if (!layoutXml) throw new Error('Could not find Rider File Layout settings for this workspace.');
    this.cachedLayout = layoutXml;

    const response = await this.getClient().request({
      command: 'rearrange',
      source: document.getText(),
      layoutXml,
      projectRoot: folder.uri.fsPath
    });

    if (!response.success) throw new Error(response.error ?? 'Rider Layout engine failed.');
    return response.source ?? document.getText();
  }

  async preview(document: vscode.TextDocument): Promise<string> {
    const folder = vscode.workspace.getWorkspaceFolder(document.uri);
    if (!folder) throw new Error('Open a C# file inside a workspace.');
    const layoutXml = this.cachedLayout ?? await this.settings.findLayoutXml(folder);
    if (!layoutXml) throw new Error('No Rider File Layout settings found.');
    return layoutXml;
  }

  private getClient(): CliClient {
    if (!this.client) {
      const configured = vscode.workspace.getConfiguration('riderLayout').get<string>('cliPath', '');
      const cliPath = configured || defaultCliPath(this.context.extensionPath);
      this.client = new CliClient(cliPath);
    }
    return this.client;
  }
}