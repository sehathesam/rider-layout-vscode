import * as vscode from 'vscode';

export function isWorkspaceDocument(document: vscode.TextDocument): boolean {
  if (document.isUntitled) return false;
  if (document.uri.scheme !== 'file') return false;
  return vscode.workspace.getWorkspaceFolder(document.uri) !== undefined;
}

export function isShownInDiffEditor(document: vscode.TextDocument): boolean {
  const uri = document.uri.toString();
  for (const group of vscode.window.tabGroups.all) {
    for (const tab of group.tabs) {
      const input = tab.input;
      if (input instanceof vscode.TabInputTextDiff) {
        if (input.original.toString() === uri || input.modified.toString() === uri) return true;
      }
    }
  }
  return false;
}

export function differsStructurally(original: string, rearranged: string): boolean {
  return stripWhitespace(original) !== stripWhitespace(rearranged);
}

function stripWhitespace(text: string): string {
  return text.replace(/\s+/g, '');
}