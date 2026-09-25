import { Globe20Regular } from "@fluentui/react-icons";
import {
  siFacebook,
  siGoogle,
  siGoogleads,
  siGooglesearchconsole,
  siInstagram,
  siMeta,
  siWhatsapp,
  siYoutube,
  type SimpleIcon
} from "simple-icons";
import { monogram } from "./platforms";

const ICONS: Record<string, SimpleIcon> = {
  GOOGLE: siGoogle,
  FACEBOOK: siFacebook,
  INSTAGRAM: siInstagram,
  META: siMeta,
  YOUTUBE: siYoutube,
  WHATSAPP: siWhatsapp,
  GOOGLEADS: siGoogleads,
  SEARCHCONSOLE: siGooglesearchconsole
};

/** Brands without a licensed mark in simple-icons render as a lettermark in their brand colour. */
const LETTERMARK_COLOR: Record<string, string> = {
  LINKEDIN: "#0A66C2",
  INDIAMART: "#E1261C",
  JUSTDIAL: "#FF8A00"
};

export function brandColor(code: string) {
  const key = code.toUpperCase();
  const icon = ICONS[key];
  return icon ? `#${icon.hex}` : LETTERMARK_COLOR[key] ?? "var(--dp-accent)";
}

export function BrandMark({ code, name, className }: { code: string; name: string; className?: string }) {
  const key = code.toUpperCase();
  const icon = ICONS[key];
  const color = brandColor(key);
  return (
    <span className={className ? `brand-mark ${className}` : "brand-mark"} style={{ "--brand": color } as React.CSSProperties} aria-hidden="true">
      {icon ? (
        <svg viewBox="0 0 24 24" role="presentation">
          <path d={icon.path} />
        </svg>
      ) : key === "WEBSITE" ? (
        <Globe20Regular />
      ) : (
        <b>{monogram(key, name)}</b>
      )}
    </span>
  );
}
