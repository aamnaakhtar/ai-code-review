import { type Language } from "../types/review";

const LANGUAGES: { value: Language; label: string }[] = [
  { value: "javascript", label: "JavaScript" },
  { value: "typescript", label: "TypeScript" },
  { value: "python", label: "Python" },
  { value: "csharp", label: "C#" },
  { value: "java", label: "Java" },
];

interface Props {
  value: Language;
  onChange: (lang: Language) => void;
}

export default function LanguageSelector({ value, onChange }: Props) {
  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value as Language)}
      className="bg-gray-800 text-gray-200 text-sm px-3 py-1.5 rounded-md border border-gray-600 focus:outline-none focus:ring-2 focus:ring-blue-500"
    >
      {LANGUAGES.map((lang) => (
        <option key={lang.value} value={lang.value}>
          {lang.label}
        </option>
      ))}
    </select>
  );
}
