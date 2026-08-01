// Erzeugt `src/lib/contract.ts` aus dem eingecheckten Vertragsdokument (`docs/openapi/v1.json`,
// geschrieben von `ContractDocumentTests`). Aufruf: `npm run gen:contract`.
//
// Warum ein Skript und kein CLI-Aufruf in package.json: der Generator braucht einen `transform`-Haken,
// und der ist über die Kommandozeile nicht erreichbar. Begründung des Hakens steht an ihm.
import { existsSync, writeFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import openapiTS, { NUMBER, astToString, tsNullable } from "openapi-typescript";

const quelle = new URL("../../docs/openapi/v1.json", import.meta.url);
const ziel = fileURLToPath(new URL("../src/lib/contract.ts", import.meta.url));

// Das Skript hängt an `postinstall`. Ohne diese Prüfung bräche `npm install` mit einem rohen ENOENT ab und
// sähe wie ein Abhängigkeitsproblem aus – dabei fehlt nur die Quelle (Teil-Checkout, frontend/ herauskopiert).
if (!existsSync(fileURLToPath(quelle))) {
  console.error(`Vertragsdokument fehlt: ${fileURLToPath(quelle)}\n`
    + "Es entsteht bei jedem Backend-Testlauf (ContractDocumentTests) und ist eingecheckt – dieses Frontend "
    + "braucht das ganze Repo, nicht nur den frontend/-Ordner.");
  process.exit(1);
}

const ast = await openapiTS(quelle, {
  /*
   * Ein Feld mit `default` bleibt **optional**. Die Vorgabe des Generators ist umgekehrt: ein Vorgabewert
   * heißt für ihn „steht in der Antwort immer da", also Pflichtfeld. Hier tragen Vorgabewerte aber
   * ausschließlich **Eingabe**-Felder – die `clear<Feld>`-Schalter der PATCH-Semantik und
   * `CreateMediaVariantDto.format = "webp"`. Als Pflicht gelesen müsste jedes Formular `clearGrade: false`
   * mitschicken, um nichts zu leeren.
   */
  defaultNonNullable: false,

  /*
   * Ganzzahlen kommen im Dokument als `type: ["integer", "string"]` samt Ziffern-`pattern`. Das ist keine
   * Merkwürdigkeit des Dokuments, sondern die Wahrheit über die **Eingabe**: `JsonSerializerDefaults.Web`
   * schaltet `NumberHandling.AllowReadingFromString` ein, der Server nimmt also auch `"5"` für eine Id an.
   * Geschrieben wird aber immer eine Zahl.
   *
   * Ungefiltert wäre jede Id im Frontend `number | string` – gemessen **158 von 169** `tsc`-Fehlern
   * (`id + 1`, `<select value={id}>`, jeder Vergleich). Der Preis dafür wäre eine Einengung an jeder
   * Lesestelle, für eine Nachsicht, die kein Client nutzt. Darum hier, an einer Stelle, mit Begründung:
   * die Vertragstypen führen Ganzzahlen als `number`.
   */
  transform(schema) {
    const typen = Array.isArray(schema.type) ? schema.type : [schema.type];
    if (!typen.includes("string")) return undefined;
    if (!typen.includes("integer") && !typen.includes("number")) return undefined;
    return typen.includes("null") ? tsNullable([NUMBER]) : NUMBER;
  },
});

const kopf = [
  "/*",
  " * ERZEUGT – nicht von Hand ändern. Quelle: docs/openapi/v1.json (schreibt `ContractDocumentTests`).",
  " * Neu erzeugen: `npm run gen:contract`. Die Hand-Ausnahmen und ihre Begründung stehen in src/lib/types.ts.",
  " */",
  "",
].join("\n");

writeFileSync(ziel, kopf + astToString(ast));
console.log(`${ziel} erzeugt.`);
