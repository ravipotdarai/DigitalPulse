import { Dropdown, Input, Label, Option, Textarea } from "@fluentui/react-components";
import { StatusBanner } from "./motion";

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
        className="field-full"
        appearance="outline"
        size="large"
        name={name}
        type={type}
        required={required}
        disabled={disabled}
        value={value}
        minLength={type === "password" ? 8 : undefined}
        autoComplete={type === "password" ? "current-password" : type === "email" ? "email" : undefined}
        onChange={onChange ? (_, data) => onChange(data.value) : undefined}
      />
      {hint ? <small className="ink-muted">{hint}</small> : null}
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
    <div className="dp-field">
      <span>{label}</span>
      <Dropdown
        className="field-full"
        appearance="outline"
        placeholder="Choose"
        value={selected?.label}
        selectedOptions={value ? [value] : []}
        onOptionSelect={(_, data) => {
          if (data.optionValue) onChange(data.optionValue);
        }}
      >
        {options.map((option) => (
          <Option key={option.value} value={option.value} text={option.label}>
            {option.label}
          </Option>
        ))}
      </Dropdown>
    </div>
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
      <Textarea className="field-full" appearance="outline" resize="vertical" value={value} onChange={(_, next) => onChange(next.value)} />
    </label>
  );
}

export function Note({ error, ok, okText = "Saved." }: { error: string | null; ok: boolean; okText?: string }) {
  return <StatusBanner error={error} ok={ok} okText={okText} />;
}
