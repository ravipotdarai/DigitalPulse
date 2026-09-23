import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useUi } from "../state/ui";

const COMMANDS = [
  { id: "command", label: "Open command center", to: "/app" },
  { id: "identity", label: "Open business identity", to: "/app/identity" },
  { id: "workspace", label: "Update workspace info", to: "/app/info" },
  { id: "connections", label: "Connection center", to: "/app/connections" },
  { id: "findings", label: "Findings", to: "/app/findings" },
  { id: "website", label: "Website intelligence", to: "/app/website" },
  { id: "social", label: "Social content", to: "/app/social" }
];

export function CommandPalette() {
  const open = useUi((s) => s.commandOpen);
  const setCommand = useUi((s) => s.setCommand);
  const navigate = useNavigate();
  const [query, setQuery] = useState("");

  useEffect(() => {
    function onKey(event: KeyboardEvent) {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        setCommand(!open);
      }
      if (event.key === "Escape") setCommand(false);
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, setCommand]);

  const items = useMemo(() => {
    const needle = query.trim().toLowerCase();
    return COMMANDS.filter((item) => item.label.toLowerCase().includes(needle));
  }, [query]);

  if (!open) return null;

  return (
    <div className="veil" role="dialog" aria-label="Command" onClick={() => setCommand(false)}>
      <div className="command-box" onClick={(event) => event.stopPropagation()}>
        <input
          autoFocus
          placeholder="Jump to a surface"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
        />
        {items.map((item) => (
          <button
            key={item.id}
            type="button"
            onClick={() => {
              navigate(item.to);
              setCommand(false);
            }}
          >
            {item.label}
          </button>
        ))}
        {items.length === 0 ? <p className="dp-empty">No matches</p> : null}
      </div>
    </div>
  );
}
