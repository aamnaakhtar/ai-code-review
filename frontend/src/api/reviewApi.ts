import axios from "axios";
import { type ReviewResult } from "../types/review";

const API_BASE = "http://localhost:5249";

export async function submitReview(
  code: string,
  language: string
): Promise<string> {
  // Returns jobId immediately
  const response = await axios.post(`${API_BASE}/api/review`, {
    code,
    language,
  });
  return response.data.jobId;
}

export async function getReviewResult(
  jobId: string
): Promise<ReviewResult> {
  const response = await axios.get(`${API_BASE}/api/review/${jobId}`);
  return response.data;
}