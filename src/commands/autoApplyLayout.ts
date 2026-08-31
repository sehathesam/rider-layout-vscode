import * as vscode from 'vscode';
import { LayoutEngineService } from '../services/layoutEngineService';
import { differsStructurally, isShownInDiffEditor, isWorkspaceDocument } from '../utils/documentUtils';
import { isIgnoredDocument } from '../utils/ignoreFolders';

export function registerAutoApplyLayout(engine: LayoutEngineService): vscode.Disposable {
  const applied = new WeakSet<vscode.TextDocument>();
  let inFlight = false;

  async function applyIfNeeded(editor?: vscode.TextEditor): Promise<void> {
    if (!editor || editor.document.languageId !== 'csharp') return;
    if (!isWorkspaceDocument(editor.document)) return;
    if (isShownInDiffEditor(editor.document)) return;
    if (isIgnoredDocument(editor.document)) return;
    if (inFlight) return;

    const config = vscode.workspace.getConfiguration('riderLayout');
    if (!config.get<boolean>('enabled', true)) return;
    if (!config.get<boolean>('autoApplyOnFocus', true)) return;
    if (applied.has(editor.document)) return;

    try {
      inFlight = true;
      const output = await engine.rearrange(editor.document);
      if (differsStructurally(editor.document.getText(), output)) {
        const fullRange = new vscode.Range(
          editor.document.positionAt(0),
          editor.document.positionAt(editor.document.getText().length)
        );
        await editor.edit(e => e.replace(fullRange, output), {
          undoStopBefore: false,
          undoStopAfter: false
        });
        await engine.formatDocument(editor.document);
      }
      applied.add(editor.document);
    } catch (error) {
      // A default layout is always bundled, so a failure here is an engine
      // problem worth surfacing, but "Select Layout File" is obsolete. Files
      // without a class are handled as a silent no-op in the CLI, so they do
      // not reach this branch.
      const message = `Rider Layout: ${error instanceof Error ? error.message : String(error)}`;
      void vscode.window.showWarningMessage(message);
    } finally {
      inFlight = false;
    }
  }

  const onFocus = vscode.window.onDidChangeActiveTextEditor(applyIfNeeded);
  void applyIfNeeded(vscode.window.activeTextEditor);

  return onFocus;
}