import * as vscode from 'vscode';

export function registerAddIgnoreFolder(): vscode.Disposable {
  return vscode.commands.registerCommand('riderLayout.addIgnoreFolder', async () => {
    const options: vscode.OpenDialogOptions = {
      canSelectMany: false,
      canSelectFolders: true,
      canSelectFiles: false,
      openLabel: 'Ignore Layout Folder'
    };

    const picked = await vscode.window.showOpenDialog(options);
    const folder = picked?.[0];
    if (!folder) return;

    const config = vscode.workspace.getConfiguration('riderLayout');
    const current = config.get<string[]>('ignoreFolders', []);
    const name = folder.fsPath.split(/[\\/]/).pop() ?? folder.fsPath;

    if (current.includes(name)) {
      void vscode.window.showInformationMessage(`"${name}" is already ignored.`);
      return;
    }

    await config.update('ignoreFolders', [...current, name], vscode.ConfigurationTarget.Workspace);
    void vscode.window.showInformationMessage(`"${name}" will be ignored by "Rearrange All C# Files".`);
  });
}