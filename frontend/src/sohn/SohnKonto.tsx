import { Link } from "react-router-dom";
import { Trans, useTranslation } from "react-i18next";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import type { Paged, WalletBalance, WalletEntry } from "../lib/types";

export function SohnKonto() {
  const { t } = useTranslation();
  const wallet = useAsync<WalletBalance>(() => api.wallet(), []);
  const history = useAsync<Paged<WalletEntry>>(() => api.walletEntries(), []);

  const coins = wallet.data?.coins ?? 0;
  const gems = wallet.data?.gems ?? 0;

  return (
    <div className="sohn-body">
      <div className="row">
        <span className="screen-title" style={{ margin: 0 }}>{t("account.title")}</span>
        <span className="chip" style={{ marginLeft: "auto" }}>🪙<b className="tabnum">{coins}</b></span>
        <span className="chip">💎<b className="tabnum">{gems}</b></span>
      </div>
      <p className="sub"><Trans i18nKey="account.intro" components={{ b: <b /> }} /></p>

      {/* Der dauerhafte Weg zur Selbstbeschreibung (B-50). Er liegt hier und nicht in der Arcade-Leiste:
          die trägt die fünf Orte, an denen gespielt und ausgegeben wird – dies ist eine Einstellung. */}
      <Link to="/sohn/ich" className="btn ghost" style={{ display: "block", textDecoration: "none", marginBottom: 10 }}>{t("account.aboutMe")}</Link>

      <h3 className="screen-title" style={{ fontSize: 18 }}>{t("account.history")}</h3>
      {history.loading ? <div className="loading">{t("home.loading")}</div> : history.error ? <div className="banner err">{history.error}</div> : (
        <div className="list">
          {history.data?.items.map((e) => (
            <div key={e.id} className="row" style={{ justifyContent: "space-between", padding: "5px 0" }}>
              {/* Die Buchungsart ist Chrome (Server-Enum), der Grund dahinter Nutzerdatum. Nicht `pointKindLabel`:
                  der Helfer bleibt fürs Vater-Web deutsch (B-304). Eine unbekannte Art zeigt ihren Rohwert. */}
              <span className="sub">{t(`pointKind.${e.kind}`, { defaultValue: e.kind })}{e.reason ? ` · ${e.reason}` : ""}</span>
              <b className="tabnum" style={{ color: e.amount < 0 ? "var(--danger,#ff6b6b)" : "var(--lime,#8bd450)" }}>
                {e.amount > 0 ? `+${e.amount}` : e.amount}</b>
            </div>
          ))}
          {history.data?.items.length === 0 && <p className="sub">{t("account.noEntries")}</p>}
        </div>
      )}
    </div>
  );
}
