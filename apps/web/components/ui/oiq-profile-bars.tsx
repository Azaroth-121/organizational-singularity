import { StatusBadge } from "@/components/ui/status-badge";
import type { StatusTone } from "@/components/ui/status-badge";

export interface OiqProfileRow {
  key: string;
  code: string;
  name: string;
  score: number | null;
  band: string | null;
}

/** 11 dimensions is a magnitude job, not an identity job -- one hue at varying bar
 * length (not 11 categorical colors, which the palette can't support past ~8
 * series safely) is the correct encoding here. Band names still carry their own
 * status tone via the badge, kept separate from the bar's own color. */
export function OiqProfileBars({ rows, bandTone }: { rows: OiqProfileRow[]; bandTone: Record<string, StatusTone> }) {
  return (
    <div className="flex flex-col gap-2">
      {rows.map((row) => {
        const pct = row.score === null ? 0 : Math.max(0, Math.min(100, ((row.score - 1) / 4) * 100));
        return (
          <div key={row.key} className="flex items-center gap-3">
            <div className="w-40 shrink-0 sm:w-56">
              <p className="truncate text-sm">
                <span className="mr-1.5 font-mono text-xs text-muted-foreground">{row.code}</span>
                {row.name}
              </p>
            </div>
            <div className="h-2 flex-1 overflow-hidden rounded-full bg-muted">
              <div className="h-full rounded-full bg-primary transition-[width]" style={{ width: `${pct}%` }} />
            </div>
            <span className="w-10 shrink-0 text-right text-sm tabular-nums text-muted-foreground">
              {row.score === null ? "—" : row.score.toFixed(1)}
            </span>
            <div className="w-24 shrink-0">
              {row.band && (
                <StatusBadge tone={bandTone[row.band] ?? "neutral"} showIcon={false}>
                  {row.band}
                </StatusBadge>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}
