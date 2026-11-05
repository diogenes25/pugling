import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { artenPending, CategoryRows, CategorySection, NameRow, SubjectRow } from "./CatalogAdmin";
import type { CategoryResponse, SubjectResponse } from "../lib/types";

/*
 * Der Regressionstest zu B-154. Geprüft wird `SubjectRow` einzeln, nicht die ganze `CatalogAdmin`: die
 * lädt beim Fachwechsel die Arten nach und hinge damit am Netz – Bausteine hier, Wege durch die App bei
 * Playwright (frontend/CLAUDE.md). Kein Test dieses Frontends mockt `../lib/api`, und das bleibt so.
 *
 * Der Fehler, den er festhält: Seit B-13 verweigert der Server `PATCH`/`DELETE` an einem fremden **und**
 * an einem ownerlosen Fach mit `403 not_owner`. Die Zeile bot „OK" und „Löschen" trotzdem an jedem Fach
 * an, und der Löschdialog zählte vorher noch auf, was alles seine Zuordnung verliert. Die Antwort trug
 * das Eigentums-Flag von Anfang an (damals `isMine`, seit B-156 `isOwn`) – gelesen wurde es nicht.
 *
 * Warum die zwei Nicht-Eigentümer-Fälle getrennt geprüft werden: „hat jemand anderes angelegt" ist bei
 * einem Fach aus dem Grundbestand nicht bloß unschön, sondern falsch – es gehört niemandem.
 */

function subject(over: Partial<SubjectResponse> = {}): SubjectResponse {
  return {
    id: 3, name: "Englisch", createdAt: "2026-08-12T00:00:00Z", categoriesCount: 2,
    ownerAdultId: 1, isOwn: true, ...over,
  };
}

const okKnopf = () => screen.queryByRole("button", { name: /speichern$/ });
const loeschKnopf = () => screen.queryByRole("button", { name: /löschen$/ });

describe("SubjectRow – Eigentum entscheidet, was angeboten wird", () => {
  it("bietet am eigenen Fach Umbenennen und Löschen an – und speichert wirklich", () => {
    const onSave = vi.fn();
    render(<SubjectRow subject={subject()} busy={false} onSave={onSave} onDelete={() => {}} />);

    const feld = screen.getByLabelText("Fach umbenennen");
    expect(loeschKnopf()).not.toBeNull();
    // „OK" erscheint erst bei echter Änderung (`dirty`), darum wird hier getippt statt nur geschaut:
    // ohne diesen Schritt wäre jede Zusicherung auf den OK-Knopf leer und könnte nie fehlschlagen.
    expect(okKnopf()).toBeNull();

    fireEvent.change(feld, { target: { value: "Englisch neu" } });

    expect(okKnopf()).not.toBeNull();
    fireEvent.click(okKnopf()!);
    expect(onSave).toHaveBeenCalledWith("Englisch neu");
  });

  it("zeigt am fremden Fach keine Knöpfe, sondern den Grund", () => {
    render(<SubjectRow subject={subject({ isOwn: false, ownerAdultId: 7 })}
      busy={false} onSave={() => {}} onDelete={() => {}} />);

    expect(screen.queryByLabelText("Fach umbenennen")).toBeNull();
    expect(okKnopf()).toBeNull();
    expect(loeschKnopf()).toBeNull();
    expect(screen.getByText(/hat jemand anderes angelegt/)).toBeTruthy();
  });

  it("nennt beim Fach aus dem Grundbestand niemanden als Eigentümer", () => {
    render(<SubjectRow subject={subject({ isOwn: false, ownerAdultId: null })}
      busy={false} onSave={() => {}} onDelete={() => {}} />);

    expect(loeschKnopf()).toBeNull();
    expect(screen.getByText(/gehört zum Grundbestand/)).toBeTruthy();
    // Der falsche Satz darf nicht bloß fehlen, er darf hier gar nicht entstehen.
    expect(screen.queryByText(/hat jemand anderes angelegt/)).toBeNull();
  });

  it("behandelt ein fehlendes ownerAdultId wie ownerlos, nicht wie fremd", () => {
    // Der Vertrag gibt `ownerAdultId` optional heraus (`ownerAdultId?: number | null`) – ein strenges
    // `=== null` hätte hier den Satz über den fremden Creator erzeugt, und der wäre erfunden.
    const ohneFeld = subject({ isOwn: false });
    delete (ohneFeld as { ownerAdultId?: number | null }).ownerAdultId;

    render(<SubjectRow subject={ohneFeld} busy={false} onSave={() => {}} onDelete={() => {}} />);

    expect(screen.getByText(/gehört zum Grundbestand/)).toBeTruthy();
  });
});

/*
 * Die Gegenprobe zur Verengung: Die „Art" (Kategorie) trägt serverseitig **kein** Eigentum
 * (`ExerciseCategory` hat kein Owner-Feld, `ExerciseCategoriesController` prüft nur die Creator-Rolle),
 * also darf die Oberfläche dort auch keines erfinden. Belegt wird hier, dass `NameRow` selbst
 * bedingungslos beide Knöpfe zeigt – *dass* die Art-Zeilen sie unverändert benutzen, steht im Diff
 * dieser Story (nur die Fach-Zeile wurde angefasst) und ist nicht Sache einer Zusicherung.
 */
describe("NameRow – kennt kein Eigentum", () => {
  it("zeigt Löschen unabhängig von jedem Recht", () => {
    render(<NameRow fieldId="ca-category-9" label="Art" srName={'Art „Grammatik"'} value="Grammatik"
      busy={false} onSave={() => {}} onDelete={() => {}} />);

    expect(screen.getByLabelText("Art")).toBeTruthy();
    expect(screen.getByRole("button", { name: 'Art „Grammatik" löschen' })).toBeTruthy();
  });
});

/*
 * B-170. Bis dahin entschied das FACH über alle seine Arten, und dieser Block hieß entsprechend „das Fach
 * entscheidet". Das war zwei Stories lang die Wahrheit und ist es nicht mehr: Anlegen ist überall frei, also
 * bekam die Art einen eigenen Eigentümer — sonst blieb eine selbst angelegte Art im Grundbestands-Fach für
 * immer stehen, weil dort niemand das Fach besitzt.
 *
 * Was der Block jetzt prüft, ist darum die BINDUNG an das `isOwn` der Art, nicht mehr an das des Fachs. Der
 * entscheidende Fall ist der gemischte: In einem Fach stehen beide Sorten Zeilen nebeneinander. Eine
 * Fassung, die weiter pro Fach entscheidet, kann ihn nicht bestehen — und genau darum trägt er.
 *
 * `subject` ist als Prop entfallen: `CategoryRows` braucht das Fach nicht mehr, und es als Prop zu behalten
 * hieße, die alte Bindung offen zu lassen.
 */
function art(over: Partial<CategoryResponse> = {}): CategoryResponse {
  return { id: 9, subjectId: 3, name: "Grammatik", createdAt: "2026-08-13T00:00:00Z", isOwn: true, ...over };
}

describe("CategoryRows – die Art entscheidet, wer sie ändern darf", () => {
  it("zeigt an eigenen Arten je ein Feld und einen Löschknopf", () => {
    render(<CategoryRows categories={[art(), art({ id: 10, name: "Vokabeln" })]}
      busy={false} onSave={() => {}} onDelete={() => {}} />);

    expect(screen.getAllByLabelText("Art")).toHaveLength(2);
    expect(screen.getByRole("button", { name: 'Art „Grammatik" löschen' })).toBeTruthy();
    expect(screen.getByRole("button", { name: 'Art „Vokabeln" löschen' })).toBeTruthy();
  });

  it("reicht die geänderte Art samt neuem Namen nach oben", () => {
    const onSave = vi.fn();
    render(<CategoryRows categories={[art()]} busy={false} onSave={onSave} onDelete={() => {}} />);

    fireEvent.change(screen.getByLabelText("Art"), { target: { value: "Grammatik neu" } });
    fireEvent.click(screen.getByRole("button", { name: /speichern$/ }));

    expect(onSave).toHaveBeenCalledWith(art(), "Grammatik neu");
  });

  /*
   * Der tragende Fall dieser Story: ZWEI Arten im selben Fach, eine eigene und eine fremde. Eine Fassung,
   * die pro Fach entscheidet, liefert hier entweder zwei Felder oder keins — beides fällt hier.
   */
  it("entscheidet pro Zeile: die eigene Art trägt Knöpfe, die fremde daneben nicht", () => {
    render(<CategoryRows
      categories={[art({ id: 9, name: "Grammtik", isOwn: true }),
        art({ id: 10, name: "Grammatik", isOwn: false })]}
      busy={false} onSave={() => {}} onDelete={() => {}} />);

    // Genau EIN Feld, und es gehört der eigenen Art.
    expect(screen.getAllByLabelText("Art")).toHaveLength(1);
    expect((screen.getByLabelText("Art") as HTMLInputElement).value).toBe("Grammtik");
    expect(screen.getByRole("button", { name: 'Art „Grammtik" löschen' })).toBeTruthy();
    expect(screen.queryByRole("button", { name: 'Art „Grammatik" löschen' })).toBeNull();

    // Die fremde bleibt LESBAR - eine Seite, die zum Filtern auffordert und die Namen verschweigt, hält
    // ihr eigenes Versprechen nicht.
    expect(screen.getByText("Grammatik")).toBeTruthy();
  });

  it("zeigt an einer fremden Art keine Bedienelemente, aber ihren Namen", () => {
    render(<CategoryRows categories={[art({ isOwn: false })]}
      busy={false} onSave={() => {}} onDelete={() => {}} />);

    expect(screen.queryByLabelText("Art")).toBeNull();
    expect(screen.queryByRole("button", { name: /löschen$/ })).toBeNull();
    expect(screen.getByText("Grammatik")).toBeTruthy();
  });

  /*
   * Kein `disabled`-Knopf mit `title`: der bliebe im Fokusbaum und nennt seinen Grund nicht — der `title`
   * erscheint gar nicht (B-160). Der Fall pinnt die Entscheidung, weil sie sonst nur ein Kommentar wäre.
   */
  it("baut an einer fremden Art keinen gesperrten Knopf ein", () => {
    const { container } = render(<CategoryRows categories={[art({ isOwn: false })]}
      busy={false} onSave={() => {}} onDelete={() => {}} />);

    expect(container.querySelectorAll("button")).toHaveLength(0);
    expect(container.querySelectorAll("[title]")).toHaveLength(0);
  });

  it("schweigt, wenn das Fach gar keine Arten hat", () => {
    // Sonst stünde unter einem leeren Fach ein Satz ueber Arten, die es nicht gibt.
    const { container } = render(<CategoryRows categories={[]}
      busy={false} onSave={() => {}} onDelete={() => {}} />);

    // `jest-dom` ist in diesem Frontend nicht eingerichtet – geprüft wird mit den nackten Matchern.
    expect(container.textContent).toBe("");
  });
});

/*
 * Die zweite Hälfte von Entscheidung 2 (B-157): **Anlegen bleibt frei**, in jedem Fach-Zustand. Der
 * gefürchtete Fehlgriff wäre, das Formular mit in die Eigentums-Bedingung zu ziehen — dann friert die
 * Art-Achse aller Seed-Fächer ein, und das sind die einzigen, die ein normaler Nutzer hat.
 *
 * **Seit B-170 hält das die Signatur, nicht mehr diese Fälle.** `CategorySection` bekommt das Fach gar
 * nicht mehr, kann also nicht danach gaten. Vorher standen hier drei Fälle, einer je Fach-Zustand; ohne
 * das Prop wären sie drei identische Renderings — drei Kopien einer Zusicherung, von denen keine anders
 * fallen kann. Geblieben ist der eine Fall, der noch etwas trägt: das Formular ist überhaupt da und
 * reicht durch. Fällt das Prop je zurück, gehören die drei Zustände mit ihm zurück.
 */
const anlegenKnopf = () => screen.queryByRole("button", { name: "Neue Art anlegen" });

describe("CategorySection – das Anlege-Formular", () => {
  it('bietet „Neue Art" an und reicht die Eingabe durch', () => {
    const onCreate = vi.fn(async () => true);
    render(<CategorySection subjectId={3} error={null} pending={false} categories={[art()]} busy={false}
      onSave={() => {}} onDelete={() => {}} onCreate={onCreate} />);

    expect(screen.getByLabelText("Neue Art")).toBeTruthy();
    expect(anlegenKnopf()).not.toBeNull();

    fireEvent.change(screen.getByLabelText("Neue Art"), { target: { value: "Hörverstehen" } });
    fireEvent.submit(screen.getByLabelText("Neue Art").closest("form")!);

    expect(onCreate).toHaveBeenCalledWith("Hörverstehen");
  });

  it("zeigt fremde Arten weiter lesbar, nur ohne Knöpfe", () => {
    // Die Überschrift zählt die Arten – eine Seite, die „du kannst sie zum Filtern verwenden" sagt und
    // dann keine zeigt, hält ihr eigenes Versprechen nicht.
    render(<CategorySection subjectId={3} error={null} pending={false}
      categories={[art({ isOwn: false }), art({ id: 10, name: "Vokabeln", isOwn: false })]} busy={false}
      onSave={() => {}} onDelete={() => {}} onCreate={async () => true} />);

    expect(screen.getByText("Grammatik")).toBeTruthy();
    expect(screen.getByText("Vokabeln")).toBeTruthy();
    expect(screen.queryByRole("button", { name: /löschen$/ })).toBeNull();
    // Das Anlegen bleibt daneben bedienbar - der Kern von Entscheidung 2.
    expect(anlegenKnopf()).not.toBeNull();
  });

  /*
   * Aus dem Review zu B-170. `useAsync` behält seine `data` nicht nur über ein `reload`, sondern über
   * jeden Wechsel der Abhängigkeit — beim Sprung auf ein anderes Fach stehen also im Ladefenster noch die
   * Arten des vorigen da, während die Rückrufe in `CatalogAdmin` schon das NEUE Fach binden. Ein Klick
   * schickte `DELETE {neues Fach}/{Art aus dem alten}` und der Vater bekäme einen 404 auf eine Zeile, die
   * er vor sich sieht.
   *
   * Bis B-170 fing der frühe Ausstieg `if (!subject.isMine)` das ab; seit die Entscheidung pro Zeile
   * fällt, entscheidet veraltetes `isOwn`. Die Zeile bringt ihr eigenes Fach mit, also ist die Prüfung
   * struktureller Art und kein Zeitfenster-Trick: eine fremde Zeile ist **nicht renderbar**, nicht bloß
   * gesperrt. Rote Probe: den `filter` entfernen — dann fällt dieser Fall und nur er.
   */
  it("rendert keine Art, die zu einem anderen Fach gehört", () => {
    render(<CategorySection subjectId={3} error={null} pending={false}
      categories={[art({ id: 9, name: "Grammatik" }), art({ id: 10, name: "Aus-Fach-4", subjectId: 4 })]}
      busy={false} onSave={() => {}} onDelete={() => {}} onCreate={async () => true} />);

    // Beide Zeilen sind `isOwn`, rendern also als Feld – gezählt wird darum über die Felder und nicht
    // über den Text: ohne den Filter stünden hier zwei, und das eine trüge den Namen aus Fach 4.
    expect(screen.getAllByLabelText("Art")).toHaveLength(1);
    expect((screen.getByLabelText("Art") as HTMLInputElement).value).toBe("Grammatik");
    // Auch die Zählung in der Überschrift, sonst nennt sie im Ladefenster die Anzahl des vorigen Fachs.
    expect(screen.getByRole("heading", { name: "Arten (1)" })).toBeTruthy();
  });
});

/*
 * B-174. „Dieses Fach hat keine Arten" und „das Laden ist gescheitert" sahen identisch aus:
 * `categories.error` wurde in der ganzen Datei nirgends gerendert, und `CategoryRows` liefert bei
 * fehlenden Daten `null` – also einen leeren Bildschirm. Der Schaden ist nicht kosmetisch: Wer auf einer
 * stumm gebliebenen Liste eine Art neu anlegt, erzeugt eine Dublette im GETEILTEN Katalog, und die ist in
 * einem Grundbestands-Fach nach B-157 von niemandem mehr löschbar.
 *
 * Geprüft wird `CategorySection`, weil der Abschnitt in jedem Fach-Zustand rendert.
 *
 * **B-170 hat hier einen Fall zusammengelegt, und der Grund gehört dazu:** Vorher gab es „am eigenen
 * Fach" und „unter einem fremden Fach, wo sonst gar nichts stünde". Der zweite lebte davon, dass die
 * Fläche unter einem fremden Fach vollständig stumm war — diesen Zustand gibt es nicht mehr (entschieden
 * wird pro Art, und die Namen bleiben immer lesbar), und `CategorySection` bekommt das Fach ohnehin nicht
 * mehr. Zwei identische Renderings wären zwei Kopien einer Zusicherung, von denen keine anders fallen
 * kann; einer trägt, was noch zu tragen ist.
 */
describe("CategorySection – ein gescheitertes Laden sieht nicht aus wie ein leeres Fach", () => {
  const props = {
    subjectId: 3, pending: false, busy: false, onSave: () => {}, onDelete: () => {}, onCreate: async () => true,
  };

  it("zeigt den Fehler, statt wie ein leeres Fach auszusehen", () => {
    render(<CategorySection categories={null} error="Netz weg" {...props} />);
    expect(screen.getByText(/Netz weg/)).toBeTruthy();
  });

  it("schweigt, wenn wirklich nur nichts da ist", () => {
    render(<CategorySection categories={[]} error={null} {...props} />);
    // Auf das PRÄFIX des Banners prüfen, nicht auf den Fehlertext: „Netz weg" allein wäre schon dadurch
    // abwesend, dass hier kein Fehler übergeben wurde – die Zusicherung fiele also nie. Diese hier fällt,
    // sobald der Banner unbedingt rendert. (Eine frühere Fassung prüfte `role="alert"`, das es zu dem
    // Zeitpunkt nirgends gab: sie prüfte den Ausgangszustand und konnte nicht fallschlagen.)
    expect(screen.queryByText(/^Arten:/)).toBeNull();
  });
});

/*
 * B-234: der dritte Zustand, den B-174 in Entscheidung 3 versprochen hatte. „Keine Arten" steht nur nach
 * einer geglückten Ladung DIESES Fachs; davor steht „werden geladen", und neben einem Fehler keins von
 * beiden. Je Fall der Mutant, der ihn rot macht, steht am Fall.
 */
describe("CategorySection – geladen, noch ladend, leer (B-234)", () => {
  const props = { subjectId: 3, busy: false, onSave: () => {}, onDelete: () => {}, onCreate: async () => true };
  const laedt = () => screen.queryByText("Arten werden geladen…");
  const leer = () => screen.queryByText("Keine Arten.");

  it("sagt im Ladefenster eines Fachwechsels „werden geladen“ – nicht „keine“ und keine Zahl", () => {
    // Die Daten stammen noch vom VORIGEN Fach (4). Mutant „`pending` ignorieren": der Filter liefert dann
    // eine leere Liste, und es stünde „Arten (0)" samt „Keine Arten." da.
    render(<CategorySection categories={[art({ subjectId: 4 })]} error={null} pending {...props} />);
    expect(laedt()).not.toBeNull();
    expect(leer()).toBeNull();
    expect(screen.getByRole("heading", { name: "Arten" })).toBeTruthy();
  });

  it("sagt „Keine Arten.“ erst nach einer geglückten Ladung", () => {
    // Mutant „Leer-Zweig entfernen": vorher stand hier gar nichts (`CategoryRows` gibt `null`).
    render(<CategorySection categories={[]} error={null} pending={false} {...props} />);
    expect(leer()).not.toBeNull();
    expect(laedt()).toBeNull();
  });

  it("behauptet neben einem Fehler weder „lädt“ noch „keine“", () => {
    // Mutant „`!error &&` am Lade-Zweig weglassen": dann stünde „werden geladen" neben dem Banner.
    render(<CategorySection categories={null} error="Netz weg" pending {...props} />);
    expect(screen.getByText(/^Arten:/)).toBeTruthy();
    expect(laedt()).toBeNull();
    expect(leer()).toBeNull();
  });

  it("behauptet neben einem Fehler beim Nachladen kein leeres Fach", () => {
    // Geladen war leer, dann scheitert ein `reload`. Mutant „`!error &&` am Leer-Zweig weglassen".
    render(<CategorySection categories={[]} error="Netz weg" pending={false} {...props} />);
    expect(leer()).toBeNull();
  });
});

/*
 * Die Verdrahtung von B-234 (Review-Fund 🟡 1): `CategorySection` sieht nur das Ergebnis. Mutant
 * „`|| categories.stale` weglassen" macht den mittleren Fall rot – genau der Fachwechsel, in dem sonst „Arten (0)"
 * und „Keine Arten." stünden. Mutant „immer `false`" macht den ersten rot.
 */
describe("artenPending (B-234)", () => {
  it("ist wahr vor der ersten Ladung, wahr im Fachwechsel und falsch, sobald die Arten dieses Fachs da sind", () => {
    expect(artenPending({ data: null, stale: false })).toBe(true);
    expect(artenPending({ data: [art({ subjectId: 4 })], stale: true })).toBe(true);
    expect(artenPending({ data: [], stale: false })).toBe(false);
  });
});
