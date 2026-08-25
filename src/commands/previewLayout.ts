import * as vscode from 'vscode';
import { LayoutEngineService } from '../services/layoutEngineService';

export function registerPreviewLayout(engine: LayoutEngineService): vscode.Disposable {
  return vscode.commands.registerCommand('riderLayout.preview', async () => {
    const editor = vscode.window.activeTextEditor;
    if (!editor || editor.document.languageId !== 'csharp') return;
    try {
      const xml = await engine.preview(editor.document);
      const doc = await vscode.workspace.openTextDocument({ language: 'xml', content: xml });
      await vscode.window.showTextDocument(doc, { preview: true });
    } catch (error) {
      void vscode.window.showErrorMessage(`Rider Layout: ${String(error)}`);
    }
  });
}
