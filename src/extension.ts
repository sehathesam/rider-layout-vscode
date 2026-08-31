import * as vscode from 'vscode';
import * as path from 'node:path';
import { registerPreviewLayout } from './commands/previewLayout';
import { registerRearrangeDocument } from './commands/rearrangeDocument';
import { registerRearrangeAllFiles } from './commands/rearrangeAllFiles';
import { registerAddIgnoreFolder } from './commands/addIgnoreFolder';
import { registerReloadLayout } from './commands/reloadLayout';
import { registerPickLayoutFile } from './commands/pickLayoutFile';
import { registerResetLayout } from './commands/resetLayout';
import { registerToggleEnabled } from './commands/toggleEnabled';
import { registerToggleRegions } from './commands/toggleRegions';
import { registerToggleFormatOnSave } from './commands/toggleFormatOnSave';
import { registerFormatOnSave } from './commands/formatOnSave';
import { registerAutoApplyLayout } from './commands/autoApplyLayout';
import { LayoutEngineService } from './services/layoutEngineService';
import { RiderSettingsService } from './services/riderSettingsService';
import { createOutputChannel } from './utils/logger';
import { syncIgnoreFoldersFromGitignore } from './utils/gitignoreSync';
import { RearrangeCodeActionProvider } from './providers/rearrangeCodeActionProvider';

export function activate(context: vscode.ExtensionContext): void {
  const output = createOutputChannel();
  void syncIgnoreFoldersFromGitignore(output).catch(error => {
    output.appendLine(`gitignore sync failed: ${error instanceof Error ? error.message : String(error)}`);
  });
  const settings = new RiderSettingsService(
    output,
    path.join(context.extensionPath, 'media', 'ideen-layout.xml')
  );
  const engine = new LayoutEngineService(context, output, settings);

  context.subscriptions.push(
    output,
    { dispose: () => { void engine.dispose(); } },
    registerRearrangeDocument(engine),
    registerRearrangeAllFiles(engine),
    registerAddIgnoreFolder(),
    registerReloadLayout(engine),
    registerPickLayoutFile(engine),
    registerResetLayout(engine),
    registerToggleEnabled(),
    registerToggleRegions(),
    registerToggleFormatOnSave(),
    registerAutoApplyLayout(engine),
    registerFormatOnSave(engine),
    registerPreviewLayout(engine),
    vscode.languages.registerCodeActionsProvider(
      { language: 'csharp' },
      new RearrangeCodeActionProvider(),
      { providedCodeActionKinds: RearrangeCodeActionProvider.providedCodeActionKinds }
    )
  );
}

export function deactivate(): void {}
