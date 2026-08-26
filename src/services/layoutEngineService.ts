import * as vscode from 'vscode';
import * as path from 'node:path';
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

    const target = await this.pickScope();
    if (!target) return undefined;

    this.cachedLayout = xml;
    await vscode.workspace.getConfiguration('riderLayout').update('settingsPath', file.fsPath, target);
    this.output.appendLine(`Rider layout stored (${target === vscode.ConfigurationTarget.Global ? 'global' : 'workspace'}): ${file.fsPath}`);
    return file.fsPath;
  }

  private async pickScope(): Promise<vscode.ConfigurationTarget | undefined> {
    interface ScopePick extends vscode.QuickPickItem {
      id: 'global' | 'workspace';
    }
    const items: ScopePick[] = [
      {
        id: 'global',
        label: 'Global (all projects)',
        description: 'Use this layout for every workspace on this machine',
        picked: true
      },
      {
        id: 'workspace',
        label: 'Workspace only',
        description: 'Use this layout only for the current workspace'
      }
    ];
    const picked = await vscode.window.showQuickPick(items, {
      placeHolder: 'Where should this layout be stored?'
    });
    if (!picked) return undefined;
    return picked.id === 'global'
      ? vscode.ConfigurationTarget.Global
      : vscode.ConfigurationTarget.Workspace;
  }

  async dispose(): Promise<void> {
    this.client?.dispose();
    this.client = undefined;
  }

  async rearrange(document: vscode.TextDocument): Promise<string> {
    const folder = vscode.workspace.getWorkspaceFolder(document.uri)
      ?? { uri: vscode.Uri.file(path.dirname(document.uri.fsPath)) };

    const layoutXml = await this.resolveLayout(folder.uri.fsPath);
    const config = vscode.workspace.getConfiguration('riderLayout');
    const emitRegions = config.get<boolean>('emitRegions', true);
    const regions = emitRegions ? config.get<string[]>('regions', []) : [];

    const response = await this.getClient().request({
      command: 'rearrange',
      source: document.getText(),
      layoutXml,
      projectRoot: folder.uri.fsPath,
      regions
    });

    if (!response.success) throw new Error(response.error ?? 'Rider Layout engine failed.');
    return response.source ?? document.getText();
  }

  async preview(document: vscode.TextDocument): Promise<string> {
    const folder = vscode.workspace.getWorkspaceFolder(document.uri)
      ?? { uri: vscode.Uri.file(path.dirname(document.uri.fsPath)) };
    return this.resolveLayout(folder.uri.fsPath);
  }

  async formatDocument(document: vscode.TextDocument): Promise<void> {
    const format = vscode.workspace.getConfiguration('riderLayout').get<boolean>('formatAfterRearrange', true);
    if (!format) return;
    const editor = vscode.window.visibleTextEditors.find(e => e.document === document)
      ?? vscode.window.activeTextEditor;
    if (!editor) return;
    await vscode.commands.executeCommand('editor.action.formatDocument');
  }

  private async resolveLayout(projectRoot: string): Promise<string> {
    if (!this.cachedLayout) {
      const { xml, missingFile } = await this.settings.resolve(projectRoot);
      if (missingFile) throw new Error(`Rider layout file is missing or invalid: ${missingFile}`);
      if (!xml) throw new Error('No Rider layout is configured. Use "Rider Layout: Select Layout File" first.');
      this.cachedLayout = xml;
    }
    return this.cachedLayout;
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