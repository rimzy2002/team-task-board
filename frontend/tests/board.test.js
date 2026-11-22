import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { resolve } from 'node:path';
import { test } from 'node:test';
import { chromium } from 'playwright';
import { createServer } from 'vite';

test('the board protects its route and handles loading, retry, validation and creation', { timeout: 60000 }, async (t) => {
  const origin = 'http://127.0.0.1:5188';
  assert((await fetch('http://127.0.0.1:9099/')).ok, 'Start the Auth Emulator before running the browser test.');
  process.env.VITE_API_BASE_URL = origin;
  const productionConfig = resolve('src/config/firebase.js');
  const server = await createServer({
    server: { host: '127.0.0.1', port: 5188, strictPort: true },
    plugins: [{
      name: 'local-auth-emulator-fixture',
      enforce: 'pre',
      transform(_, id) {
        if (resolve(id) !== productionConfig) return;
        return `
          import { initializeApp } from 'firebase/app';
          import { connectAuthEmulator, getAuth } from 'firebase/auth';
          export const firebaseApp = initializeApp({ projectId: 'demo-team-task-board', apiKey: 'demo-key' });
          export const auth = getAuth(firebaseApp);
          connectAuthEmulator(auth, 'http://127.0.0.1:9099', { disableWarnings: true });
        `;
      },
    }],
  });
  await server.listen();
  t.after(() => server.close());
  const browser = await chromium.launch();
  t.after(() => browser.close());
  const context = await browser.newContext({ viewport: { width: 375, height: 800 } });
  const page = await context.newPage();
  page.setDefaultTimeout(15000);
  let todoFailed = false;
  let task;
  let writes = 0;
  await page.route(origin + '/api/**', async (route) => {
    const request = route.request();
    const url = new URL(request.url());
    assert(/^Bearer .+/.test(request.headers().authorization ?? ''), 'API request must have a Bearer header.');
    if (url.pathname === '/api/projects') {
      await route.fulfill({ json: [{ id: 1, name: 'Test project' }] });
      return;
    }
    assert.equal(url.pathname, '/api/projects/1/tasks');
    if (request.method() === 'POST') {
      writes++;
      task = { id: 1, projectId: 1, ...request.postDataJSON() };
      await route.fulfill({ status: 201, json: task });
      return;
    }
    const status = url.searchParams.get('status');
    if (status === 'ToDo' && !todoFailed) {
      todoFailed = true;
      await new Promise((resolve) => setTimeout(resolve, 600));
      await route.fulfill({ status: 503 });
      return;
    }
    const items = task?.status === status ? [task] : [];
    await route.fulfill({ json: { items, page: 1, pageSize: 10, totalCount: items.length } });
  });

  await page.goto(origin + '/projects/1/board');
  await page.waitForURL('**/login');
  await page.getByRole('link', { name: 'Create an account' }).click();
  await page.getByLabel('Email').fill(`${randomUUID()}@example.test`);
  await page.getByLabel('Password').fill(randomUUID());
  const [signup] = await Promise.all([
    page.waitForResponse((response) => new URL(response.url()).pathname.endsWith('/accounts:signUp')),
    page.getByRole('button', { name: 'Create account' }).click(),
  ]);
  assert.equal(signup.status(), 200, 'Auth Emulator signup status');
  await page.waitForURL(origin + '/', { waitUntil: 'domcontentloaded' });
  await page.getByRole('link', { name: 'Test project' }).click();
  const todo = page.getByRole('region', { name: 'To Do', exact: true });
  await todo.getByText('Loading To Do tasks…').waitFor();
  await todo.getByRole('alert').waitFor();
  await todo.getByRole('button', { name: 'Retry To Do' }).click();
  await todo.getByText('No tasks yet.').waitFor();

  await page.getByRole('button', { name: 'Create task' }).click();
  const form = page.getByRole('dialog');
  const save = form.getByRole('button', { name: 'Save task' });
  await save.click();
  await form.getByText('Enter a title of 1 to 200 characters.').waitFor();
  await form.getByLabel('Title', { exact: true }).fill('Fixture agenda');
  await form.getByLabel('Due date (optional, UTC)').fill('2000-01-01');
  await save.click();
  await form.getByText(/A new due date must be today/).waitFor();
  assert.equal(writes, 0);
  await form.getByLabel('Due date (optional, UTC)').fill('');
  await save.click();
  await todo.getByRole('heading', { name: 'Fixture agenda' }).waitFor();
  assert.equal(writes, 1);
  assert(await page.evaluate(() => document.documentElement.scrollWidth <= 375));
  await page.getByRole('button', { name: 'Log out' }).click();
  await page.waitForURL('**/login');
});
