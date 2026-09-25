import { Input } from "@fluentui/react-components";
import { useMemo, useState, type ReactNode } from "react";

export function DataGrid({
  noun,
  empty,
  columns,
  rows,
  selectedId,
  onRow,
  maxHeight = "22rem"
}: {
  noun: string;
  empty: string;
  columns: string[];
  rows: { id: string; cells: ReactNode[]; search: string; actions?: ReactNode }[];
  selectedId?: string | null;
  onRow?: (id: string) => void;
  maxHeight?: string;
}) {
  const [query, setQuery] = useState("");
  const visible = useMemo(() => {
    const needle = query.trim().toLowerCase();
    if (!needle) return rows;
    return rows.filter((row) => row.search.includes(needle));
  }, [query, rows]);

  return (
    <div className="dp-grid" style={{ maxHeight }}>
      <div className="dp-grid-bar">
        <p>{rows.length} {noun}{rows.length === 1 ? "" : "s"}</p>
        <Input size="small" placeholder="Search" value={query} onChange={(_, next) => setQuery(next.value)} aria-label={`Filter ${noun}s`} />
      </div>
      {rows.length === 0 ? (
        <div className="dp-empty"><strong>No {noun}s</strong><p>{empty}</p></div>
      ) : visible.length === 0 ? (
        <div className="dp-empty"><strong>No matches</strong><p>Nothing matches “{query}”.</p></div>
      ) : (
        <div className="dp-grid-scroll">
          <table>
            <thead>
              <tr>
                {columns.map((column) => <th key={column}>{column}</th>)}
                <th className="actions"> </th>
              </tr>
            </thead>
            <tbody>
              {visible.map((row) => (
                <tr
                  key={row.id}
                  className={selectedId === row.id ? "is-on" : undefined}
                  tabIndex={onRow ? 0 : undefined}
                  onClick={onRow ? () => onRow(row.id) : undefined}
                  onKeyDown={onRow ? (event) => { if (event.key === "Enter") onRow(row.id); } : undefined}
                >
                  {row.cells.map((cell, index) => <td key={`${row.id}-${index}`}>{cell}</td>)}
                  <td className="actions">{row.actions}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

export function GridAction({ children, onClick, danger }: { children: ReactNode; onClick: () => void; danger?: boolean }) {
  return (
    <button type="button" className={danger ? "grid-action is-danger" : "grid-action"} onClick={(event) => { event.stopPropagation(); onClick(); }}>
      {children}
    </button>
  );
}
