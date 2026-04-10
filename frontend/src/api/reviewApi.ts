import axios from "axios";
import { type ReviewResult } from "../types/review";

const API_BASE = "http://localhost:5249";

export async function submitReview(
  code: string,
  language: string
): Promise<ReviewResult> {
  const response = await axios.post(`${API_BASE}/api/review`, {
    code,
    language,
  });
  return response.data;
}