import { Spinner, Text } from "@fluentui/react-components";

export function PageState({
  title,
  detail,
  mode = "info"
}: {
  title: string;
  detail?: string;
  mode?: "loading" | "empty" | "error" | "info";
}) {
  return (
    <div className="flex min-h-[40vh] flex-col items-center justify-center gap-3 px-6 text-center">
      {mode === "loading" ? <Spinner size="large" label={title} /> : null}
      {mode !== "loading" ? <h2 className="display text-3xl">{title}</h2> : null}
      {detail ? <Text style={{ color: "var(--muted)" }}>{detail}</Text> : null}
    </div>
  );
}
