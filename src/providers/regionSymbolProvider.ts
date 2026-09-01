import * as vscode from 'vscode';

export class RegionSymbolProvider implements vscode.DocumentSymbolProvider {
  private readonly regionRe = /^\s*#region\s+(.+?)\s*$/;
  private readonly endRegionRe = /^\s*#endregion\s*$/;

  provideDocumentSymbols(document: vscode.TextDocument): vscode.DocumentSymbol[] {
    const roots: vscode.DocumentSymbol[] = [];
    const stack: vscode.DocumentSymbol[] = [];

    for (let i = 0; i < document.lineCount; i++) {
      const line = document.lineAt(i).text;
      const match = this.regionRe.exec(line);
      if (match) {
        const name = match[1].trim();
        const nameIndex = line.indexOf(name);
        const symbol = new vscode.DocumentSymbol(
          name,
          '',
          vscode.SymbolKind.Namespace,
          new vscode.Range(i, 0, i, line.length),
          new vscode.Range(i, nameIndex, i, nameIndex + name.length)
        );
        if (stack.length > 0) {
          stack[stack.length - 1].children.push(symbol);
        } else {
          roots.push(symbol);
        }
        stack.push(symbol);
        continue;
      }
      if (this.endRegionRe.test(line) && stack.length > 0) {
        const open = stack.pop()!;
        open.range = new vscode.Range(open.range.start, new vscode.Position(i, line.length));
      }
    }

    return roots;
  }
}

export function registerRegionSymbolProvider(): vscode.Disposable {
  return vscode.languages.registerDocumentSymbolProvider(
    { language: 'csharp', scheme: 'file' },
    new RegionSymbolProvider()
  );
}