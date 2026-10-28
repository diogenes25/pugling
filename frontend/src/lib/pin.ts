/**
 * Die Mindestlänge einer PIN, wie der Server sie prüft (`PinHasher.IsAcceptable`, B-28): leer heißt „Login
 * deaktiviert" bzw. „unverändert" und ist erlaubt, sonst mindestens vier Zeichen.
 *
 * Die Oberfläche prüft sie selbst vor dem Absenden (B-281): der Server antwortet mit `400 validation_error`, und dieser
 * Code steht für zu viele Prüfungen, um eine deutsche Fassung zu tragen – der Vater las darum einen englischen Satz.
 * Der Server bleibt die Quelle der Wahrheit; das hier ist nur die Meldung in seiner Sprache.
 */
export const PIN_MIN_LENGTH = 4;

/** Die Meldung zu einer zu kurzen PIN, sonst `null`. Geprüft wird der Wert, der wirklich gesendet wird. */
export function pinProblem(pin: string): string | null {
  return pin.length > 0 && pin.length < PIN_MIN_LENGTH
    ? `Die PIN braucht mindestens ${PIN_MIN_LENGTH} Zeichen.`
    : null;
}
