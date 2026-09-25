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

const LOCAL_PATHS: Record<string, string> = {
  LINKEDIN:
    "M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433c-1.144 0-2.063-.926-2.063-2.065 0-1.138.92-2.063 2.063-2.063 1.14 0 2.064.925 2.064 2.063 0 1.139-.925 2.065-2.064 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z"
};

export function brandColor(code: string) {
  const key = code.toUpperCase();
  const icon = ICONS[key];
  return icon ? `#${icon.hex}` : LETTERMARK_COLOR[key] ?? "var(--dp-accent)";
}

export function BrandMark({ code, name, className }: { code: string; name: string; className?: string }) {
  const key = code.toUpperCase();
  const icon = ICONS[key];
  const localPath = LOCAL_PATHS[key];
  const color = brandColor(key);
  return (
    <span className={className ? `brand-mark ${className}` : "brand-mark"} style={{ "--brand": color } as React.CSSProperties} aria-hidden="true">
      {icon || localPath ? (
        <svg viewBox="0 0 24 24" role="presentation">
          <path d={icon?.path ?? localPath} />
        </svg>
      ) : key === "WEBSITE" ? (
        <Globe20Regular />
      ) : (
        <b>{monogram(key, name)}</b>
      )}
    </span>
  );
}
