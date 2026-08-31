import * as vscode from 'vscode';

export function isInIgnoredFolder(fsPath: string, ignoreFolders: string[]): boolean {
  const normalizedPath = fsPath.replace(/\\/g, '/').toLowerCase();
  const parents = normalizedPath.split('/').slice(0, -1);
  return ignoreFolders.some(folder => {
    const normalized = folder.trim().replace(/\\/g, '/').replace(/\/+$/, '').toLowerCase();
    if (!normalized) return false;
    if (normalized.includes('/')) {
      const withSlash = normalized.endsWith('/') ? normalized : normalized + '/';
      return normalizedPath.includes(withSlash) || normalizedPath.includes('/' + withSlash);
    }
    return parents.includes(normalized);
  });
}

export function isIgnoredDocument(document: vscode.TextDocument): boolean {
  const config = vscode.workspace.getConfiguration('riderLayout');
  const ignoreFolders = config.get<string[]>('ignoreFolders', []);
  return isInIgnoredFolder(document.uri.fsPath, ignoreFolders);
}