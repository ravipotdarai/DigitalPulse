/** Ambient product backdrop: drifting aurora light over a faint signal grid. Purely decorative. */
export function Backdrop() {
  return (
    <div className="backdrop" aria-hidden="true">
      <span className="backdrop-orb is-a" />
      <span className="backdrop-orb is-b" />
      <span className="backdrop-orb is-c" />
      <span className="backdrop-grid" />
    </div>
  );
}
