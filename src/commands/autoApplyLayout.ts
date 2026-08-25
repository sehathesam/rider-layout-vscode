import * as vscode from 'vscode';
import { LayoutEngineService } from '../services/layoutEngineService';

export function registerAutoApplyLayout(engine: LayoutEngineService): vscode.Disposable {
  const applied = new WeakSet<vscode.TextDocument>();
  let inFlight = false;

  async function applyIfNeeded(editor?: vscode.TextEditor): Promise<void> {
    if (!editor || editor.document.languageId !== 'csharp') return;
    if (inFlight) return;

    const config = vscode.workspace.getConfiguration('riderLayout');
    if (!config.get<boolean>('enabled', true)) return;
    if (!config.get<boolean>('autoApplyOnFocus', true)) return;
    if (applied.has(editor.document)) return;

    try {
      inFlight = true;
      const output = await engine.rearrange(editor.document);
      if (output !== editor.document.getText()) {
        const fullRange = new vscode.Range(
          editor.document.positionAt(0),
          editor.document.positionAt(editor.document.getText().length)
        );
        await editor.edit(e => e.replace(fullRange, output), {
          undoStopBefore: false,
          undoStopAfter: false
        });
      }
      applied.add(editor.document);
    } catch {
      // Layout not configured or engine error: stay quiet and retry on the next
      // focus so it applies once a layout file has been selected.
    } finally {
      inFlight = false;
    }
  }

  const onFocus = vscode.window.onDidChangeActiveTextEditor(applyIfNeeded);
  // Apply to the currently active editor when the extension activates.
  void applyIfNeeded(vscode.window.activeTextEditor);

  return onFocus;
}