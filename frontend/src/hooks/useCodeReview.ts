import { useState, useEffect, useCallback } from "react";
import { type Language, type ReviewResult, type ReviewHistory } from "../types/review";
import { submitReview, getReviewResult, getReviewHistory, getReviewById } from "../api/reviewApi";
import axios from "axios";

export function useCodeReview() {
  const [result, setResult]       = useState<ReviewResult | null>(null);
  const [isLoading, setLoading]   = useState(false);
  const [error, setError]         = useState<string | null>(null);
  const [rateLimitSeconds, setRateLimitSeconds] = useState(0);
  const [history, setHistory]     = useState<ReviewHistory[]>([]);
  const [historyLoading, setHistoryLoading] = useState(true);
  const [selectedJobId, setSelectedJobId]   = useState<string | null>(null);

  // Load history on mount
  useEffect(() => {
    loadHistory();
  }, []);

  // Rate limit countdown
  useEffect(() => {
    if (rateLimitSeconds <= 0) return;
    const timer = setTimeout(
      () => setRateLimitSeconds((s) => s - 1),
      1000
    );
    return () => clearTimeout(timer);
  }, [rateLimitSeconds]);

  const loadHistory = async () => {
    try {
      setHistoryLoading(true);
      const data = await getReviewHistory();
      setHistory(data);
    } catch {
      // History failing shouldn't block the main UI
    } finally {
      setHistoryLoading(false);
    }
  };

  const handleReview = useCallback(async (code: string, language: Language) => {
    setLoading(true);
    setError(null);
    setResult(null);
    setSelectedJobId(null);

    try {
      const jobId = await submitReview(code, language);

      const poll = async (): Promise<void> => {
        const data = await getReviewResult(jobId);

        if (data.status === "done" || data.status === "error") {
          setResult(data);
          setLoading(false);
          // Refresh history after new review completes
          loadHistory();
          return;
        }

        setTimeout(poll, 2000);
      };

      await poll();
    } catch (err) {
      setLoading(false);

      // Handle rate limit specifically
      if (axios.isAxiosError(err) && err.response?.status === 429) {
        setRateLimitSeconds(60);
        setError("Rate limit reached — you can submit again in 60 seconds.");
        return;
      }

      setError("Review failed. Is the backend running?");
    }
  }, []);

  const handleSelectHistory = useCallback(async (jobId: string) => {
    setSelectedJobId(jobId);
    setLoading(true);
    setError(null);

    try {
      const data = await getReviewById(jobId);
      setResult(data);
    } catch {
      setError("Could not load this review.");
    } finally {
      setLoading(false);
    }
  }, []);

  return {
    result,
    isLoading,
    error,
    rateLimitSeconds,
    history,
    historyLoading,
    selectedJobId,
    handleReview,
    handleSelectHistory,
  };
}