import { type ReviewResult, type ReviewIssue } from "../types/review";

const SEVERITY_STYLES = {
  high: "bg-red-900/40 border-red-500 text-red-300",
  medium: "bg-yellow-900/40 border-yellow-500 text-yellow-300",
  low: "bg-blue-900/40 border-blue-500 text-blue-300",
};

const TYPE_LABELS = {
  bug: "Bug",
  performance: "Perf",
  style: "Style",
  security: "Security",
};

function IssueCard({ issue }: { issue: ReviewIssue }) {
  return (
    <div
      className={`border-l-2 rounded-r-md p-3 mb-2 ${SEVERITY_STYLES[issue.severity]}`}
    >
      <div className="flex items-center gap-2 mb-1">
        <span className="text-xs font-semibold uppercase tracking-wide opacity-80">
          {TYPE_LABELS[issue.type]}
        </span>
        <span className="text-xs opacity-60">
          {issue.line ? `Line ${issue.line}` : "General"}
        </span>
      </div>
      <p className="text-sm mb-1">{issue.message}</p>
      <p className="text-xs opacity-70 italic">{issue.suggestion}</p>
    </div>
  );
}

interface Props {
  result: ReviewResult | null;
  isLoading: boolean;
  error: string | null;
}

export default function ReviewPanel({ result, isLoading, error }: Props) {
  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center h-full gap-3 text-gray-400">
        <div className="w-6 h-6 border-2 border-blue-400 border-t-transparent rounded-full animate-spin" />
        <p className="text-sm">Analyzing your code...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-4 text-red-400 text-sm">
        Something went wrong: {error}
      </div>
    );
  }

  if (!result) {
    return (
      <div className="flex flex-col items-center justify-center h-full gap-2 text-gray-500">
        <p className="text-sm">Paste code and click Review</p>
        <p className="text-xs opacity-60">
          Bugs, performance, and style issues will appear here
        </p>
      </div>
    );
  }

  const counts = {
    high: result.issues.filter((i) => i.severity === "high").length,
    medium: result.issues.filter((i) => i.severity === "medium").length,
    low: result.issues.filter((i) => i.severity === "low").length,
  };

  return (
    <div className="p-4 overflow-y-auto h-full">
      <p className="text-gray-300 text-sm mb-3">{result.summary}</p>
      <div className="flex gap-3 mb-4 text-xs">
        <span className="text-red-400">{counts.high} high</span>
        <span className="text-yellow-400">{counts.medium} medium</span>
        <span className="text-blue-400">{counts.low} low</span>
      </div>
      {result.issues.map((issue, i) => (
        <IssueCard key={i} issue={issue} />
      ))}
    </div>
  );
}
