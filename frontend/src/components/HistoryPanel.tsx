import { type ReviewHistory } from "../types/review";

const LANGUAGE_COLORS: Record<string, string> = {
  javascript: "bg-yellow-500/20 text-yellow-300 border-yellow-500/30",
  typescript: "bg-blue-500/20 text-blue-300 border-blue-500/30",
  python: "bg-green-500/20 text-green-300 border-green-500/30",
  csharp: "bg-purple-500/20 text-purple-300 border-purple-500/30",
  java: "bg-orange-500/20 text-orange-300 border-orange-500/30",
};

function timeAgo(isoString: string): string {
  const diff = Date.now() - new Date(isoString).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return "just now";
  if (mins < 60) return `${mins}m ago`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours}h ago`;
  return `${Math.floor(hours / 24)}d ago`;
}

interface Props {
  history: ReviewHistory[];
  isLoading: boolean;
  onSelect: (jobId: string) => void;
  selectedJobId: string | null;
}

export default function HistoryPanel({
  history,
  isLoading,
  onSelect,
  selectedJobId,
}: Props) {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-32 text-gray-500 text-sm">
        Loading history...
      </div>
    );
  }

  if (history.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center h-32 gap-1 text-gray-600">
        <p className="text-sm">No reviews yet</p>
        <p className="text-xs">Your history will appear here</p>
      </div>
    );
  }

  return (
    <div className="overflow-y-auto flex-1">
      {history.map((item) => (
        <button
          key={item.jobId}
          onClick={() => onSelect(item.jobId)}
          className={`w-full text-left px-3 py-3 border-b border-gray-800 hover:bg-gray-800/50 transition-colors ${
            selectedJobId === item.jobId
              ? "bg-gray-800 border-l-2 border-l-blue-400"
              : ""
          }`}
        >
          <div className="flex items-center justify-between mb-1">
            <span
              className={`text-xs px-1.5 py-0.5 rounded border ${
                LANGUAGE_COLORS[item.language] ?? "bg-gray-700 text-gray-300"
              }`}
            >
              {item.language}
            </span>
            <span className="text-xs text-gray-500">
              {timeAgo(item.createdAt)}
            </span>
          </div>
          <p className="text-xs text-gray-400 truncate">{item.codePreview}</p>
          <p className="text-xs text-gray-500 mt-0.5">
            {item.totalIssues} issues found
          </p>
        </button>
      ))}
    </div>
  );
}
