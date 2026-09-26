import { BrandMark } from "./BrandMark";
import { Button } from "./Button";
import { Overlay } from "./motion";

export function PlatformLoginDialog({
  open,
  platform,
  busy,
  error,
  onClose,
  onContinue
}: {
  open: boolean;
  platform: { code: string; name: string } | null;
  busy: boolean;
  error: string | null;
  onClose: () => void;
  onContinue: () => void;
}) {
  if (!platform) return null;

  return (
    <Overlay open={open} onClose={onClose} label={`Sign in to ${platform.name}`}>
      <div className="platform-login">
        <BrandMark code={platform.code} name={platform.name} />
        <p className="hero-kicker">Official login</p>
        <h2>Sign in to {platform.name}</h2>
        <p>
          DigitalPulse opens the official {platform.name} login in a window. Approve access there.
          Tokens stay on this host. A password for {platform.name} is never collected here.
        </p>
        {error ? <p className="note-err" role="alert">{error}</p> : null}
        <div className="id-form-actions">
          <Button appearance="primary" disabled={busy} onClick={onContinue}>
            {busy ? "Opening login…" : `Continue to ${platform.name}`}
          </Button>
          <Button appearance="subtle" disabled={busy} onClick={onClose}>
            Cancel
          </Button>
        </div>
      </div>
    </Overlay>
  );
}
