import * as vscode from 'vscode';
import { LayoutEngineService } from '../services/layoutEngineService';

export function registerResetLayout(engine: LayoutEngineService): vscode.Disposable {
  return vscode.commands.registerCommand('riderLayout.resetLayout', async () => {
    try {
      const source = await engine.resetToDefault();
      if (source) {
        void vscode.window.showInformationMessage(`Rider Layout: reset to default layout.`);
      } else {
        void vscode.window.showErrorMessage('Rider Layout: bundled default layout could not be loaded.');
      }
    } catch (error) {
      void vscode.window.showErrorMessage(`Rider Layout: ${String(error)}`);
    }
  });
}