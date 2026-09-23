import { Dropdown, Input, Label, Option, Textarea } from "@fluentui/react-components";

export function Field({
  label,
  value,
  onChange,
  name,
  type = "text",
  required,
  hint,
  disabled
}: {
  label: string;
  value?: string;
  onChange?: (value: string) => void;
  name?: string;
  type?: "text" | "email" | "password" | "number" | "url";
  required?: boolean;
  hint?: string;
  disabled?: boolean;
}) {
  return (
    <label className="dp-field">
      <span>{label}</span>
      <Input
        name={name}
        type={type}
        required={required}
        disabled={disabled}
        value={value}
        minLength={type === "password" ? 8 : undefined}
        autoComplete={type === "password" ? "current-password" : type === "email" ? "email" : undefined}
        onChange={onChange ? (_, data) => onChange(data.value) : undefined}
      />
      {hint ? <small style={{ color: "var(--muted)" }}>{hint}</small> : null}
    </label>
  );
}

export function SelectField({
  label,
  value,
  onChange,
  options
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: { value: string; label: string }[];
}) {
  const selected = options.find((option) => option.value === value);
  return (
    <label className="dp-field">
      <span>{label}</span>
      <Dropdown
        style={{ minWidth: "100%" }}
        value={selected?.label ?? "Choose"}
        selectedOptions={value ? [value] : []}
        onOptionSelect={(_, data) => onChange(data.optionValue ?? "")}
      >
        {options.map((option) => (
          <Option key={option.value} value={option.value}>{option.label}</Option>
        ))}
      </Dropdown>
    </label>
  );
}

export function AreaField({
  label,
  value,
  onChange
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <label className="dp-field">
      <Label>{label}</Label>
      <Textarea value={value} onChange={(_, next) => onChange(next.value)} resize="vertical" />
    </label>
  );
}

export function Note({ error, ok, okText = "Saved." }: { error: string | null; ok: boolean; okText?: string }) {
  if (error) return <p className="note-err" role="alert">{error}</p>;
  if (ok) return <p className="note-ok">{okText}</p>;
  return null;
}
