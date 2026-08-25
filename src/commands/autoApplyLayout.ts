import * as vscode from 'vscode';
import { LayoutEngineService } from '../services/layoutEngineService';

export function registerAutoApplyLayout(engine: LayoutEngineService): vscode.Disposable {
  const applied = new WeakSet<vscode.TextDocument>();
  const warned = new WeakSet<vscode.TextDocument>();
  let inFlight = false;
  let lastErrorShownAt = 0;

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
    } catch (error) {
      // Only nag once per document so we do not bother the user on every focus
      // switch, but still make the failure visible instead of silently failing.
      if (!warned.has(editor.document)) {
        warned.add(editor.document);
        const now = Date.now();
        if (now - lastErrorShownAt > 30_000) {
          lastErrorShownAt = now;
          const message = `Rider Layout: ${error instanceof Error ? error.message : String(error)}`;
          void vscode.window.showWarningMessage(message, 'Select Layout File').then(action => {
            if (action === 'Select Layout File') {
              void vscode.commands.executeCommand('riderLayout.pickLayoutFile');
            }
          });
        }
      }
    } finally {
      inFlight = false;
    }
  }

  const onFocus = vscode.window.onDidChangeActiveTextEditor(applyIfNeeded);
  void applyIfNeeded(vscode.window.activeTextEditor);

  return onFocus;
}