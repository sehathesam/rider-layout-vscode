import * as vscode from 'vscode';
import { LayoutEngineService } from '../services/layoutEngineService';

export function registerRearrangeSelection(engine: LayoutEngineService): vscode.Disposable {
  return vscode.commands.registerCommand('riderLayout.rearrangeSelection', async () => {
    const editor = vscode.window.activeTextEditor;
    if (!editor || editor.document.languageId !== 'csharp' || editor.selection.isEmpty) return;
    void vscode.window.showInformationMessage('Selection rearrangement will be enabled after multi-type/class selection support is added.');
  });
}
