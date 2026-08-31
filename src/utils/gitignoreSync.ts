import * as vscode from 'vscode';

export function parseGitignoreFolders(content: string): string[] {
  const result: string[] = [];
  for (const raw of content.split(/\r?\n/)) {
    let entry = raw.trimEnd();
    if (!entry) continue;
    if (entry.startsWith('#')) continue;
    if (entry.startsWith('!')) continue;
    entry = entry.replace(/[ \t]+#[\s\S]*$/, '').trimEnd();
    if (!entry) continue;

    let isDirectory = false;
    if (entry.endsWith('/')) {
      isDirectory = true;
      entry = entry.slice(0, -1);
    }
    if (entry.startsWith('/')) entry = entry.slice(1);
    if (!entry) continue;

    if (/[*?[\]{}]/.test(entry)) continue;
    if (entry.includes('\\')) continue;

    if (!isDirectory) {
      const last = entry.split('/').pop() ?? '';
      if (last.slice(1).includes('.')) continue;
    }

    result.push(entry);
  }
  return result;
}

export async function syncIgnoreFoldersFromGitignore(
  output: vscode.OutputChannel
): Promise<string[]> {
  const folders = vscode.workspace.workspaceFolders;
  if (!folders || folders.length === 0) return [];

  const candidates = new Set<string>();
  for (const folder of folders) {
    try {
      const content = (await vscode.workspace.fs.readFile(vscode.Uri.joinPath(folder.uri, '.gitignore'))).toString();
      for (const entry of parseGitignoreFolders(content)) candidates.add(entry);
    } catch {
      // No .gitignore in this workspace root.
    }
  }

  const config = vscode.workspace.getConfiguration('riderLayout');
  const current = config.get<string[]>('ignoreFolders', []);
  const defaults = (config.inspect<string[]>('ignoreFolders')?.defaultValue ?? []) as string[];
  const merged = Array.from(new Set([...defaults, ...current].map(value => value.trim()).filter(Boolean)));
  const known = new Set(merged.map(value => value.toLowerCase()));

  const added: string[] = [];
  for (const name of candidates) {
    if (known.has(name.toLowerCase())) continue;
    merged.push(name);
    added.push(name);
  }

  if (added.length > 0) {
    await config.update('ignoreFolders', merged, vscode.ConfigurationTarget.Workspace);
    output.appendLine(`gitignore sync: added ${added.join(', ')} to ignoreFolders.`);
  }

  return added;
}