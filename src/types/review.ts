export type Language = 
  | "javascript" 
  | "typescript" 
  | "python" 
  | "csharp" 
  | "java";

export type ReviewStatus = "idle" | "pending" | "processing" | "done" | "error";

export interface ReviewRequest {
  code: string;
  language: Language;
}

export interface ReviewIssue {
  type: "bug" | "performance" | "style" | "security";
  severity: "high" | "medium" | "low";
  line?: number;
  message: string;
  suggestion: string;
}

export interface ReviewResult {
  id: string;
  status: ReviewStatus;
  issues: ReviewIssue[];
  summary: string;
  reviewedAt: string;
}