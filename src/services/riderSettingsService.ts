import * as fs from 'node:fs/promises';
import * as path from 'node:path';
import * as vscode from 'vscode';

const PATTERNS_START = '<Patterns';
const PATTERNS_END = '</Patterns>';

export class RiderSettingsService {
  constructor(private readonly output: vscode.OutputChannel) {}

  async loadLayoutFromFile(filePath: string): Promise<string | undefined> {
    try {
      const text = await fs.readFile(filePath, 'utf8');
      const xml = this.extractPatterns(text);
      if (xml) {
        this.output.appendLine(`Loaded Rider layout: ${filePath}`);
        return xml;
      }
      this.output.appendLine(`No <Patterns> block found in ${filePath}`);
      return undefined;
    } catch (error) {
      this.output.appendLine(`Cannot read Rider layout ${filePath}: ${String(error)}`);
      return undefined;
    }
  }

  async findLayoutXml(folder: vscode.WorkspaceFolder): Promise<string | undefined> {
    const configured = vscode.workspace.getConfiguration('riderLayout').get<string | null>('settingsPath');
    const candidates = configured
      ? [configured]
      : await this.findCandidateFiles(folder.uri.fsPath);

    for (const file of candidates) {
      try {
        const text = await fs.readFile(file, 'utf8');
        const xml = this.extractPatterns(text);
        if (xml) {
          this.output.appendLine(`Using Rider layout: ${file}`);
          return xml;
        }
      } catch (error) {
        this.output.appendLine(`Cannot read Rider settings candidate ${file}: ${String(error)}`);
      }
    }
    return undefined;
  }

  private async findCandidateFiles(root: string): Promise<string[]> {
    const result: string[] = [];
    const maxDepth = 5;

    async function walk(dir: string, depth: number): Promise<void> {
      if (depth > maxDepth) return;
      let entries: import('node:fs').Dirent[];
      try { entries = await fs.readdir(dir, { withFileTypes: true }); } catch { return; }

      for (const entry of entries) {
        if (entry.name === 'node_modules' || entry.name === 'Library' || entry.name === 'Temp' || entry.name === 'obj' || entry.name === 'bin') continue;
        const full = path.join(dir, entry.name);
        if (entry.isDirectory()) {
          if (entry.name === '.idea' || entry.name.endsWith('.idea')) await walk(full, depth + 1);
          else if (depth < 2) await walk(full, depth + 1);
        } else if (entry.name.endsWith('.DotSettings') || entry.name.endsWith('.xml')) {
          if (entry.name.toLowerCase().includes('settings') || entry.name.toLowerCase().includes('layout') || full.includes(`${path.sep}.idea${path.sep}`)) {
            result.push(full);
          }
        }
      }
    }

    await walk(root, 0);
    return result;
  }

  private extractPatterns(text: string): string | undefined {
    const decoded = text.replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&quot;/g, '"').replace(/&amp;/g, '&');
    const start = decoded.indexOf(PATTERNS_START);
    if (start < 0) return undefined;
    const end = decoded.indexOf(PATTERNS_END, start);
    if (end < 0) return undefined;
    return decoded.slice(start, end + PATTERNS_END.length);
  }
}
