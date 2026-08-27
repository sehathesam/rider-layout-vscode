import * as vscode from 'vscode';
import { LayoutEngineService } from '../services/layoutEngineService';

export function registerRearrangeAllFiles(engine: LayoutEngineService): vscode.Disposable {
  return vscode.commands.registerCommand('riderLayout.rearrangeAllFiles', async () => {
    const folders = vscode.workspace.workspaceFolders;
    if (!folders || folders.length === 0) {
      void vscode.window.showWarningMessage('Rider Layout: open a workspace folder first.');
      return;
    }

    const config = vscode.workspace.getConfiguration('riderLayout');
    if (!config.get<boolean>('enabled', true)) {
      void vscode.window.showWarningMessage('Rider Layout: the extension is disabled.');
      return;
    }

    const choice = await vscode.window.showWarningMessage(
      'This will rearrange and save every C# file in the workspace. Continue?',
      { modal: true },
      'Rearrange All C# Files'
    );
    if (choice !== 'Rearrange All C# Files') return;

    try {
      const files = await vscode.workspace.findFiles('**/*.cs', '**/node_modules/**');
      const output = vscode.window.createOutputChannel('Rider Layout');
      let changed = 0;
      let failed = 0;

      await vscode.window.withProgress(
        {
          location: vscode.ProgressLocation.Notification,
          title: 'Rider Layout: rearranging C# files…',
          cancellable: true
        },
        async (progress, token) => {
          for (let i = 0; i < files.length; i++) {
            if (token.isCancellationRequested) {
              output.appendLine(`Cancelled after ${changed} file(s) changed.`);
              return;
            }
            progress.report({ message: `${i + 1}/${files.length}`, increment: 100 / files.length });

            const document = await vscode.workspace.openTextDocument(files[i]);
            if (document.languageId !== 'csharp') continue;

            let rearranged: string;
            try {
              rearranged = await engine.rearrange(document);
            } catch (error) {
              failed++;
              output.appendLine(`Failed: ${files[i].fsPath} — ${error instanceof Error ? error.message : String(error)}`);
              continue;
            }

            const original = document.getText();
            if (rearranged === original) continue;

            const fullRange = new vscode.Range(
              document.positionAt(0),
              document.positionAt(document.getText().length)
            );
            const edit = new vscode.WorkspaceEdit();
            edit.replace(document.uri, fullRange, rearranged);
            await vscode.workspace.applyEdit(edit);
            const open = vscode.workspace.textDocuments.find(d => d.uri.toString() === document.uri.toString());
            const filePath = (open ?? document).uri.fsPath;
            if (open) {
              await open.save();
            } else {
              await vscode.workspace.fs.writeFile(document.uri, Buffer.from(rearranged, 'utf8'));
            }
            changed++;
            output.appendLine(`Updated: ${filePath}`);
          }
        }
      );

      const summary = `Rider Layout: ${changed} file(s) rearranged, ${failed} failed out of ${files.length}.`;
      output.appendLine(summary);
      if (failed > 0) {
        void vscode.window.showWarningMessage(summary);
      } else if (changed > 0) {
        void vscode.window.showInformationMessage(summary);
      } else {
        void vscode.window.showInformationMessage('Rider Layout: no files needed changes.');
      }
    } catch (error) {
      void vscode.window.showErrorMessage(`Rider Layout: ${error instanceof Error ? error.message : String(error)}`);
    }
  });
}