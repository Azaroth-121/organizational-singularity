import Link from "next/link";
import type { LucideIcon } from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import type { StatusTone } from "@/components/ui/status-badge";
import { cn } from "@/lib/utils";

const TONE_CHIP: Record<StatusTone, string> = {
  good: "bg-status-good/10 text-status-good",
  warning: "bg-status-warning/15 text-amber-700 dark:text-status-warning",
  serious: "bg-status-serious/15 text-orange-700 dark:text-status-serious",
  critical: "bg-status-critical/10 text-status-critical",
  neutral: "bg-primary/10 text-primary",
};

/** The dashboard/register "headline number" pattern -- icon chip + tabular value +
 * label, optionally status-toned when the number itself is a severity/urgency
 * signal (e.g. an open-critical-findings count). */
export function StatTile({
  label,
  value,
  icon: Icon,
  tone = "neutral",
  href,
}: {
  label: string;
  value: React.ReactNode;
  icon?: LucideIcon;
  tone?: StatusTone;
  href?: string;
}) {
  const content = (
    <CardContent className="flex items-center gap-3 py-4">
      {Icon && (
        <span className={cn("flex size-9 shrink-0 items-center justify-center rounded-lg", TONE_CHIP[tone])}>
          <Icon className="size-4" />
        </span>
      )}
      <div className="min-w-0">
        <p className="text-3xl leading-none font-semibold tabular-nums">{value}</p>
        <p className="mt-1.5 truncate text-xs text-muted-foreground">{label}</p>
      </div>
    </CardContent>
  );

  if (href) {
    return (
      <Card className="transition-colors hover:bg-muted/40">
        <Link href={href}>{content}</Link>
      </Card>
    );
  }
  return <Card>{content}</Card>;
}
