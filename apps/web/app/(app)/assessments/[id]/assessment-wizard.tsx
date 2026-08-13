"use client";

import { useEffect, useMemo, useState, useTransition } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import { CONFIDENCE_LABELS, CONFIDENCE_VALUES } from "../values";
import type { ApiMaturityLevel } from "@/lib/api";

export const NOT_APPLICABLE_VALUE = "__na__";

export interface FlatQuestion {
  id: string;
  code: string;
  text: string;
  dimensionCode: string;
  dimensionName: string;
  fundamentalQuestion: string | null;
  capabilityCode: string;
  capabilityName: string;
  evidenceGuidance: string | null;
  response: {
    answerState: string;
    selectedMaturityLevelId: string | null;
    respondentComment: string | null;
    confidence: string | null;
    evidenceReferences: string[] | null;
  } | null;
}

interface AnswerDraft {
  selection: string | null; // maturity level id, or NOT_APPLICABLE_VALUE, or null if untouched
  respondentComment: string;
  confidence: string;
  evidenceReferences: string;
}

type SavePayload = {
  answerState: string;
  selectedMaturityLevelId: string | null;
  respondentComment: string;
  confidence?: string;
  evidenceReferences: string[];
};

function draftFrom(q: FlatQuestion): AnswerDraft {
  const r = q.response;
  return {
    selection: r?.answerState === "Answered" ? r.selectedMaturityLevelId : r?.answerState === "NotApplicable" ? NOT_APPLICABLE_VALUE : null,
    respondentComment: r?.respondentComment ?? "",
    confidence: r?.confidence ?? "",
    evidenceReferences: r?.evidenceReferences?.join(", ") ?? "",
  };
}

export function AssessmentWizard({
  questions,
  maturityLevels,
  saveAction,
  submitAction,
  readOnly,
}: {
  questions: FlatQuestion[];
  maturityLevels: ApiMaturityLevel[];
  saveAction: (questionId: string, payload: SavePayload) => Promise<{ error: string | null }>;
  submitAction: () => Promise<{ error: string | null }>;
  readOnly: boolean;
}) {
  const [drafts, setDrafts] = useState<Record<string, AnswerDraft>>(() =>
    Object.fromEntries(questions.map((q) => [q.id, draftFrom(q)]))
  );
  const [index, setIndex] = useState(() => {
    const firstUnanswered = questions.findIndex((q) => !drafts[q.id]?.selection);
    return firstUnanswered === -1 ? 0 : firstUnanswered;
  });
  const [detailOpen, setDetailOpen] = useState(false);
  const [prevIndex, setPrevIndex] = useState(index);
  const [error, setError] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();
  const [isSubmitting, startSubmitTransition] = useTransition();

  const total = questions.length;
  const answeredCount = useMemo(() => Object.values(drafts).filter((d) => d.selection).length, [drafts]);
  const complete = answeredCount === total;

  const question = questions[index];
  const draft = drafts[question.id];

  // Reset the detail section when the question changes -- adjusted during render (React's
  // recommended pattern for this) rather than in an effect, since a synchronous setState
  // inside an effect body triggers an extra, avoidable render pass.
  if (index !== prevIndex) {
    setPrevIndex(index);
    setDetailOpen(false);
  }

  useEffect(() => {
    if (readOnly) return;
    function onKeyDown(e: KeyboardEvent) {
      if (e.target instanceof HTMLTextAreaElement || e.target instanceof HTMLInputElement) return;
      if (e.key >= "1" && e.key <= "5") {
        const level = maturityLevels.find((l) => String(l.level) === e.key);
        if (level) selectValue(level.id);
      } else if (e.key === "0") {
        selectValue(NOT_APPLICABLE_VALUE);
      } else if (e.key === "ArrowRight") {
        setIndex((i) => Math.min(i + 1, total - 1));
      } else if (e.key === "ArrowLeft") {
        setIndex((i) => Math.max(i - 1, 0));
      }
    }
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [index, readOnly, maturityLevels]);

  function persist(questionId: string, next: AnswerDraft) {
    const answerState = next.selection === NOT_APPLICABLE_VALUE ? "NotApplicable" : next.selection ? "Answered" : "Unanswered";
    if (answerState === "Unanswered") return;

    startTransition(async () => {
      const result = await saveAction(questionId, {
        answerState,
        selectedMaturityLevelId: next.selection === NOT_APPLICABLE_VALUE ? null : next.selection,
        respondentComment: next.respondentComment,
        confidence: next.confidence || undefined,
        evidenceReferences: next.evidenceReferences.split(",").map((s) => s.trim()).filter(Boolean),
      });
      setError(result.error);
    });
  }

  function selectValue(value: string) {
    if (readOnly) return;
    const next: AnswerDraft = { ...draft, selection: value };
    setDrafts((prev) => ({ ...prev, [question.id]: next }));
    persist(question.id, next);
  }

  function updateDetail(patch: Partial<AnswerDraft>) {
    const next: AnswerDraft = { ...draft, ...patch };
    setDrafts((prev) => ({ ...prev, [question.id]: next }));
  }

  function commitDetail() {
    if (!readOnly && draft.selection) persist(question.id, draft);
  }

  function goTo(i: number) {
    setIndex(Math.max(0, Math.min(i, total - 1)));
  }

  function handleSubmit() {
    startSubmitTransition(async () => {
      const result = await submitAction();
      setSubmitError(result.error);
    });
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-col gap-2">
        <div className="flex items-center justify-between text-xs text-muted-foreground">
          <span className="tabular-nums">
            Question {index + 1} of {total}
          </span>
          <span className="tabular-nums">{answeredCount}/{total} answered</span>
        </div>
        <div className="h-1.5 w-full overflow-hidden rounded-full bg-muted">
          <div
            className="h-full rounded-full bg-primary transition-[width] duration-300 ease-out"
            style={{ width: `${(answeredCount / total) * 100}%` }}
          />
        </div>
        <div className="flex gap-1 overflow-x-auto pb-1">
          {questions.map((q, i) => {
            const state = drafts[q.id]?.selection;
            const tone =
              state === NOT_APPLICABLE_VALUE
                ? "bg-muted-foreground/40"
                : state
                  ? "bg-primary"
                  : "bg-muted border border-border";
            return (
              <button
                key={q.id}
                type="button"
                title={`${q.code}${state ? " — answered" : ""}`}
                onClick={() => goTo(i)}
                className={cn(
                  "h-2 w-4 shrink-0 rounded-full transition-transform",
                  tone,
                  i === index && "scale-y-[1.8] ring-2 ring-primary/40"
                )}
              />
            );
          })}
        </div>
      </div>

      <div className="flex flex-col gap-4 rounded-lg border bg-card p-6 shadow-sm">
        <div>
          <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
            {question.dimensionCode} · {question.dimensionName} — {question.capabilityCode} {question.capabilityName}
          </p>
          <p className="mt-2 text-lg font-medium leading-snug">{question.text}</p>
        </div>

        <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
          {maturityLevels.map((level) => {
            const selected = draft.selection === level.id;
            return (
              <button
                key={level.id}
                type="button"
                disabled={readOnly}
                onClick={() => selectValue(level.id)}
                className={cn(
                  "flex flex-col items-start gap-0.5 rounded-md border px-3 py-2.5 text-left transition-colors disabled:opacity-60",
                  selected ? "border-primary bg-primary/10" : "border-input hover:bg-muted"
                )}
              >
                <span className="text-xs text-muted-foreground">Level {level.level}</span>
                <span className="text-sm font-medium">{level.name}</span>
              </button>
            );
          })}
          <button
            type="button"
            disabled={readOnly}
            onClick={() => selectValue(NOT_APPLICABLE_VALUE)}
            className={cn(
              "flex flex-col items-start justify-center gap-0.5 rounded-md border px-3 py-2.5 text-left text-muted-foreground transition-colors disabled:opacity-60",
              draft.selection === NOT_APPLICABLE_VALUE ? "border-primary bg-primary/10" : "border-input hover:bg-muted"
            )}
          >
            <span className="text-sm font-medium">Not applicable</span>
          </button>
        </div>

        {!detailOpen ? (
          <button
            type="button"
            onClick={() => setDetailOpen(true)}
            className="self-start text-xs text-muted-foreground underline-offset-2 hover:underline"
          >
            + Add comment, confidence, or evidence
          </button>
        ) : (
          <div className="flex flex-col gap-2 border-t pt-3">
            {question.evidenceGuidance && (
              <p className="text-xs text-muted-foreground">Evidence examples: {question.evidenceGuidance}</p>
            )}
            <textarea
              placeholder="Comment (optional)"
              value={draft.respondentComment}
              onChange={(e) => updateDetail({ respondentComment: e.target.value })}
              onBlur={commitDetail}
              disabled={readOnly}
              rows={2}
              className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm disabled:opacity-60"
            />
            <div className="flex flex-wrap items-center gap-2">
              <select
                value={draft.confidence}
                onChange={(e) => {
                  updateDetail({ confidence: e.target.value });
                }}
                onBlur={commitDetail}
                disabled={readOnly}
                className="h-8 rounded-md border border-input bg-background px-2 text-sm disabled:opacity-60"
              >
                <option value="">Evidence confidence…</option>
                {CONFIDENCE_VALUES.map((c) => (
                  <option key={c} value={c}>{CONFIDENCE_LABELS[c]}</option>
                ))}
              </select>
              <Input
                placeholder="Evidence references (comma-separated)"
                value={draft.evidenceReferences}
                onChange={(e) => updateDetail({ evidenceReferences: e.target.value })}
                onBlur={commitDetail}
                disabled={readOnly}
                className="min-w-[200px] flex-1"
              />
            </div>
          </div>
        )}

        {error && <p className="text-xs text-destructive">{error}</p>}
      </div>

      <div className="flex items-center justify-between gap-3">
        <Button variant="outline" onClick={() => goTo(index - 1)} disabled={index === 0}>
          Back
        </Button>
        <span className="text-xs text-muted-foreground">
          {isPending ? "Saving…" : "1–5 to answer · 0 for N/A · ←/→ to move"}
        </span>
        {index < total - 1 ? (
          <Button onClick={() => goTo(index + 1)}>Next</Button>
        ) : (
          <span className="w-[70px]" />
        )}
      </div>

      {!readOnly && (
        <div className="flex flex-col gap-2 rounded-md border p-3">
          <p className="text-sm">
            {complete
              ? "All questions answered — ready to submit."
              : `${total - answeredCount} question(s) still need an answer (or Not Applicable) before this assessment can be submitted.`}
          </p>
          <div>
            <Button onClick={handleSubmit} disabled={!complete || isSubmitting}>
              {isSubmitting ? "Submitting…" : "Submit assessment"}
            </Button>
          </div>
          {submitError && <p className="text-sm text-destructive">{submitError}</p>}
        </div>
      )}
    </div>
  );
}
