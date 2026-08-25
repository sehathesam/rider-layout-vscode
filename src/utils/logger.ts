import * as vscode from 'vscode';

export function createOutputChannel(): vscode.OutputChannel {
  return vscode.window.createOutputChannel('Rider Layout');
}
