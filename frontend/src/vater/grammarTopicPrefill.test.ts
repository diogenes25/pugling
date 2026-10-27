import { describe, expect, it } from "vitest";
import { prefillGrammarTopics } from "./grammarTopicPrefill";

/*
 * B-155, AK 5: vorbelegen, nicht vererben. Die Fälle unterscheiden sich allein darin, ob die Unit gewechselt hat –
 * bei gleicher Unit zählt die Auswahl des Creators (auch eine Abwahl), bei neuer Unit deren Themen.
 */
describe("prefillGrammarTopics", () => {
  it("übernimmt beim Wechsel der Unit deren Themen und verwirft die alte Auswahl", () => {
    expect(prefillGrammarTopics({ unitId: 2, previousUnitId: 1, unitTopicIds: [7, 9, 7], current: [3] }))
      .toEqual([7, 9]);
  });

  it("lässt bei derselben Unit eine Abwahl stehen – nichts kommt zurück", () => {
    // Mutante „immer die Unit-Themen“: hier käme [7, 9] zurück, obwohl der Creator 9 abgewählt hat.
    expect(prefillGrammarTopics({ unitId: 2, previousUnitId: 2, unitTopicIds: [7, 9], current: [7] }))
      .toEqual([7]);
    expect(prefillGrammarTopics({ unitId: 2, previousUnitId: 2, unitTopicIds: [7, 9], current: [] }))
      .toEqual([]);
  });

  it("lässt bei derselben Unit eine Ergänzung stehen, die die Unit nicht trägt", () => {
    expect(prefillGrammarTopics({ unitId: 2, previousUnitId: 2, unitTopicIds: [7], current: [7, 11] }))
      .toEqual([7, 11]);
  });

  it("leert die Auswahl, wenn keine Unit mehr gewählt ist", () => {
    expect(prefillGrammarTopics({ unitId: null, previousUnitId: 2, unitTopicIds: [], current: [7] }))
      .toEqual([]);
  });

  it("belegt die erste Unit vor, wenn vorher keine gewählt war", () => {
    expect(prefillGrammarTopics({ unitId: 5, previousUnitId: null, unitTopicIds: [4], current: [] }))
      .toEqual([4]);
  });
});
