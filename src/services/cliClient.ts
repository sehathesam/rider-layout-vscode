import * as cp from 'node:child_process';
import * as path from 'node:path';
import * as readline from 'node:readline';

export interface RearrangeRequest {
  command: 'rearrange' | 'parse';
  source?: string;
  layoutXml?: string;
  projectRoot?: string;
}

export interface RearrangeResponse {
  id: number;
  success: boolean;
  source?: string;
  error?: string;
  diagnostics?: string[];
}

interface PendingRequest {
  resolve: (response: RearrangeResponse) => void;
  reject: (error: Error) => void;
}

/**
 * Long-lived JSON-lines client for the Rider Layout CLI.
 *
 * The CLI process is spawned once and kept alive across requests so Roslyn's
 * warm-up cost (~300-500ms) is paid a single time. Requests are correlated by
 * an incrementing id and matched against the CLI's out-of-order responses.
 */
export class CliClient {
  private child?: cp.ChildProcess;
  private lineReader?: readline.Interface;
  private nextId = 1;
  private readonly pending = new Map<number, PendingRequest>();
  private disposed = false;

  constructor(private readonly cliPath: string) {}

  async request(request: RearrangeRequest): Promise<RearrangeResponse> {
    if (this.disposed) throw new Error('Rider Layout CLI client is disposed.');
    this.ensureSpawned();

    const id = this.nextId++;
    const payload = JSON.stringify({ id, ...request });

    return new Promise((resolve, reject) => {
      this.pending.set(id, { resolve, reject });
      this.child!.stdin!.write(payload + '\n');
    });
  }

  dispose(): void {
    if (this.disposed) return;
    this.disposed = true;

    for (const pending of this.pending.values()) {
      pending.reject(new Error('Rider Layout CLI client was disposed.'));
    }
    this.pending.clear();

    this.lineReader?.close();
    this.lineReader = undefined;
    this.child?.kill();
    this.child = undefined;
  }

  private ensureSpawned(): void {
    if (this.child && !this.child.killed) return;

    const child = cp.spawn('dotnet', [this.cliPath], {
      stdio: ['pipe', 'pipe', 'pipe'],
      windowsHide: true
    });
    this.child = child;

    let stderr = '';
    child.stderr.on('data', chunk => {
      stderr += chunk.toString();
      if (stderr.length > 4096) stderr = stderr.slice(-4096);
    });

    const lineReader = readline.createInterface({ input: child.stdout });
    this.lineReader = lineReader;

    lineReader.on('line', line => {
      let response: RearrangeResponse;
      try {
        response = JSON.parse(line) as RearrangeResponse;
      } catch (error) {
        this.rejectAll(`Invalid CLI response: ${String(error)}`);
        return;
      }
      this.settle(response);
    });

    child.once('error', error => {
      this.rejectAll(`Failed to start Rider Layout CLI: ${error.message}`);
    });

    child.once('exit', code => {
      lineReader.close();
      if (this.child === child) {
        this.child = undefined;
        this.lineReader = undefined;
      }
      if (!this.disposed && this.pending.size > 0) {
        this.rejectAll(`Rider Layout CLI exited unexpectedly (exit code ${code}).`);
      }
    });
  }

  private settle(response: RearrangeResponse): void {
    const pending = this.pending.get(response.id ?? NaN);
    if (!pending) return;
    this.pending.delete(response.id!);
    pending.resolve(response);
  }

  private rejectAll(message: string): void {
    for (const pending of this.pending.values()) pending.reject(new Error(message));
    this.pending.clear();
  }
}

export function defaultCliPath(extensionPath: string): string {
  return path.join(extensionPath, 'runtime', 'RiderLayout.Cli.dll');
}