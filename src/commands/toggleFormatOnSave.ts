import * as vscode from 'vscode';

export function registerToggleFormatOnSave(): vscode.Disposable {
  return vscode.commands.registerCommand('riderLayout.toggleFormatOnSave', async () => {
    const config = vscode.workspace.getConfiguration('riderLayout');
    const current = config.get<boolean>('formatOnSave', false);
    await config.update('formatOnSave', !current, vscode.ConfigurationTarget.Global);
    void vscode.window.showInformationMessage(
      `Rider Layout: Format on Save ${!current ? 'enabled' : 'disabled'}.`
    );
  });
}