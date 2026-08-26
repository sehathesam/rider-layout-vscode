import * as vscode from 'vscode';

export function registerToggleRegions(command = 'riderLayout.toggleRegions'): vscode.Disposable {
  return vscode.commands.registerCommand(command, async () => {
    const config = vscode.workspace.getConfiguration('riderLayout');
    const current = config.get<boolean>('emitRegions', true);
    await config.update('emitRegions', !current, vscode.ConfigurationTarget.Global);
    void vscode.window.showInformationMessage(
      `Rider Layout: Region blocks ${!current ? 'enabled' : 'disabled'}.`
    );
  });
}