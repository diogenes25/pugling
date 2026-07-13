#!/usr/bin/env node

import { mkdir, readdir, readFile, rm, writeFile } from 'node:fs/promises';
import path from 'node:path';

const defaultInput = 'http://localhost:5200/openapi/v1.json';
const defaultOutput = 'tools/bruno/Pugling.Api';
const generatedMarker = '.pugling-generated';

const args = parseArgs(process.argv.slice(2));
const input = args.input ?? args.i ?? defaultInput;
const output = args.output ?? args.o ?? defaultOutput;
const force = args.force === true;

const knownVariables = new Map([
  ['fatherId', '1'],
  ['childId', '1'],
  ['fatherPin', '0000'],
  ['childPin', '1111'],
  ['subjectId', '1'],
  ['chapterId', '1'],
  ['categoryId', '1'],
  ['exerciseId', '1'],
  ['planId', '1'],
  ['positionId', '1'],
  ['sessionId', '1'],
  ['attemptId', '1'],
  ['tagId', '1'],
  ['classTestId', '1'],
  ['missionId', '1'],
  ['achievementId', '1'],
  ['rewardId', '1'],
  ['redemptionId', '1'],
  ['articleId', '1'],
  ['listingId', '1'],
  ['purchaseId', '1'],
  ['activationId', '1'],
  ['vocabularyId', '1'],
  ['wordId', '1'],
  ['skinId', 'classic'],
  ['date', new Date().toISOString().slice(0, 10)],
]);
const discoveredVariables = new Set(knownVariables.keys());

const resourceIdBySegment = new Map([
  ['fathers', 'fatherId'],
  ['children', 'childId'],
  ['subjects', 'subjectId'],
  ['chapters', 'chapterId'],
  ['categories', 'categoryId'],
  ['exercises', 'exerciseId'],
  ['study-plans', 'planId'],
  ['positions', 'positionId'],
  ['practice-sessions', 'sessionId'],
  ['tests', 'attemptId'],
  ['class-tests', 'classTestId'],
  ['tags', 'tagId'],
  ['missions', 'missionId'],
  ['achievements', 'achievementId'],
  ['rewards', 'rewardId'],
  ['redemptions', 'redemptionId'],
  ['articles', 'articleId'],
  ['listings', 'listingId'],
  ['purchases', 'purchaseId'],
  ['activations', 'activationId'],
  ['vocabulary', 'vocabularyId'],
  ['words', 'wordId'],
  ['skins', 'skinId'],
]);

// Kuratierte Sammelordner: Der Backbone der Verschachtelung ist die URL-Route (siehe pathOf), aber an
// wenigen Stellen wollen wir einen synthetischen Zwischenordner einziehen, der aus Route/Tag allein nicht
// ableitbar ist. `tier` grenzt die Regel auf eine Ebene ein, `parentKey` verankert den Gruppenordner am
// Routen-Key eines Eltern-Tags (leer = direkt unter dem Tier), `match` entscheidet die Mitgliedschaft.
// Reihenfolge = Priorität (erste passende Regel gewinnt).
const curatedGroups = [
  // Übungs-Verwaltung (Katalog/Kategorien/Vorschau/Typen) direkt unter dem Creator-Tier bündeln – die
  // vier Tags tragen alle „Exercise" im Namen, liegen aber auf unterschiedlichen Routen.
  {
    tier: 'creator',
    name: 'Exercises',
    parentKey: [],
    match: tag => tag.name.includes('Exercise'),
  },
  // Alle typisierten Übungen (arithmetic, cloze, grammar, …) unter Kapitel in einen „Exercises"-Ordner
  // sammeln. Erkannt an der Route …/subjects/{}/chapters/{typ}, nicht am (uneinheitlichen) Tag-Namen.
  {
    tier: 'creator',
    name: 'Exercises',
    parentKey: ['creator', 'subjects', 'chapters'],
    match: tag => tag.operations.some(operation => {
      const key = routeKey(operation.path);
      return key.length >= 4 && key[1] === 'subjects' && key[2] === 'chapters';
    }),
  },
];

const reasonPhrases = new Map([
  [200, 'OK'], [201, 'Created'], [202, 'Accepted'], [204, 'No Content'],
  [400, 'Bad Request'], [401, 'Unauthorized'], [403, 'Forbidden'], [404, 'Not Found'],
  [405, 'Method Not Allowed'], [409, 'Conflict'], [415, 'Unsupported Media Type'],
  [422, 'Unprocessable Entity'], [429, 'Too Many Requests'], [500, 'Internal Server Error'],
]);

function parseArgs(values) {
  const result = {};

  for (let index = 0; index < values.length; index += 1) {
    const value = values[index];

    if (!value.startsWith('--')) {
      if (!result.input) result.input = value;
      continue;
    }

    const [rawName, inlineValue] = value.slice(2).split('=', 2);
    const name = rawName.length === 1 ? rawName : rawName.replaceAll('-', '');

    if (inlineValue !== undefined) {
      result[name] = inlineValue;
      continue;
    }

    if (name === 'force') {
      result[name] = true;
      continue;
    }

    result[name] = values[index + 1];
    index += 1;
  }

  return result;
}

async function loadOpenApi(source) {
  if (/^https?:\/\//i.test(source)) {
    const response = await fetch(source);
    if (!response.ok) throw new Error(`OpenAPI konnte nicht geladen werden: ${response.status} ${response.statusText}`);
    return response.json();
  }

  return JSON.parse(await readFile(source, 'utf8'));
}

// Kein destruktives rm mehr (das brach unter Windows bei gesperrten Dateien mitten im Löschen ab und
// hinterließ eine halb-leere Collection). Wir stellen nur sicher, dass Ordner + Marker existieren und
// melden zurück, ob die Collection von uns verwaltet wird (Marker vorhanden oder --force) – dann dürfen
// beim Sync verwaiste Altdateien entfernt werden.
async function prepareOutput(directory, forceOutput) {
  const markerPath = path.join(directory, generatedMarker);

  let managed = forceOutput;
  try {
    await readFile(markerPath, 'utf8');
    managed = true;
  } catch (error) {
    if (error.code !== 'ENOENT') throw error;
  }

  await mkdir(directory, { recursive: true });
  await writeFile(markerPath, 'Generated by tools/bruno/generate-bruno.mjs. Do not edit generated request files by hand.\n');
  return managed;
}

async function writeCollectionFiles(api, directory, managed) {
  const operations = collectOperations(api);
  const written = new Set([path.resolve(directory, generatedMarker)]);

  const write = async (filePath, content) => {
    await mkdir(path.dirname(filePath), { recursive: true });
    await writeWithRetry(filePath, content);
    written.add(path.resolve(filePath));
  };

  // Root der OpenCollection (.yml-Format): Collection-weite Bearer-Auth mit {{token}},
  // die alle Ordner/Requests per `auth: inherit` erben.
  await write(path.join(directory, 'opencollection.yml'), collectionRoot());
  await write(path.join(directory, 'README.md'), collectionReadme());
  await write(path.join(directory, 'environments', 'local.yml'), localEnvironment());

  // Verschachtelter Ordnerbaum statt flacher Tag-Liste (Tier-Wurzel → Routen-Schachtelung →
  // kuratierte Sammelordner). placeOperations liefert je Tag den vollständigen Knotenpfad.
  await writeFolderTree(placeOperations(operations), directory, write);

  if (managed) await pruneStale(directory, written);
}

// Ordnet jedem Tag seinen Ort im Baum zu. Ergebnis: [{ tag, nodes }] mit `nodes` = Kette von
// Ordnerknoten (tier → … → tag) von der Wurzel bis zum Blatt.
function placeOperations(operations) {
  const byTag = new Map();
  for (const operation of operations) {
    if (!byTag.has(operation.tag)) byTag.set(operation.tag, []);
    byTag.get(operation.tag).push(operation);
  }

  const tags = [...byTag.entries()].map(([name, ops]) => ({
    name,
    operations: ops,
    tier: tierOf(ops[0].path),
    key: canonicalKey(ops),
  }));
  // Routen-Key → Tag, um den Eltern-Tag beim Schachteln aufzulösen (längster echter Präfix gewinnt).
  const byKey = new Map(tags.map(tag => [tag.key.join('/'), tag]));

  const tierNode = tier => ({ kind: 'tier', name: tierDisplay(tier), dir: tierDisplay(tier) });
  const groupNode = group => ({ kind: 'group', name: group.name, dir: group.name });
  const tagNode = tag => ({ kind: 'tag', name: tag.name, dir: folderSlug(tag.name) });

  // Eltern-Tag = jener Tag, dessen kanonische Route der längste echte Präfix dieser Route ist.
  const parentTagOf = tag => {
    for (let length = tag.key.length - 1; length >= 1; length -= 1) {
      const owner = byKey.get(tag.key.slice(0, length).join('/'));
      if (owner && owner !== tag) return owner;
    }
    return null;
  };

  const matchingGroup = tag => curatedGroups.find(group => group.tier === tag.tier && group.match(tag));

  // Voller Pfad eines Tags: kuratierte Gruppe hat Vorrang vor der reinen Routen-Schachtelung; beide
  // hängen sich rekursiv an den Pfad ihres Eltern-Knotens.
  const pathOf = tag => {
    const group = matchingGroup(tag);
    if (group) {
      const parent = group.parentKey.length > 0 ? byKey.get(group.parentKey.join('/')) : null;
      const base = parent ? pathOf(parent) : [tierNode(tag.tier)];
      return [...base, groupNode(group), tagNode(tag)];
    }

    const parent = parentTagOf(tag);
    const base = parent ? pathOf(parent) : [tierNode(tag.tier)];
    return [...base, tagNode(tag)];
  };

  return tags.map(tag => ({ tag, nodes: collapsePath(pathOf(tag)) }));
}

// Schreibt Ordner-Metadaten (folder.yml je Knoten) und die Requests in ihre Blatt-Ordner. Ein Tag-Ordner
// kann zugleich Requests und Unterordner tragen (z. B. Subjects mit Requests + Chapters-Unterordner).
async function writeFolderTree(placements, directory, write) {
  const folders = new Map();
  for (const { nodes } of placements) {
    let parentDir = '';
    for (const node of nodes) {
      const dir = parentDir ? `${parentDir}/${node.dir}` : node.dir;
      if (!folders.has(dir)) folders.set(dir, { name: node.name, parentDir, dir });
      parentDir = dir;
    }
  }

  // seq je Geschwistergruppe (alphabetisch nach Anzeigename, stabil).
  const siblings = new Map();
  for (const folder of folders.values()) {
    if (!siblings.has(folder.parentDir)) siblings.set(folder.parentDir, []);
    siblings.get(folder.parentDir).push(folder);
  }
  const seqOf = new Map();
  for (const list of siblings.values()) {
    list.sort((left, right) => left.name.localeCompare(right.name));
    list.forEach((folder, index) => seqOf.set(folder.dir, index + 1));
  }

  for (const folder of folders.values()) {
    const target = path.join(directory, ...folder.dir.split('/'), 'folder.yml');
    await write(target, folderFile(folder.name, seqOf.get(folder.dir)));
  }

  for (const { tag, nodes } of placements) {
    const leafDir = path.join(directory, ...nodes.map(node => node.dir));
    const usedNames = new Set();
    for (const [index, operation] of tag.operations.entries()) {
      const fileName = uniqueName(usedNames, operationSlug(operation));
      await write(path.join(leafDir, `${fileName}.yml`), renderRequest(operation, index + 1));
    }
  }
}

// Erster Routen-Segment nach /api/v1 = Tier (creator/supervisor/student/auth).
function tierOf(apiPath) {
  return apiPath.split('/').filter(Boolean).at(2) ?? 'api';
}

function tierDisplay(tier) {
  return tier.charAt(0).toUpperCase() + tier.slice(1);
}

// Statische Routen-Segmente nach /api/v1 (Parameter wie {subjectId} entfallen) – Basis für Präfix-Vergleich.
function routeKey(apiPath) {
  return apiPath.split('/').filter(Boolean).slice(2).filter(segment => !segment.startsWith('{'));
}

// Kanonische Route eines Tags = kürzeste (spezifischste-neutrale) Route; bestimmt die Einhängung.
function canonicalKey(operations) {
  let best = null;
  for (const operation of operations) {
    const key = routeKey(operation.path);
    if (!best || key.length < best.length || (key.length === best.length && key.join('/') < best.join('/'))) best = key;
  }
  return best ?? [];
}

// Kollabiert einen Tag, dessen Name dem Tier entspricht (z. B. „Auth"), in den Tier-Ordner – sonst
// entstünde ein redundantes Auth/Auth. Die Requests landen dann direkt im Tier-Ordner.
function collapsePath(nodes) {
  if (nodes.length >= 2) {
    const last = nodes.at(-1);
    const parent = nodes.at(-2);
    if (last.kind === 'tag' && parent.kind === 'tier' && last.name === parent.name) return nodes.slice(0, -1);
  }
  return nodes;
}

// Entfernt Dateien/leere Ordner, die zu einem früheren Lauf gehörten (z. B. umbenannte Slugs), aber
// diesmal nicht geschrieben wurden. Best-effort: eine gesperrte Datei bricht den Sync nicht ab.
async function pruneStale(directory, written) {
  const failures = [];
  // Vergleich case-insensitiv: Auf case-insensitiven Dateisystemen (Windows/macOS) meint eine reine
  // Groß-/Kleinschreibungs-Umbenennung (altes `auth` → neues `Auth`) denselben Ordner. Ein case-sensitiver
  // Set-Vergleich hielte die frisch geschriebene Datei fälschlich für verwaist und löschte sie wieder.
  const keep = new Set([...written].map(entry => entry.toLowerCase()));

  const walk = async currentDir => {
    let entries;
    try {
      entries = await readdir(currentDir, { withFileTypes: true });
    } catch {
      return true;
    }

    let emptied = true;
    for (const entry of entries) {
      const entryPath = path.join(currentDir, entry.name);
      if (entry.isDirectory()) {
        const childEmpty = await walk(entryPath);
        if (childEmpty) {
          try { await rm(entryPath, { recursive: true, maxRetries: 5, retryDelay: 150 }); }
          catch { emptied = false; failures.push(entryPath); }
        } else {
          emptied = false;
        }
        continue;
      }

      if (keep.has(path.resolve(entryPath).toLowerCase())) {
        emptied = false;
        continue;
      }

      try { await rm(entryPath, { force: true, maxRetries: 5, retryDelay: 150 }); }
      catch { emptied = false; failures.push(entryPath); }
    }

    return emptied;
  };

  await walk(directory);
  if (failures.length > 0) {
    console.warn(`Warnung: ${failures.length} veraltete Datei(en) konnten nicht entfernt werden (gesperrt?):`);
    for (const file of failures) console.warn(`  ${path.relative(directory, file)}`);
  }
}

// writeFile mit kurzen Retries gegen transiente Windows-Sperren (Virenscanner, Indexer, IDE-Watcher).
async function writeWithRetry(filePath, content, attempts = 5) {
  for (let attempt = 1; ; attempt += 1) {
    try {
      await writeFile(filePath, content);
      return;
    } catch (error) {
      if (attempt >= attempts) throw new Error(`Konnte ${filePath} nicht schreiben (${error.code ?? error.message}). Ist die Datei in einem Editor geöffnet/gesperrt?`);
      await delay(150 * attempt);
    }
  }
}

function delay(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

function collectOperations(api) {
  const methods = new Set(['get', 'post', 'put', 'patch', 'delete', 'head', 'options']);
  const operations = [];

  for (const [apiPath, pathItem] of Object.entries(api.paths ?? {})) {
    for (const [method, operation] of Object.entries(pathItem)) {
      if (!methods.has(method)) continue;

      const params = collectParams(api, pathItem, operation, apiPath);
      // Nur Path-Parameter brauchen eine Environment-Variable (ihr Wert ist {{var}}); Query-Filter
      // tragen literale Beispielwerte und würden das Environment sonst mit Dutzenden Einträgen fluten.
      for (const raw of collectPathParameters(apiPath)) discoveredVariables.add(toVariableName(raw));

      const bodyText = sampleBodyText(api, operation);

      operations.push({
        method: method.toUpperCase(),
        path: apiPath,
        url: `{{baseUrl}}${replacePathVariables(apiPath)}`,
        tag: operation.tags?.[0] ?? firstPathSegment(apiPath) ?? 'API',
        name: oneLine(operation.summary?.trim() || `${method.toUpperCase()} ${apiPath}`),
        params,
        bodyText,
        description: oneLine(operation.description),
        isAuthEndpoint: isAuthEndpoint(apiPath),
        captureScript: captureScript(apiPath),
        examples: collectExamples(operation, bodyText),
      });
    }
  }

  return operations.sort((left, right) => `${left.tag} ${left.path} ${left.method}`.localeCompare(`${right.tag} ${right.path} ${right.method}`));
}

// Ein Request als OpenCollection-.yml. Reihenfolge der Top-Level-Blöcke wie beim nativen Bruno-Import.
function renderRequest(operation, sequence) {
  const http = {
    method: operation.method,
    url: quoted(operation.url),
    params: operation.params.length > 0 ? operation.params.map(paramNode) : undefined,
    body: operation.bodyText !== undefined ? { type: 'json', data: new Block(operation.bodyText) } : undefined,
    // Alle Requests erben die Collection-Bearer-Auth. Die Login-Endpunkte sind anonym und
    // ignorieren den (anfangs leeren) Token serverseitig; ihr Script setzt {{token}} für den Rest.
    auth: 'inherit',
  };

  const document = {
    info: { name: operation.name, type: 'http', seq: sequence, tags: [operation.tag] },
    http,
    runtime: operation.captureScript ? { scripts: [{ type: 'after-response', code: new Block(operation.captureScript) }] } : undefined,
    settings: { encodeUrl: true, timeout: 0, followRedirects: true, maxRedirects: 5 },
    examples: operation.examples.length > 0 ? operation.examples.map(exampleNode(operation)) : undefined,
    docs: new Block(docsMarkdown(operation)),
  };

  return dumpDocument(document);
}

function paramNode(parameter) {
  return {
    name: parameter.name,
    value: quoted(parameter.value),
    type: parameter.type,
    description: parameter.description ? oneLine(parameter.description) : undefined,
    // Optionale Query-Parameter ohne Default sind abgehakt (disabled), damit Bruno nicht bei jedem
    // Request `?a=&b=` leer mitschickt. Der Nutzer aktiviert gezielt die gewünschten Filter.
    disabled: parameter.enabled === false ? true : undefined,
  };
}

// Baut einen nativen Bruno-Beispiel-Eintrag: pro Szenario echter Request-Input (gepaart über den
// gemeinsamen Example-Key von requestBody/response) + aufgezeichneter Response-Body.
function exampleNode(operation) {
  return example => {
    const request = { url: quoted(operation.url), method: operation.method };
    if (operation.params.length > 0) request.params = operation.params.map(paramNode);
    if (example.requestBodyText !== undefined) request.body = { type: 'json', data: new Block(example.requestBodyText) };

    return {
      name: example.name,
      description: example.description || undefined,
      request,
      response: {
        status: example.code,
        statusText: example.statusText,
        headers: [{ name: 'Content-Type', value: 'application/json' }],
        body: { type: 'json', data: new Block(example.responseBodyText) },
      },
    };
  };
}

function docsMarkdown(operation) {
  const lines = [`# ${operation.name}`, '', `\`${operation.method} ${operation.path}\``];
  if (operation.description) lines.push('', operation.description);
  return lines.join('\n');
}

function collectParams(api, pathItem, operation, apiPath) {
  const declared = [...(pathItem.parameters ?? []), ...(operation.parameters ?? [])];
  const describe = (name, location) => declared.find(parameter => parameter.name === name && parameter.in === location)?.description;

  const params = [];

  // Path-Parameter in Pfad-Reihenfolge; Wert als Variable, damit die Tabelle vorbelegt ist. Immer aktiv.
  for (const raw of collectPathParameters(apiPath)) {
    const variable = toVariableName(raw);
    params.push({ name: variable, value: `{{${variable}}}`, type: 'path', description: describe(raw, 'path'), enabled: true });
  }

  for (const parameter of declared.filter(parameter => parameter.in === 'query')) {
    const variable = toVariableName(parameter.name);
    const schema = resolveSchema(api, parameter.schema) ?? parameter.schema ?? {};
    // Aktiv nur, wenn der Server den Parameter erwartet (required) oder einen sinnvollen Default liefert
    // (z. B. skip/take/matchAll) – wie beim nativen Bruno-Import. Sonst abgehakt.
    const enabled = parameter.required === true || schema.default !== undefined;
    // Bekannte Ressourcen-IDs bleiben variabilisiert (chainbar); übrige Filter bekommen einen literalen
    // Beispielwert und landen NICHT im Environment, damit dieses schlank bleibt.
    const value = knownVariables.has(variable) ? `{{${variable}}}` : queryScalarSample(api, schema);
    params.push({ name: parameter.name, value, type: 'query', description: parameter.description, enabled });
  }

  return params;
}

// Ein einzelner, URL-tauglicher Beispielwert für einen Query-Parameter (Default/Enum-Erstwert/typ-basiert).
function queryScalarSample(api, schema) {
  const resolved = resolveSchema(api, schema) ?? schema ?? {};
  if (resolved.default !== undefined) return String(resolved.default);
  if (resolved.enum?.length) return String(resolved.enum[0]);

  const composite = resolved.oneOf ?? resolved.anyOf ?? resolved.allOf;
  if (composite?.length) return queryScalarSample(api, composite[0]);

  const type = Array.isArray(resolved.type) ? resolved.type.find(entry => entry !== 'null') : resolved.type;
  if (type === 'boolean') return 'false';
  return '';
}

function collectExamples(operation, bodySampleText) {
  const jsonRequest = operation.requestBody?.content?.['application/json'];
  const requestExamples = jsonRequest?.examples;
  const hasBody = jsonRequest !== undefined;
  const result = [];

  for (const [code, response] of Object.entries(operation.responses ?? {})) {
    const content = response.content?.['application/json'];
    if (!content) continue;

    const entries = content.examples
      ? Object.entries(content.examples)
      : ('example' in content ? [[null, { value: content.example }]] : []);

    for (const [key, example] of entries) {
      const pairedRequest = key ? requestExamples?.[key]?.value : undefined;
      result.push({
        name: oneLine(example.summary?.trim() || key || `HTTP ${code}`),
        description: oneLine(example.description),
        requestBodyText: hasBody
          ? (pairedRequest !== undefined ? JSON.stringify(pairedRequest, null, 2) : bodySampleText)
          : undefined,
        code: Number(code),
        statusText: reasonPhrases.get(Number(code)) ?? response.description ?? '',
        responseBodyText: JSON.stringify(example.value, null, 2),
      });
    }
  }

  return result;
}

function collectPathParameters(apiPath) {
  return [...apiPath.matchAll(/\{([^}:]+)(?::[^}]+)?\}/g)].map(match => match[1]);
}

// Bruno-Pfadparameter-Syntax: `:name` im URL, Zuordnung zum Wert über den params-Block (type: path).
function replacePathVariables(apiPath) {
  return apiPath.replace(/\{([^}:]+)(?::[^}]+)?\}/g, (_, name) => `:${toVariableName(name)}`);
}

function isAuthEndpoint(apiPath) {
  return apiPath.startsWith('/api/v1/auth/father') || apiPath.startsWith('/api/v1/auth/child');
}

function sampleBodyText(api, operation) {
  const body = sampleBody(api, operation);
  return body === undefined ? undefined : JSON.stringify(body, null, 2);
}

function sampleBody(api, operation) {
  const jsonContent = operation.requestBody?.content?.['application/json'];
  if (!jsonContent) return undefined;

  const explicitExample = jsonContent.example ?? firstExampleValue(jsonContent.examples);
  if (explicitExample !== undefined) return substituteVariables(explicitExample);

  const schema = resolveSchema(api, jsonContent.schema);
  if (!schema) return {};

  return substituteVariables(sampleFromSchema(api, schema));
}

function firstExampleValue(examples) {
  const first = Object.values(examples ?? {})[0];
  return first?.value;
}

function sampleFromSchema(api, schema) {
  if (schema.example !== undefined) return schema.example;
  if (schema.default !== undefined) return schema.default;
  if (schema.enum?.length) return schema.enum[0];
  if (schema.oneOf?.length) return sampleFromSchema(api, resolveSchema(api, schema.oneOf[0]) ?? schema.oneOf[0]);
  if (schema.anyOf?.length) return sampleFromSchema(api, resolveSchema(api, schema.anyOf[0]) ?? schema.anyOf[0]);
  if (schema.allOf?.length) return Object.assign({}, ...schema.allOf.map(item => sampleFromSchema(api, resolveSchema(api, item) ?? item)));

  if (schema.type === 'array') return [sampleFromSchema(api, resolveSchema(api, schema.items) ?? schema.items ?? { type: 'string' })];
  if (schema.type === 'object' || schema.properties) {
    const result = {};
    for (const [name, property] of Object.entries(schema.properties ?? {})) {
      result[name] = valueForProperty(api, name, resolveSchema(api, property) ?? property);
    }
    return result;
  }

  return primitiveSample(schema.type, schema.format);
}

function valueForProperty(api, name, schema) {
  const variableName = toVariableName(name);
  if (knownVariables.has(variableName)) return `{{${variableName}}}`;
  if (name.toLowerCase() === 'pin') return '{{fatherPin}}';
  if (schema.format === 'date') return '{{date}}';
  return sampleFromSchema(api, schema);
}

function primitiveSample(type, format) {
  if (format === 'date') return '{{date}}';
  if (format === 'date-time') return new Date().toISOString();
  if (type === 'integer' || type === 'number') return 1;
  if (type === 'boolean') return true;
  return 'string';
}

function resolveSchema(api, schema) {
  if (!schema?.$ref) return schema;

  const pathParts = schema.$ref.replace(/^#\//, '').split('/').map(part => part.replaceAll('~1', '/').replaceAll('~0', '~'));
  return pathParts.reduce((current, part) => current?.[part], api);
}

function substituteVariables(value) {
  if (Array.isArray(value)) return value.map(substituteVariables);
  if (value && typeof value === 'object') {
    const loginPin = value.childId !== undefined ? '{{childPin}}' : value.fatherId !== undefined ? '{{fatherPin}}' : undefined;
    return Object.fromEntries(Object.entries(value).map(([key, entry]) => [key, substituteVariablesForKey(key, entry, loginPin)]));
  }
  return value;
}

function substituteVariablesForKey(key, value, loginPin) {
  const variableName = toVariableName(key);
  if (knownVariables.has(variableName)) return `{{${variableName}}}`;
  if (key.toLowerCase() === 'pin') return loginPin ?? '{{fatherPin}}';
  return substituteVariables(value);
}

function captureVariablesFor(apiPath) {
  if (apiPath === '/api/v1/auth/father') return ['token', 'fatherId'];
  if (apiPath === '/api/v1/auth/child') return ['token', 'childId'];

  const variables = new Set(collectPathParameters(apiPath).map(toVariableName));
  for (const segment of apiPath.split('/').filter(Boolean).reverse()) {
    if (resourceIdBySegment.has(segment)) {
      variables.add(resourceIdBySegment.get(segment));
      break;
    }
  }

  return [...variables];
}

// after-response-Script: Login setzt {{token}}; alle anderen fangen IDs aus der Antwort ins
// Environment, sodass Folge-Requests (z. B. neu angelegte Ressourcen) direkt darauf zugreifen.
function captureScript(apiPath) {
  if (isAuthEndpoint(apiPath)) {
    // Token als RUNTIME-Variable (bru.setVar), NICHT ins Environment: Bruno liest Environment-Variablen
    // bei jedem Collection-Reload neu aus der Datei – und schon ein Request-Edit löst so einen Reload aus.
    // Ein per setEnvVar (ohne { persist: true }) gesetzter Token wird dabei auf den Dateiwert "" zurück-
    // gesetzt → stiller 401. Runtime-Variablen sind nicht dateigebunden, überleben den Reload und haben
    // bei der Auflösung von {{token}} ohnehin Vorrang (die leere Environment-Variable wird überstimmt).
    // Entspricht Brunos Empfehlung, Zugangsdaten/Tokens mit bru.setVar statt bru.setEnvVar zu halten.
    return `var jsonData = res.getBody();\nbru.setVar('token', jsonData.token);`;
  }

  const mappings = captureVariablesFor(apiPath).map(variable => `capture('${variable}', body.${variable} ?? body.id);`);
  const genericCaptures = [...knownVariables.keys()]
    .filter(variable => variable.endsWith('Id'))
    .map(variable => `capture('${variable}', body.${variable});`);

  const bodyLines = [...new Set([...mappings, ...genericCaptures])].join('\n');
  if (!bodyLines) return undefined;

  // Bewusst NUR ins aktive Environment schreiben (kein bru.setVar): Runtime-Variablen haben in Bruno
  // Vorrang vor Environment-Variablen und ließen sich im Environment-Editor nicht mehr überschreiben.
  // So landet jeder gefangene Wert als Environment-Variable, die der Nutzer notfalls von Hand ändern kann.
  return `const body = res.getBody();\n\nfunction capture(name, value) {\n  if (value === undefined || value === null || value === '') return;\n  bru.setEnvVar(name, String(value));\n}\n\nif (body && typeof body === 'object' && !Array.isArray(body)) {\n${indent(bodyLines, 2)}\n}`;
}

function collectionRoot() {
  return `opencollection: 1.0.0

info:
  name: Pugling API

request:
  auth:
    type: bearer
    token: "{{token}}"
bundled: false
extensions:
  bruno:
    ignore:
      - node_modules
      - .git
`;
}

function folderFile(tag, sequence) {
  return dumpDocument({
    info: { name: tag, type: 'folder', seq: sequence },
    request: { auth: 'inherit' },
  });
}

function localEnvironment() {
  const values = new Map([
    ...knownVariables.entries(),
    ...[...discoveredVariables].sort().map(variable => [variable, defaultValueFor(variable)]),
  ]);

  const variables = [
    { name: 'baseUrl', value: 'http://localhost:5200' },
    { name: 'token', value: quoted('') },
    ...[...values.entries()].map(([name, value]) => ({ name, value: quoted(String(value)) })),
  ];

  return dumpDocument({ name: 'local', variables });
}

function defaultValueFor(variableName) {
  if (knownVariables.has(variableName)) return knownVariables.get(variableName);

  const lower = variableName.toLowerCase();
  if (variableName.endsWith('Id')) return '1';
  if (lower.includes('date')) return '{{date}}';
  if (lower === 'skip' || lower === 'offset') return '0';
  if (lower === 'take' || lower === 'limit' || lower === 'pagesize') return '100';
  if (lower === 'page') return '1';
  if (lower.includes('active')) return 'true';
  return '';
}

function collectionReadme() {
  return `# Pugling API Bruno Collection

Generiert im OpenCollection-\`.yml\`-Format. Manuelle Änderungen gehen beim nächsten Export verloren.

## Aktualisieren

\`npm run bruno:generate\`

Standardquelle ist \`http://localhost:5200/openapi/v1.json\`. Alternativ eine gespeicherte OpenAPI-Datei:

\`node tools/bruno/generate-bruno.mjs --input ./openapi.json --output tools/bruno/Pugling.Api\`

## Auth

Die Collection (\`opencollection.yml\`) trägt Bearer-Auth mit \`{{token}}\`; Ordner und Requests erben sie
per \`auth: inherit\`. Die Login-Requests (\`auth: none\`) setzen \`token\` per after-response-Script
automatisch ins aktive Environment.

## Variablen & Beispiele

Pfad-/Query-Werte sind als \`{{variable}}\` vorbelegt (Environment \`environments/local.yml\`).
Jeder Request bringt die von \`DocsCaptureTests\` verifizierten \`examples\` (Request-Eingabe + Response) mit.
`;
}

function firstPathSegment(apiPath) {
  return apiPath.split('/').filter(Boolean).at(2);
}

function toVariableName(name) {
  return name.replace(/[^a-zA-Z0-9]+(.)/g, (_, character) => character.toUpperCase());
}

function uniqueName(used, name) {
  let candidate = name;
  let counter = 2;
  while (used.has(candidate)) candidate = `${name} (${counter++})`;
  used.add(candidate);
  return candidate;
}

// Dateiname aus HTTP-Methode + Route (stabil, rein ASCII, kein Satzzeichen). Der menschenlesbare
// Titel bleibt in `info.name` erhalten und wird von Bruno angezeigt.
// GET /api/v1/supervisor/children -> get-supervisor-children; POST /api/v1/supervisor/children/{childId}/points -> post-children-childId-points
function operationSlug(operation) {
  const route = operation.path.replace(/^\/api\/v1\//, '').replace(/[{}]/g, '');
  return `${operation.method.toLowerCase()}-${route}`
    .replace(/[^a-zA-Z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '') || 'request';
}

// Ordnername als ASCII-Slug des Tags (Leerzeichen/En-Dash/Akzente entfernt); Anzeigename bleibt in folder.yml.
// "Admin \u2013 Children" -> admin-children
function folderSlug(tag) {
  return String(tag)
    .normalize('NFKD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-zA-Z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .toLowerCase() || 'folder';
}

function oneLine(value) {
  return value === undefined || value === null ? undefined : String(value).replace(/\s*[\r\n]+\s*/g, ' ').trim();
}

// --- Minimaler YAML-Emitter (nur die hier benötigte Teilmenge) -------------------------------

// Markiert einen String als literalen Blockskalar (`|-`), z. B. JSON-Bodies und Script-Code.
class Block {
  constructor(text) {
    this.text = String(text);
  }
}

// Markiert einen bereits fertig formatierten Skalar (z. B. eine Variable/URL), der wörtlich – ggf.
// in Anführungszeichen – ausgegeben werden soll, statt erneut die Quoting-Heuristik zu durchlaufen.
class Raw {
  constructor(text) {
    this.text = String(text);
  }
}

function quoted(value) {
  return new Raw(needsQuote(String(value)) ? doubleQuote(String(value)) : String(value));
}

function needsQuote(value) {
  if (value === '') return true;
  if (/^\s|\s$/.test(value)) return true;
  if (/^[-?:,\[\]{}#&*!|>'"%@`]/.test(value)) return true;
  if (/:(\s|$)/.test(value)) return true;
  if (/\s#/.test(value)) return true;
  if (/^(true|false|null|~|yes|no|on|off)$/i.test(value)) return true;
  if (/^[+-]?(\d+\.?\d*|\.\d+)([eE][+-]?\d+)?$/.test(value)) return true;
  if (/^\d{4}-\d\d-\d\d/.test(value)) return true;
  return false;
}

function doubleQuote(value) {
  return `"${value.replace(/\\/g, '\\\\').replace(/"/g, '\\"')}"`;
}

function scalar(value) {
  if (value instanceof Raw) return value.text;
  if (value === null) return 'null';
  if (typeof value === 'boolean' || typeof value === 'number') return String(value);
  const text = String(value);
  return needsQuote(text) ? doubleQuote(text) : text;
}

function isMapping(value) {
  return value !== null && typeof value === 'object' && !Array.isArray(value) && !(value instanceof Block) && !(value instanceof Raw);
}

function dumpMapping(object, level) {
  const pad = '  '.repeat(level);
  const lines = [];

  for (const [key, value] of Object.entries(object)) {
    if (value === undefined) continue;

    if (value instanceof Block) {
      lines.push(`${pad}${key}: |-`);
      const blockPad = '  '.repeat(level + 1);
      for (const line of value.text.split('\n')) lines.push(line === '' ? '' : `${blockPad}${line}`);
      continue;
    }

    if (Array.isArray(value)) {
      if (value.length === 0) {
        lines.push(`${pad}${key}: []`);
        continue;
      }
      lines.push(`${pad}${key}:`);
      lines.push(...dumpArray(value, level + 1));
      continue;
    }

    if (isMapping(value)) {
      lines.push(`${pad}${key}:`);
      lines.push(...dumpMapping(value, level + 1));
      continue;
    }

    lines.push(`${pad}${key}: ${scalar(value)}`);
  }

  return lines;
}

function dumpArray(array, level) {
  const pad = '  '.repeat(level);
  const lines = [];

  for (const item of array) {
    if (isMapping(item)) {
      const inner = dumpMapping(item, level + 1);
      const firstContent = inner[0].slice((level + 1) * 2);
      lines.push(`${pad}- ${firstContent}`);
      for (let index = 1; index < inner.length; index += 1) lines.push(inner[index]);
      continue;
    }

    lines.push(`${pad}- ${scalar(item)}`);
  }

  return lines;
}

// Top-Level-Blöcke durch Leerzeilen getrennt – wie in den nativ importierten .yml-Dateien.
function dumpDocument(object) {
  const blocks = [];
  for (const [key, value] of Object.entries(object)) {
    if (value === undefined) continue;
    blocks.push(dumpMapping({ [key]: value }, 0).join('\n'));
  }
  return `${blocks.join('\n\n')}\n`;
}

function indent(value, spaces) {
  const prefix = ' '.repeat(spaces);
  return String(value).split('\n').map(line => `${prefix}${line}`).join('\n');
}

// Ausführung am Dateiende: die Emitter-Klassen (Block/Raw) sind Klassendeklarationen und
// nicht gehoistet – der Einstieg muss daher hinter ihrer Initialisierung stehen.
const openApi = await loadOpenApi(input);
const managed = await prepareOutput(output, force);
await writeCollectionFiles(openApi, output, managed);

console.log(`Bruno-Collection erzeugt: ${output}`);
