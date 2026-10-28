#!/usr/bin/env node
// Builds the image, runs it and checks that the container answers. Hexlet
// requires the project to build into an image and run in a container, so this
// is the check that says it does. It needs Docker and runs in CI as
// `mise run docker:check`.

import { spawnSync } from 'node:child_process'

const image = 'booking-calendar:ci'
const container = 'booking-calendar-ci'
const port = process.env.IMAGE_CHECK_PORT ?? '18080'
const base = `http://127.0.0.1:${port}`
const timeoutMs = 60_000

function step(command, args) {
  const result = spawnSync(command, args, { stdio: 'inherit' })
  if (result.error) throw result.error
  if (result.status !== 0) {
    throw new Error(`${command} ${args.join(' ')} exited with ${result.status}`)
  }
}

function removeContainer() {
  spawnSync('docker', ['rm', '--force', container], { stdio: 'ignore' })
}

async function waitFor(url) {
  // The deadline starts when the wait starts: the build before it can take
  // minutes, and a deadline from module load would already be in the past.
  const deadline = Date.now() + timeoutMs
  let lastError = null
  while (Date.now() < deadline) {
    try {
      const response = await fetch(url)
      if (response.ok) return response
      lastError = new Error(`${url} answered ${response.status}`)
    } catch (error) {
      lastError = error
    }
    await new Promise((resolve) => setTimeout(resolve, 1000))
  }
  throw lastError ?? new Error(`${url} did not answer in time`)
}

async function main() {
  console.log(`building ${image}`)
  step('docker', ['build', '--tag', image, '.'])

  removeContainer()
  console.log(`running ${container} on port ${port}`)
  step('docker', ['run', '--detach', '--name', container, '--publish', `${port}:8080`, image])

  const health = await waitFor(`${base}/api/health`)
  const payload = await health.json()
  if (payload.status !== 'ok') {
    throw new Error(`unexpected health payload: ${JSON.stringify(payload)}`)
  }
  console.log('health endpoint answered ok')

  const page = await waitFor(`${base}/`)
  const html = await page.text()
  if (!html.includes('id="root"')) {
    throw new Error('the index page of the frontend is not served')
  }
  console.log('frontend is served from the same container')
}

try {
  await main()
} catch (error) {
  console.error(`\nimage check failed: ${error.message}\n`)
  spawnSync('docker', ['logs', '--tail', '50', container], { stdio: 'inherit' })
  process.exitCode = 1
} finally {
  removeContainer()
}
