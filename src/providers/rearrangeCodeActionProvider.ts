import * as vscode from 'vscode';

export class RearrangeCodeActionProvider implements vscode.CodeActionProvider {
  static readonly providedCodeActionKinds = [vscode.CodeActionKind.RefactorRewrite];

  provideCodeActions(_document: vscode.TextDocument, _range: vscode.Range): vscode.CodeAction[] {
    const action = new vscode.CodeAction('Apply Rider File Layout', vscode.CodeActionKind.RefactorRewrite);
    action.command = { command: 'riderLayout.rearrangeDocument', title: 'Apply Rider File Layout' };
    return [action];
  }
}
