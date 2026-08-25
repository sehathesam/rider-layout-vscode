import * as vscode from 'vscode';
import { LayoutEngineService } from '../services/layoutEngineService';

export function registerPickLayoutFile(engine: LayoutEngineService): vscode.Disposable {
  return vscode.commands.registerCommand('riderLayout.pickLayoutFile', async () => {
    try {
      const file = await engine.pickLayoutFile();
      if (!file) return;
      void vscode.window.showInformationMessage(`Rider Layout: using ${file}`);
    } catch (error) {
      void vscode.window.showErrorMessage(`Rider Layout: ${String(error)}`);
    }
  });
}