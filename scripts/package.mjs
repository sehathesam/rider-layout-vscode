import { spawnSync } from 'node:child_process';

const result = spawnSync('npx', ['@vscode/vsce', 'package'], {
  stdio: 'inherit',
  shell: true,
  env: { ...process.env, NODE_OPTIONS: '--no-deprecation' }
});
process.exit(result.status ?? 1);