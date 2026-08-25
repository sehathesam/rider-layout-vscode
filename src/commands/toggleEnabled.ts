import * as vscode from 'vscode';

export function registerToggleEnabled(): vscode.Disposable {
  return vscode.commands.registerCommand('riderLayout.toggleEnabled', async () => {
    const config = vscode.workspace.getConfiguration('riderLayout');
    const current = config.get<boolean>('enabled', true);
    await config.update('enabled', !current, vscode.ConfigurationTarget.Workspace);
    const message = !current ? 'Auto-apply enabled.' : 'Auto-apply disabled.';
    void vscode.window.showInformationMessage(`Rider Layout: ${message}`);
  });
}