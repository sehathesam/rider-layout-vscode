import * as vscode from 'vscode';
import { registerPreviewLayout } from './commands/previewLayout';
import { registerRearrangeDocument } from './commands/rearrangeDocument';
import { registerRearrangeSelection } from './commands/rearrangeSelection';
import { registerReloadLayout } from './commands/reloadLayout';
import { LayoutEngineService } from './services/layoutEngineService';
import { RiderSettingsService } from './services/riderSettingsService';
import { createOutputChannel } from './utils/logger';
import { RearrangeCodeActionProvider } from './providers/rearrangeCodeActionProvider';

export function activate(context: vscode.ExtensionContext): void {
  const output = createOutputChannel();
  const settings = new RiderSettingsService(output);
  const engine = new LayoutEngineService(context, output, settings);

  context.subscriptions.push(
    output,
    { dispose: () => { void engine.dispose(); } },
    registerRearrangeDocument(engine),
    registerRearrangeSelection(engine),
    registerReloadLayout(engine),
    registerPreviewLayout(engine),
    vscode.languages.registerCodeActionsProvider(
      { language: 'csharp' },
      new RearrangeCodeActionProvider(),
      { providedCodeActionKinds: RearrangeCodeActionProvider.providedCodeActionKinds }
    )
  );
}

export function deactivate(): void {}
