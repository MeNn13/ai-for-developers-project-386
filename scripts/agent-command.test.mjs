import assert from 'node:assert/strict'
import test from 'node:test'
import { isAllowed } from './agent-command.mjs'

test('allows routine Git and GitHub operations', () => {
  for (const command of [
    ['git', 'add', 'backend'],
    ['git', 'commit', '-m', 'fix: preserve behavior'],
    ['git', 'switch', '-c', 'topic'],
    ['gh', 'issue', 'view', '39'],
    ['gh', 'issue', 'create', '--title', 'A title; no shell expansion'],
    ['gh', 'pr', 'create', '--body-file', 'artifacts/pr.md'],
  ])
    assert.equal(isAllowed(command), true, JSON.stringify(command))
})

test('requires approval for publication, destructive operations and arbitrary programs', () => {
  for (const command of [
    ['git', 'push', 'origin', 'main'],
    ['git', 'push', '--force', 'origin', 'topic'],
    ['git', 'reset', '--hard'],
    ['git', 'clean', '-fd'],
    ['git', 'checkout', '--', '.'],
    ['git', 'switch', '-C', 'topic'],
    ['git', 'switch', '-c', 'topic', '--discard-changes'],
    ['git', '-c', 'alias.erase=!rm -rf .', 'erase'],
    ['gh', 'pr', 'merge', '--admin'],
    ['gh', 'api', '--method', 'DELETE', 'repos/owner/repo'],
    ['powershell', '-Command', 'anything'],
    [],
    null,
    ['git', 1],
  ])
    assert.equal(isAllowed(command), false, JSON.stringify(command))
})
