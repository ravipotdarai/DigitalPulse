import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useUi } from "../state/ui";
import { Overlay, Stagger, StaggerItem } from "./motion";

const COMMANDS = [
  { id: "command", label: "Overview — command center", to: "/app" },
  { id: "findings", label: "Signals — DigitalPulse Check", to: "/app/findings" },
  { id: "identity", label: "Business identity", to: "/app/identity" },
  { id: "connections", label: "Connections ecosystem", to: "/app/connections" },
  { id: "projects", label: "Projects and content factory", to: "/app/projects" },
  { id: "website", label: "Website and search", to: "/app/website" },
  { id: "social", label: "Social content", to: "/app/social" },
  { id: "directories", label: "Directories — IndiaMART / Justdial", to: "/app/directories" },
  { id: "workspace", label: "Workspace settings", to: "/app/info" }
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

  useEffect(() => {
    if (!open) setQuery("");
  }, [open]);

  const items = useMemo(() => {
    const needle = query.trim().toLowerCase();
    return COMMANDS.filter((item) => item.label.toLowerCase().includes(needle));
  }, [query]);

  return (
    <Overlay open={open} onClose={() => setCommand(false)} label="Command">
      <div className="command-box">
        <input
          autoFocus
          placeholder="Jump to a surface"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
        />
        <Stagger>
          {items.map((item) => (
            <StaggerItem key={item.id}>
              <button
                type="button"
                onClick={() => {
                  navigate(item.to);
                  setCommand(false);
                }}
              >
                {item.label}
              </button>
            </StaggerItem>
          ))}
        </Stagger>
        {items.length === 0 ? <p className="dp-empty">No matches</p> : null}
      </div>
    </Overlay>
  );
}
