import * as vscode from 'vscode';
import { LayoutEngineService } from '../services/layoutEngineService';

export function registerFormatOnSave(engine: LayoutEngineService): vscode.Disposable {
  return vscode.workspace.onWillSaveTextDocument(event => {
    if (event.document.languageId !== 'csharp') return;

    const config = vscode.workspace.getConfiguration('riderLayout');
    if (!config.get<boolean>('enabled', true)) return;
    if (!config.get<boolean>('formatOnSave', false)) return;

    const format = async (): Promise<void> => {
      try {
        const output = await engine.rearrange(event.document);
        if (output === event.document.getText()) return;

        const fullRange = new vscode.Range(
          event.document.positionAt(0),
          event.document.positionAt(event.document.getText().length)
        );
        const edit = new vscode.WorkspaceEdit();
        edit.replace(event.document.uri, fullRange, output);
        await vscode.workspace.applyEdit(edit);
        await engine.formatDocument(event.document);
      } catch (error) {
        const message = error instanceof Error ? error.message : String(error);
        void vscode.window.showWarningMessage(`Rider Layout: ${message}`);
      }
    };
    event.waitUntil(format());
  });
}