import assert from 'node:assert/strict';
import test from 'node:test';
import { chooseVersion, readTags, readVersions } from './resolve-preview-version.mjs';

test('first publish uses the configured preview; later publishes increment numerically', () => {
  assert.equal(chooseVersion('0.1.0-preview.1', []), '0.1.0-preview.1');
  assert.equal(chooseVersion('0.1.0-preview.1', ['0.1.0-preview.1']), '0.1.0-preview.2');
  assert.equal(chooseVersion('0.1.0-preview.1', ['0.1.0-preview.9', '0.1.0-preview.10']), '0.1.0-preview.11');
});

test('configured preview is a floor and other release lines do not affect it', () => {
  assert.equal(chooseVersion('0.1.0-preview.4', [
    '0.1.0-preview.1', '0.2.0-preview.99', '0.1.0', '0.1.0-rc.9',
  ]), '0.1.0-preview.4');
});

test('the next preview clears the highest number from any source, not just the feed', () => {
  // The case this rule exists for: one source is ahead of the other. Whichever
  // source reports preview.3, the next publish must be preview.4 - never the
  // preview.3 that only the lagging source still believes is free.
  const feed = ['0.1.0-preview.1', '0.1.0-preview.2'];
  const tags = ['0.1.0-preview.3'];
  assert.equal(chooseVersion('0.1.0-preview.1', [...feed, ...tags]), '0.1.0-preview.4');
  assert.equal(chooseVersion('0.1.0-preview.1', [...tags, ...feed]), '0.1.0-preview.4');
  assert.equal(chooseVersion('0.1.0-preview.1', feed), '0.1.0-preview.3');
  // Gaps are never filled in: a burned high number wins over a dense low run.
  assert.equal(chooseVersion('0.1.0-preview.1', ['0.1.0-preview.7', '0.1.0-preview.1']), '0.1.0-preview.8');
  // Duplicates across sources are the normal case once both agree.
  assert.equal(chooseVersion('0.1.0-preview.1', ['0.1.0-preview.2', '0.1.0-preview.2']), '0.1.0-preview.3');
});

test('only unused previews on the configured release line are accepted', () => {
  const published = ['0.1.0-preview.1'];
  assert.equal(chooseVersion('0.1.0-preview.1', published, { suffix: 'preview.3' }), '0.1.0-preview.3');
  assert.equal(chooseVersion('0.1.0-preview.1', published, { tag: 'v0.1.0-preview.2' }), '0.1.0-preview.2');
  for (const suffix of ['preview.1', 'rc.2', 'preview.0', 'preview.02', 'preview.2;evil']) {
    assert.throws(() => chooseVersion('0.1.0-preview.1', published, { suffix }));
  }
  for (const tag of ['v0.1.0', 'v0.1.0-rc.2', 'v0.2.0-preview.2', 'v0.1.0-preview.1']) {
    assert.throws(() => chooseVersion('0.1.0-preview.1', published, { tag }));
  }
  assert.throws(() => chooseVersion('0.1.0', published));
});

test('a request is rejected when only the other source has burned the number', () => {
  // preview.3 looks free to the feed alone; the tag says it is not.
  const used = ['0.1.0-preview.2', '0.1.0-preview.3'];
  assert.throws(() => chooseVersion('0.1.0-preview.1', used, { suffix: 'preview.3' }),
    /at least '0\.1\.0-preview\.4'/);
  assert.throws(() => chooseVersion('0.1.0-preview.1', used, { tag: 'v0.1.0-preview.3' }));
  assert.equal(chooseVersion('0.1.0-preview.1', used, { tag: 'v0.1.0-preview.9' }), '0.1.0-preview.9');
});

test('release tags are read as versions, skipping the tag that triggered the run', () => {
  const git = () => 'v0.1.0-preview.1\nv0.1.0-preview.3\n\nv0.2.0-preview.1\n';
  assert.deepEqual(readTags('', git), ['0.1.0-preview.1', '0.1.0-preview.3', '0.2.0-preview.1']);
  assert.deepEqual(readTags('0.1.0-preview.3', git), ['0.1.0-preview.1', '0.2.0-preview.1']);
  assert.deepEqual(readTags('', () => ''), []);
  // The tag push that starts a publish must not rule its own version out.
  assert.equal(chooseVersion('0.1.0-preview.1', readTags('0.1.0-preview.3', git),
    { tag: 'v0.1.0-preview.3' }), '0.1.0-preview.3');
});

function fakeFeed(responses) {
  return async (url, options) => {
    assert.ok(options.signal);
    if (url === 'https://feed/index.json') return Response.json({
      resources: [{ '@type': 'PackageBaseAddress/3.0.0', '@id': 'https://feed/flat/' }],
    });
    assert.ok(Object.hasOwn(responses, url), `Unexpected request ${url}`);
    const response = responses[url];
    return typeof response === 'number' ? new Response(null, { status: response }) : Response.json(response);
  };
}

test('all packages contribute, including a partially published newer preview', async () => {
  const versions = await readVersions('https://feed/index.json', ['Core', 'Provider', 'New'], {}, fakeFeed({
    'https://feed/flat/core/index.json': { versions: ['0.1.0-preview.1'] },
    'https://feed/flat/provider/index.json': { versions: ['0.1.0-preview.1', '0.1.0-preview.2'] },
    'https://feed/flat/new/index.json': 404,
  }));
  assert.equal(chooseVersion('0.1.0-preview.1', versions), '0.1.0-preview.3');
});

test('feed failures and malformed responses stop publication', async () => {
  for (const response of [401, 403, 429, 500, {}, { versions: [2] }]) {
    await assert.rejects(readVersions('https://feed/index.json', ['Core'], {}, fakeFeed({
      'https://feed/flat/core/index.json': response,
    })));
  }
  await assert.rejects(readVersions('https://feed/index.json', ['Core'], {}, async () => {
    throw new Error('Network unavailable');
  }));
  await assert.rejects(readVersions('https://feed/index.json', ['Core'], {}, async () => Response.json({})));
});
