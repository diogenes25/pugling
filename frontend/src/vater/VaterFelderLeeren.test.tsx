import { act, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import type { SeriesUnitResponse, UpdateVocabularyDto, VocabularyResponse } from "../lib/types";
import { UnitForm } from "./VaterLehrwerke";
import { VocabRow } from "./VaterVocab";

/*
 * B-791: dieselbe Falle wie B-789 außerhalb des Shops. Ein geleertes Feld geht als `""` zum Server – der leert damit;
 * `null` hieße beim PATCH „unverändert", und „gespeichert" wäre gelogen. Dass `""` am Server leert, halten
 * `PatchClearFieldTests.B791_…`.
 */
afterEach(() => vi.restoreAllMocks());

describe("Geleerte Felder außerhalb des Shops (B-791)", () => {
  // AK 2. Rot mit `grammar: form.grammar.trim() || null` bzw. `vocabularyNotes: … || null` (je Feld ein Mutant).
  it("eine Unit schickt geleerte Grammatik und geleerten Wortschatz als leeren Text", async () => {
    const unit = {
      id: 11, seriesId: 7, label: "Unit 1", orderIndex: 0, grade: 6, bookType: "Textbook", topics: [],
      grammar: "simple past", vocabularyNotes: "Schule", createdAt: "2026-11-01T00:00:00Z", grammarTopicIds: [], exerciseCount: 0,
    } as SeriesUnitResponse;
    const update = vi.spyOn(api, "updateSeriesUnit").mockResolvedValue(unit as never);
    render(<UnitForm onMeldung={() => {}} seriesId={7} subjectId={null} unit={unit} onDone={() => {}} />);
    fireEvent.change(screen.getByLabelText("Grammatik der Unit"), { target: { value: "" } });
    fireEvent.change(screen.getByLabelText("Wortschatz der Unit"), { target: { value: " " } });
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "Speichern" })); });

    expect(update).toHaveBeenCalledTimes(1);
    const dto = update.mock.calls[0][2] as { grammar: string | null; vocabularyNotes: string | null };
    expect(dto.grammar).toBe("");
    expect(dto.vocabularyNotes).toBe("");
  });

  // B-800. Rot ohne `clearGrade` (Ausgangszustand): `grade: null` allein ließe den alten Band stehen.
  it("eine Unit schickt einen geleerten Band über den Schalter", async () => {
    const unit = {
      id: 11, seriesId: 7, label: "Unit 1", orderIndex: 0, grade: 6, bookType: "Textbook", topics: [],
      createdAt: "2026-11-01T00:00:00Z", grammarTopicIds: [], exerciseCount: 0,
    } as SeriesUnitResponse;
    const update = vi.spyOn(api, "updateSeriesUnit").mockResolvedValue(unit as never);
    render(<UnitForm onMeldung={() => {}} seriesId={7} subjectId={null} unit={unit} onDone={() => {}} />);
    fireEvent.change(screen.getByLabelText(/^Band/), { target: { value: "" } });
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "Speichern" })); });

    expect(update).toHaveBeenCalledTimes(1);
    expect(update.mock.calls[0][2]).toMatchObject({ grade: null, clearGrade: true });
  });

  // AK 3. Rot mit `baseFormRelation: … || null` bzw. `pronunciationAudioUrl: … || null` (je Feld ein Mutant).
  it("eine Vokabel schickt geleerte Relation und geleerte Audio-URL als leeren Text", async () => {
    const v = {
      id: 4, key: "en_song", version: "1.0", sourceLanguage: "en", targetLanguage: "de", word: "sung", translation: "gesungen",
      translationAlternatives: null, partOfSpeech: "Verb", noun: null, verb: { isBaseForm: false }, baseFormKey: null,
      baseFormRelation: "Partizip", pronunciationAudioUrl: "https://example.org/sung.mp3", tags: [],
    } as unknown as VocabularyResponse;
    const update = vi.spyOn(api, "updateVocabulary").mockResolvedValue(v as never);
    render(<table><tbody><VocabRow v={v} onChanged={() => {}} childId={null} globalTags={[]} reloadGlobalTags={() => {}}
      childTagOptions={[]} reloadChildTags={() => {}} /></tbody></table>);
    fireEvent.click(screen.getByRole("button", { name: "Bearbeiten" }));
    fireEvent.change(screen.getByLabelText("Grundform-Relation"), { target: { value: "" } });
    fireEvent.change(screen.getByLabelText("Aussprache-Audio-URL"), { target: { value: "  " } });
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "OK" })); });

    expect(update).toHaveBeenCalledTimes(1);
    const patch = update.mock.calls[0][1] as UpdateVocabularyDto;
    expect(patch.baseFormRelation).toBe("");
    expect(patch.pronunciationAudioUrl).toBe("");
  });
});
