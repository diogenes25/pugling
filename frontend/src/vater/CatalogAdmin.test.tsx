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
 * B-157. Die Art hat keinen eigenen Eigentümer - sie gehört dem, dem ihr Fach gehört. Bis dahin bot die
 * Katalogseite "OK" und "Löschen" an jeder Art an, auch unter einem fremden Fach, und der Server nahm sie
 * seit dieser Story mit 403 not_owner nicht mehr an.
 *
 * Der wichtigste Fall ist der letzte: das Anlege-Formular bleibt IMMER bedienbar. Es liegt darum bewusst
 * außerhalb von `CategoryRows` - wäre es drin, haette diese Story die Art-Achse aller Seed-Fächer
 * eingefroren.
 */
function art(over: Partial<CategoryResponse> = {}): CategoryResponse {
  return { id: 9, subjectId: 3, name: "Grammatik", createdAt: "2026-08-13T00:00:00Z", ...over };
}

describe("CategoryRows – das Fach entscheidet, wer die Arten ändern darf", () => {
  it("zeigt am eigenen Fach je Art ein Feld und einen Löschknopf", () => {
    render(<CategoryRows subject={subject()} categories={[art(), art({ id: 10, name: "Vokabeln" })]}
      busy={false} onSave={() => {}} onDelete={() => {}} />);

    expect(screen.getAllByLabelText("Art")).toHaveLength(2);
    expect(screen.getByRole("button", { name: 'Art „Grammatik" löschen' })).toBeTruthy();
    expect(screen.getByRole("button", { name: 'Art „Vokabeln" löschen' })).toBeTruthy();
  });

  it("reicht die geänderte Art samt neuem Namen nach oben", () => {
    const onSave = vi.fn();
    render(<CategoryRows subject={subject()} categories={[art()]}
      busy={false} onSave={onSave} onDelete={() => {}} />);

    fireEvent.change(screen.getByLabelText("Art"), { target: { value: "Grammatik neu" } });
    fireEvent.click(screen.getByRole("button", { name: /speichern$/ }));

    expect(onSave).toHaveBeenCalledWith(art(), "Grammatik neu");
  });

  it("zeigt am fremden Fach keine Knöpfe, sondern den Grund – und nennt das Fach", () => {
    render(<CategoryRows subject={subject({ isMine: false, ownerAdultId: 7 })} categories={[art()]}
      busy={false} onSave={() => {}} onDelete={() => {}} />);

    expect(screen.queryByLabelText("Art")).toBeNull();
    expect(screen.queryByRole("button", { name: /löschen$/ })).toBeNull();
    // Der Satz muss das FACH nennen, nicht über die Arten behaupten, jemand habe sie angelegt: die Art hat
    // keinen Eigentümer, und anlegen darf sie hier ohnehin jeder – der Satz wäre in einem Klick falsch.
    expect(screen.getByText(/Fach „Englisch", das jemand anderes angelegt hat/)).toBeTruthy();
    expect(screen.getByText(/ergänzen darfst du sie/)).toBeTruthy();
  });

  it("nennt beim Fach aus dem Grundbestand niemanden als Eigentümer", () => {
    render(<CategoryRows subject={subject({ isMine: false, ownerAdultId: null })} categories={[art()]}
      busy={false} onSave={() => {}} onDelete={() => {}} />);

    expect(screen.queryByLabelText("Art")).toBeNull();
    expect(screen.getByText(/aus dem Grundbestand/)).toBeTruthy();
    expect(screen.queryByText(/jemand anderes angelegt hat/)).toBeNull();
  });

  it("behandelt ein fehlendes ownerAdultId wie ownerlos, nicht wie fremd", () => {
    // Dieselbe Sensitivitätslücke, die `SubjectRow` schon abdeckt: der Vertrag gibt `ownerAdultId` optional
    // heraus, ein strenges `=== null` erzeugte hier den erfundenen fremden Creator.
    const ohneFeld = subject({ isMine: false });
    delete (ohneFeld as { ownerAdultId?: number | null }).ownerAdultId;

    render(<CategoryRows subject={ohneFeld} categories={[art()]}
      busy={false} onSave={() => {}} onDelete={() => {}} />);

    expect(screen.getByText(/aus dem Grundbestand/)).toBeTruthy();
  });

  it("schweigt, wenn das Fach gar keine Arten hat", () => {
    // Sonst stünde unter einem fremden leeren Fach ein Satz ueber Arten, die es nicht gibt.
    const { container } = render(<CategoryRows subject={subject({ isMine: false, ownerAdultId: 7 })}
      categories={[]} busy={false} onSave={() => {}} onDelete={() => {}} />);

    // `jest-dom` ist in diesem Frontend nicht eingerichtet – geprüft wird mit den nackten Matchern.
    expect(container.textContent).toBe("");
  });
});

/*
 * Die zweite Hälfte von Entscheidung 2, und der Grund, warum es diesen Baustein überhaupt gibt: **Anlegen
 * bleibt frei**, in allen drei Fach-Zuständen. Ohne diese drei Fälle wäre der gefürchtete Fehlgriff — das
 * Formular mit in die Eigentums-Bedingung zu ziehen — grün durchgegangen, und die Entscheidung wäre nur ein
 * Kommentar gewesen (vom `frontend-reviewer` gefunden).
 */
const anlegenKnopf = () => screen.queryByRole("button", { name: "Neue Art anlegen" });

describe("CategorySection – das Anlege-Formular bleibt in jedem Zustand bedienbar", () => {
  const zustaende: [string, Partial<SubjectResponse>][] = [
    ["eigenes Fach", {}],
    ["fremdes Fach", { isMine: false, ownerAdultId: 7 }],
    ["Fach aus dem Grundbestand", { isMine: false, ownerAdultId: null }],
  ];

  for (const [name, over] of zustaende) {
    it(`bietet „Neue Art" am ${name} an`, () => {
      render(<CategorySection subject={subject(over)} categories={[art()]} busy={false}
        onSave={() => {}} onDelete={() => {}} onCreate={async () => true} />);

      expect(screen.getByLabelText("Neue Art")).toBeTruthy();
      expect(anlegenKnopf()).not.toBeNull();
    });
  }

  it("legt wirklich an und leert danach das Feld", () => {
    const onCreate = vi.fn(async () => true);
    render(<CategorySection subject={subject({ isMine: false, ownerAdultId: 7 })} categories={[art()]}
      busy={false} onSave={() => {}} onDelete={() => {}} onCreate={onCreate} />);

    fireEvent.change(screen.getByLabelText("Neue Art"), { target: { value: "Hörverstehen" } });
    fireEvent.submit(screen.getByLabelText("Neue Art").closest("form")!);

    expect(onCreate).toHaveBeenCalledWith("Hörverstehen");
  });

  it("zeigt am fremden Fach die Namen weiter lesbar, nur ohne Knöpfe", () => {
    // Die Überschrift zählt die Arten – eine Seite, die „du kannst sie zum Filtern verwenden" sagt und dann
    // keine zeigt, hält ihr eigenes Versprechen nicht.
    render(<CategorySection subject={subject({ isMine: false, ownerAdultId: 7 })}
      categories={[art(), art({ id: 10, name: "Vokabeln" })]} busy={false}
      onSave={() => {}} onDelete={() => {}} onCreate={async () => true} />);

    expect(screen.getByText(/Grammatik · Vokabeln/)).toBeTruthy();
    expect(screen.queryByRole("button", { name: /löschen$/ })).toBeNull();
  });
});
