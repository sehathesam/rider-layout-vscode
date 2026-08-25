import * as vscode from 'vscode';
import { LayoutEngineService } from '../services/layoutEngineService';

export function registerReloadLayout(engine: LayoutEngineService): vscode.Disposable {
  return vscode.commands.registerCommand('riderLayout.reload', async () => {
    await engine.reload();
    void vscode.window.showInformationMessage('Rider Layout settings cache cleared.');
  });
}
