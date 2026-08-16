"use client";

import { useEffect, useMemo, useState, useTransition } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Progress } from "@/components/ui/progress";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { StatusBadge } from "@/components/ui/status-badge";
import { cn } from "@/lib/utils";
import { CONFIDENCE_LABELS, CONFIDENCE_VALUES } from "../values";
import type { ApiMaturityLevel } from "@/lib/api";

const NO_CONFIDENCE = "__none__";

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
    isCarriedForward: boolean;
    confirmedAtUtc: string | null;
    carriedForwardFrom: {
      selectedMaturityLevelId: string | null;
      respondentComment: string | null;
      confidence: string | null;
      evidenceReferences: string[] | null;
    } | null;
  } | null;
}

interface AnswerDraft {
  selection: string | null; // maturity level id, or NOT_APPLICABLE_VALUE, or null if untouched
  respondentComment: string;
  confidence: string;
  evidenceReferences: string;
  confirmed: boolean; // only meaningful when the question is carried forward
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
    confirmed: !r?.isCarriedForward || r.confirmedAtUtc !== null,
  };
}

function needsConfirmation(q: FlatQuestion, draft: AnswerDraft | undefined): boolean {
  return Boolean(q.response?.isCarriedForward) && !(draft?.confirmed ?? false);
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
  const unconfirmedCount = useMemo(
    () => questions.filter((q) => needsConfirmation(q, drafts[q.id])).length,
    [questions, drafts]
  );
  const complete = answeredCount === total && unconfirmedCount === 0;

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
      // Any successful save -- whether the value changed or was re-saved as-is --
      // confirms a carried-forward answer server-side (SaveResponseAsync sets
      // ConfirmedAtUtc unconditionally). Mirror that locally so the badge clears
      // without a full page reload.
      if (!result.error) {
        setDrafts((prev) => ({ ...prev, [questionId]: { ...prev[questionId], confirmed: true } }));
      }
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

  function levelName(levelId: string | null) {
    if (levelId === null) return "Not applicable";
    return maturityLevels.find((l) => l.id === levelId)?.name ?? "—";
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
        <div className="flex items-center justify-between text-sm">
          <span className="tabular-nums text-muted-foreground">
            Question {index + 1} of {total}
          </span>
          <span className="font-medium tabular-nums">{answeredCount}/{total} answered</span>
        </div>
        <Progress value={(answeredCount / total) * 100} />
        <div className="flex gap-1 overflow-x-auto pb-1 pt-1">
          {questions.map((q, i) => {
            const state = drafts[q.id]?.selection;
            const unconfirmed = needsConfirmation(q, drafts[q.id]);
            const tone = unconfirmed
              ? "bg-status-warning"
              : state === NOT_APPLICABLE_VALUE
                ? "bg-muted-foreground/40"
                : state
                  ? "bg-primary"
                  : "bg-muted border border-border";
            return (
              <button
                key={q.id}
                type="button"
                title={`${q.code}${unconfirmed ? " — carried forward, needs confirmation" : state ? " — answered" : ""}`}
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

        {needsConfirmation(question, draft) && question.response?.carriedForwardFrom && (
          <div className="flex flex-col gap-2 rounded-md border border-status-warning/40 bg-status-warning/10 p-3 text-sm">
            <div className="flex items-center justify-between gap-2">
              <StatusBadge tone="warning">Carried forward from prior assessment</StatusBadge>
              {!readOnly && (
                <button type="button" onClick={commitDetail} className="text-xs text-primary underline-offset-2 hover:underline">
                  Confirm as-is
                </button>
              )}
            </div>
            <p className="text-xs text-muted-foreground">
              Originally answered: {levelName(question.response.carriedForwardFrom.selectedMaturityLevelId)}
              {question.response.carriedForwardFrom.respondentComment && ` — "${question.response.carriedForwardFrom.respondentComment}"`}
            </p>
            {question.response.carriedForwardFrom.evidenceReferences && question.response.carriedForwardFrom.evidenceReferences.length > 0 && (
              <p className="text-xs text-muted-foreground">
                Inherited evidence: {question.response.carriedForwardFrom.evidenceReferences.join(", ")}
              </p>
            )}
            <p className="text-xs text-muted-foreground">Review the pre-filled answer below, then confirm or change it.</p>
          </div>
        )}

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
            <Textarea
              placeholder="Comment (optional)"
              value={draft.respondentComment}
              onChange={(e) => updateDetail({ respondentComment: e.target.value })}
              onBlur={commitDetail}
              disabled={readOnly}
              rows={2}
            />
            <div className="flex flex-wrap items-center gap-2">
              <Select
                value={draft.confidence || NO_CONFIDENCE}
                onValueChange={(value) => {
                  const next: AnswerDraft = { ...draft, confidence: value && value !== NO_CONFIDENCE ? value : "" };
                  setDrafts((prev) => ({ ...prev, [question.id]: next }));
                  if (!readOnly && next.selection) persist(question.id, next);
                }}
                disabled={readOnly}
              >
                <SelectTrigger className="h-8 w-full sm:w-56">
                  <SelectValue placeholder="Evidence confidence…" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={NO_CONFIDENCE}>Evidence confidence…</SelectItem>
                  {CONFIDENCE_VALUES.map((c) => (
                    <SelectItem key={c} value={c}>{CONFIDENCE_LABELS[c]}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
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
              : answeredCount < total
                ? `${total - answeredCount} question(s) still need an answer (or Not Applicable) before this assessment can be submitted.`
                : `${unconfirmedCount} answer(s) carried forward from the prior assessment still need to be confirmed or updated before this assessment can be submitted.`}
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
