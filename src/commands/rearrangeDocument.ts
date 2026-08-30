import * as vscode from 'vscode';
import { LayoutEngineService } from '../services/layoutEngineService';
import { differsStructurally, isShownInDiffEditor, isWorkspaceDocument } from '../utils/documentUtils';

export function registerRearrangeDocument(engine: LayoutEngineService): vscode.Disposable {
  return vscode.commands.registerCommand('riderLayout.rearrangeDocument', async () => {
    const editor = vscode.window.activeTextEditor;
    if (!editor || editor.document.languageId !== 'csharp') return;
    if (!isWorkspaceDocument(editor.document)) return;
    if (isShownInDiffEditor(editor.document)) return;

    try {
      const output = await engine.rearrange(editor.document);
      if (!differsStructurally(editor.document.getText(), output)) {
        void vscode.window.showInformationMessage('Rider Layout: no changes needed.');
        return;
      }
      const fullRange = new vscode.Range(editor.document.positionAt(0), editor.document.positionAt(editor.document.getText().length));
      await editor.edit(edit => edit.replace(fullRange, output), { undoStopBefore: true, undoStopAfter: true });
      await engine.formatDocument(editor.document);
    } catch (error) {
      void vscode.window.showErrorMessage(`Rider Layout: ${String(error)}`);
    }
  });
}
