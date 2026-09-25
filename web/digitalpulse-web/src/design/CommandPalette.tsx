import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useUi } from "../state/ui";
import { Overlay } from "./motion";

const COMMANDS = [
  { id: "command", label: "Overview", to: "/app" },
  { id: "workspace", label: "Workspace — tenant and plan", to: "/app/info" },
  { id: "billing", label: "Billing and subscription", to: "/app/billing" },
  { id: "identity", label: "Business profile", to: "/app/identity" },
  { id: "agency", label: "Company clients and white label", to: "/app/agency" },
  { id: "website", label: "Website and search", to: "/app/website" },
  { id: "monitoring", label: "Monitor", to: "/app/monitoring" },
  { id: "findings", label: "Scans — DigitalPulse Check", to: "/app/findings" },
  { id: "projects", label: "Projects", to: "/app/projects" },
  { id: "social", label: "Common post — all platforms", to: "/app/social" },
  { id: "google", label: "Google social studio", to: "/app/social/google" },
  { id: "facebook", label: "Facebook social studio", to: "/app/social/facebook" },
  { id: "instagram", label: "Instagram social studio", to: "/app/social/instagram" },
  { id: "linkedin", label: "LinkedIn social studio", to: "/app/social/linkedin" },
  { id: "youtube", label: "YouTube social studio", to: "/app/social/youtube" },
  { id: "whatsapp", label: "WhatsApp", to: "/app/whatsapp" },
  { id: "indiamart", label: "IndiaMART", to: "/app/directories?platform=INDIAMART" },
  { id: "justdial", label: "Justdial", to: "/app/directories?platform=JUSTDIAL" },
  { id: "connections", label: "Connection center", to: "/app/connections" },
  { id: "ai", label: "Orchestrator", to: "/app/ai" },
  { id: "actions", label: "Actions", to: "/app/actions" },
  { id: "operations", label: "Operations", to: "/app/operations" }
];

export function CommandPalette() {
  const open = useUi((s) => s.commandOpen);
  const setCommand = useUi((s) => s.setCommand);
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const [active, setActive] = useState(0);

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
    if (!open) {
      setQuery("");
      setActive(0);
    }
  }, [open]);

  const items = useMemo(() => {
    const needle = query.trim().toLowerCase();
    return COMMANDS.filter((item) => item.label.toLowerCase().includes(needle));
  }, [query]);

  function go(to: string) {
    navigate(to);
    setCommand(false);
  }

  return (
    <Overlay open={open} onClose={() => setCommand(false)} label="Jump">
      <div className="command-box">
        <input
          autoFocus
          className="command-input"
          placeholder="Jump to a surface"
          value={query}
          aria-label="Jump to a surface"
          onChange={(event) => {
            setQuery(event.target.value);
            setActive(0);
          }}
          onKeyDown={(event) => {
            if (event.key === "ArrowDown") {
              event.preventDefault();
              setActive((index) => Math.min(items.length - 1, index + 1));
            }
            if (event.key === "ArrowUp") {
              event.preventDefault();
              setActive((index) => Math.max(0, index - 1));
            }
            if (event.key === "Enter" && items[active]) {
              event.preventDefault();
              go(items[active].to);
            }
          }}
        />
        <div className="command-list" role="listbox" aria-label="Surfaces">
          {items.map((item, index) => (
            <button
              key={item.id}
              type="button"
              role="option"
              aria-selected={index === active}
              className={index === active ? "is-on" : undefined}
              onMouseEnter={() => setActive(index)}
              onClick={() => go(item.to)}
            >
              {item.label}
            </button>
          ))}
          {items.length === 0 ? <p className="command-empty">No matches</p> : null}
        </div>
      </div>
    </Overlay>
  );
}
