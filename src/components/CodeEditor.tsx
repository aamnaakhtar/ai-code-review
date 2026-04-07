import Editor from "@monaco-editor/react";
import { type Language } from "../types/review";

interface Props {
  code: string;
  language: Language;
  onChange: (value: string) => void;
}

export default function CodeEditor({ code, language, onChange }: Props) {
  return (
    <Editor
      height="100%"
      language={language === "csharp" ? "csharp" : language}
      value={code}
      onChange={(val) => onChange(val ?? "")}
      theme="vs-dark"
      options={{
        fontSize: 14,
        minimap: { enabled: true },
        scrollBeyondLastLine: false,
        wordWrap: "on",
        lineNumbers: "on",
        renderLineHighlight: "line",
        automaticLayout: true,
      }}
    />
  );
}
