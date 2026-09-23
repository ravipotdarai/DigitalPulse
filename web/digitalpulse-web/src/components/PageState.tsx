import { Spinner } from "@fluentui/react-components";

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
    <div className="state" role={mode === "error" ? "alert" : "status"}>
      {mode === "loading" ? <Spinner size="medium" label={title} /> : <h2 className="display">{title}</h2>}
      {detail ? <p>{detail}</p> : null}
    </div>
  );
}
