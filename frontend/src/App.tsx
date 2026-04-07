import { useState } from "react";
import CodeEditor from "./components/CodeEditor";
import ReviewPanel from "./components/ReviewPanel";
import LanguageSelector from "./components/LanguageSelector";
import { type Language, type ReviewResult } from "./types/review";

const PLACEHOLDER = `// Paste your code here
function calculateTotal(items) {
  let total = 0;
  for (let i = 0; i <= items.length; i++) {  // bug: off-by-one
    total += items[i].price;
  }
  return total;
}`;

export default function App() {
  const [code, setCode] = useState(PLACEHOLDER);
  const [language, setLanguage] = useState<Language>("javascript");
  const [result, setResult] = useState<ReviewResult | null>(null);
  const [isLoading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleReview() {
    setLoading(true);
    setError(null);

    // Day 4 — this will call the real C# backend
    // For now, we mock the response so the UI works end-to-end
    await new Promise((r) => setTimeout(r, 1500));
    setResult({
      id: "mock-001",
      status: "done",
      summary:
        "Found 3 issues in your code — 1 bug, 1 performance note, 1 style suggestion.",
      reviewedAt: new Date().toISOString(),
      issues: [
        {
          type: "bug",
          severity: "high",
          line: 4,
          message:
            "Off-by-one error: loop runs i <= items.length, accessing items[items.length] which is undefined.",
          suggestion: "Change condition to i < items.length",
        },
        {
          type: "performance",
          severity: "medium",
          message:
            "Array iteration using index-based loop is fine here, but Array.reduce() is more idiomatic and avoids mutation.",
          suggestion: "Use items.reduce((sum, item) => sum + item.price, 0)",
        },
        {
          type: "style",
          severity: "low",
          message: "Function lacks a type annotation for the items parameter.",
          suggestion: "Add JSDoc or convert to TypeScript with Item[] type",
        },
      ],
    });
    setLoading(false);
  }

  return (
    <div className="h-screen bg-gray-950 text-white flex flex-col">
      {/* Header */}
      <header className="flex items-center justify-between px-4 py-3 border-b border-gray-800 bg-gray-900">
        <div className="flex items-center gap-3">
          <span className="text-blue-400 font-semibold text-sm tracking-wide">
            AI Code Review
          </span>
          <LanguageSelector value={language} onChange={setLanguage} />
        </div>
        <button
          onClick={handleReview}
          disabled={isLoading || !code.trim()}
          className="px-4 py-1.5 text-sm bg-blue-600 hover:bg-blue-500 disabled:opacity-40 disabled:cursor-not-allowed rounded-md font-medium transition-colors"
        >
          {isLoading ? "Reviewing..." : "Review code"}
        </button>
      </header>

      {/* Main split layout */}
      <div className="flex flex-1 overflow-hidden">
        {/* Left — editor */}
        <div className="flex-1 border-r border-gray-800">
          <CodeEditor code={code} language={language} onChange={setCode} />
        </div>
        {/* Right — results */}
        <div className="w-96 bg-gray-900 flex flex-col">
          <div className="px-4 py-2 border-b border-gray-800 text-xs text-gray-500 uppercase tracking-wider">
            Review results
          </div>
          <ReviewPanel result={result} isLoading={isLoading} error={error} />
        </div>
      </div>
    </div>
  );
}
