import { useState } from "react";
import CodeEditor from "./components/CodeEditor";
import ReviewPanel from "./components/ReviewPanel";
import LanguageSelector from "./components/LanguageSelector";
import HistoryPanel from "./components/HistoryPanel";
import AuthForm from "./components/AuthForm";
import { useCodeReview } from "./hooks/useCodeReview";
import { useAuth } from "./context/AuthContext";
import { type Language } from "./types/review";

const PLACEHOLDER = "// Paste your code here\n";

export default function App() {
  const { user, logout } = useAuth();
  const [code, setCode] = useState(PLACEHOLDER);
  const [language, setLanguage] = useState<Language>("javascript");
  const [sidebarTab, setSidebarTab] = useState<"results" | "history">(
    "results",
  );

  const {
    result,
    isLoading,
    error,
    rateLimitSeconds,
    history,
    historyLoading,
    selectedJobId,
    handleReview,
    handleSelectHistory,
  } = useCodeReview();

  // Show login/register screen if not authenticated
  if (!user) return <AuthForm />;

  return (
    <div className="h-screen bg-gray-950 text-white flex flex-col">
      {/* Header */}
      <header className="flex items-center justify-between px-4 py-3 border-b border-gray-800 bg-gray-900 flex-shrink-0">
        <div className="flex items-center gap-3">
          <span className="text-blue-400 font-semibold text-sm tracking-wide">
            AI Code Review
          </span>
          <LanguageSelector value={language} onChange={setLanguage} />
        </div>

        <div className="flex items-center gap-3">
          {rateLimitSeconds > 0 && (
            <span className="text-xs text-yellow-400">
              Rate limited — wait {rateLimitSeconds}s
            </span>
          )}
          <span className="text-xs text-gray-500">{user.username}</span>
          <button
            onClick={logout}
            className="text-xs text-gray-500 hover:text-gray-300 transition-colors"
          >
            Sign out
          </button>
          <button
            onClick={() => handleReview(code, language)}
            disabled={isLoading || !code.trim() || rateLimitSeconds > 0}
            className="px-4 py-1.5 text-sm bg-blue-600 hover:bg-blue-500 disabled:opacity-40 disabled:cursor-not-allowed rounded-md font-medium transition-colors"
          >
            {isLoading ? "Reviewing..." : "Review code"}
          </button>
        </div>
      </header>

      {/* Main layout */}
      <div className="flex flex-1 overflow-hidden">
        {/* Left — editor */}
        <div className="flex-1 border-r border-gray-800 overflow-hidden">
          <CodeEditor code={code} language={language} onChange={setCode} />
        </div>

        {/* Right — sidebar */}
        <div className="w-96 bg-gray-900 flex flex-col">
          {/* Tabs */}
          <div className="flex border-b border-gray-800 flex-shrink-0">
            <button
              onClick={() => setSidebarTab("results")}
              className={`flex-1 py-2 text-xs font-medium uppercase tracking-wider transition-colors ${
                sidebarTab === "results"
                  ? "text-blue-400 border-b-2 border-blue-400"
                  : "text-gray-500 hover:text-gray-300"
              }`}
            >
              Review results
            </button>
            <button
              onClick={() => setSidebarTab("history")}
              className={`flex-1 py-2 text-xs font-medium uppercase tracking-wider transition-colors ${
                sidebarTab === "history"
                  ? "text-blue-400 border-b-2 border-blue-400"
                  : "text-gray-500 hover:text-gray-300"
              }`}
            >
              History {history.length > 0 && `(${history.length})`}
            </button>
          </div>

          {/* Tab content */}
          {sidebarTab === "results" ? (
            <ReviewPanel result={result} isLoading={isLoading} error={error} />
          ) : (
            <HistoryPanel
              history={history}
              isLoading={historyLoading}
              onSelect={(jobId) => {
                handleSelectHistory(jobId);
                setSidebarTab("results");
              }}
              selectedJobId={selectedJobId}
            />
          )}
        </div>
      </div>
    </div>
  );
}
