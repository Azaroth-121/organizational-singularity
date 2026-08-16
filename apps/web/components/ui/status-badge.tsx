import { AlertCircle, AlertTriangle, CheckCircle2, Circle, OctagonAlert } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

export type StatusTone = "good" | "warning" | "serious" | "critical" | "neutral";

const TONE_CLASSES: Record<StatusTone, string> = {
  good: "bg-status-good/10 text-status-good dark:bg-status-good/15",
  warning: "bg-status-warning/15 text-amber-800 dark:bg-status-warning/20 dark:text-status-warning",
  serious: "bg-status-serious/15 text-orange-800 dark:bg-status-serious/20 dark:text-status-serious",
  critical: "bg-status-critical/10 text-status-critical dark:bg-status-critical/20",
  neutral: "bg-muted text-muted-foreground",
};

const TONE_ICONS: Record<StatusTone, React.ComponentType<{ className?: string }>> = {
  good: CheckCircle2,
  warning: AlertTriangle,
  serious: AlertCircle,
  critical: OctagonAlert,
  neutral: Circle,
};

/** A status color is never carried by hue alone -- always paired with its label
 * (and, unless suppressed, an icon). Tones map to the four reserved status
 * tokens in globals.css; "neutral" is the non-signal case (informational-only). */
export function StatusBadge({
  tone,
  children,
  showIcon = true,
  className,
}: {
  tone: StatusTone;
  children: React.ReactNode;
  showIcon?: boolean;
  className?: string;
}) {
  const Icon = TONE_ICONS[tone];
  return (
    <Badge variant="outline" className={cn("border-transparent", TONE_CLASSES[tone], className)}>
      {showIcon && <Icon className="size-3" />}
      {children}
    </Badge>
  );
}
