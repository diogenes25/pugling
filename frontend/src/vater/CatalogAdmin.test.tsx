import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { CategoryRows, CategorySection, NameRow, SubjectRow } from "./CatalogAdmin";
import type { CategoryResponse, SubjectResponse } from "../lib/types";

/*
 * Der Regressionstest zu B-154. Geprüft wird `SubjectRow` einzeln, nicht die ganze `CatalogAdmin`: die
 * lädt beim Fachwechsel die Arten nach und hinge damit am Netz – Bausteine hier, Wege durch die App bei
 * Playwright (frontend/CLAUDE.md). Kein Test dieses Frontends mockt `../lib/api`, und das bleibt so.
 *
 * Der Fehler, den er festhält: Seit B-13 verweigert der Server `PATCH`/`DELETE` an einem fremden **und**
 * an einem ownerlosen Fach mit `403 not_owner`. Die Zeile bot „OK" und „Löschen" trotzdem an jedem Fach
 * an, und der Löschdialog zählte vorher noch auf, was alles seine Zuordnung verliert. Die Antwort trug
 * `isMine` von Anfang an – gelesen wurde es nicht.
 *
 * Warum die zwei Nicht-Eigentümer-Fälle getrennt geprüft werden: „hat jemand anderes angelegt" ist bei
 * einem Fach aus dem Grundbestand nicht bloß unschön, sondern falsch – es gehört niemandem.
 */

function subject(over: Partial<SubjectResponse> = {}): SubjectResponse {
  return {
    id: 3, name: "Englisch", createdAt: "2026-08-12T00:00:00Z", categoriesCount: 2,
    ownerAdultId: 1, isMine: true, ...over,
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
    render(<SubjectRow subject={subject({ isMine: false, ownerAdultId: 7 })}
      busy={false} onSave={() => {}} onDelete={() => {}} />);

    expect(screen.queryByLabelText("Fach umbenennen")).toBeNull();
    expect(okKnopf()).toBeNull();
    expect(loeschKnopf()).toBeNull();
    expect(screen.getByText(/hat jemand anderes angelegt/)).toBeTruthy();
  });

  it("nennt beim Fach aus dem Grundbestand niemanden als Eigentümer", () => {
    render(<SubjectRow subject={subject({ isMine: false, ownerAdultId: null })}
      busy={false} onSave={() => {}} onDelete={() => {}} />);

    expect(loeschKnopf()).toBeNull();
    expect(screen.getByText(/gehört zum Grundbestand/)).toBeTruthy();
    // Der falsche Satz darf nicht bloß fehlen, er darf hier gar nicht entstehen.
    expect(screen.queryByText(/hat jemand anderes angelegt/)).toBeNull();
  });

  it("behandelt ein fehlendes ownerAdultId wie ownerlos, nicht wie fremd", () => {
    // Der Vertrag gibt `ownerAdultId` optional heraus (`ownerAdultId?: number | null`) – ein strenges
    // `=== null` hätte hier den Satz über den fremden Creator erzeugt, und der wäre erfunden.
    const ohneFeld = subject({ isMine: false });
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
 * Was der Block jetzt prüft, ist darum die BINDUNG an `isOwn`, nicht mehr an `subject.isMine`. Der
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
    render(<CategorySection error={null} categories={[art()]} busy={false}
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
    render(<CategorySection error={null}
      categories={[art({ isOwn: false }), art({ id: 10, name: "Vokabeln", isOwn: false })]} busy={false}
      onSave={() => {}} onDelete={() => {}} onCreate={async () => true} />);

    expect(screen.getByText("Grammatik")).toBeTruthy();
    expect(screen.getByText("Vokabeln")).toBeTruthy();
    expect(screen.queryByRole("button", { name: /löschen$/ })).toBeNull();
    // Das Anlegen bleibt daneben bedienbar - der Kern von Entscheidung 2.
    expect(anlegenKnopf()).not.toBeNull();
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
    busy: false, onSave: () => {}, onDelete: () => {}, onCreate: async () => true,
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
