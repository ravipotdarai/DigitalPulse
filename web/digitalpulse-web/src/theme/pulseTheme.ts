import { BrandVariants, createDarkTheme, createLightTheme, Theme } from "@fluentui/react-components";

const pulse: BrandVariants = {
  10: "#140904",
  20: "#2A1408",
  30: "#3F1D0C",
  40: "#5A2910",
  50: "#7A3714",
  60: "#9C4818",
  70: "#C45C26",
  80: "#E46A2E",
  90: "#F07A3F",
  100: "#F3925E",
  110: "#F6AB7E",
  120: "#F8C3A0",
  130: "#FAD8C0",
  140: "#FCE8D8",
  150: "#FDF4EC",
  160: "#FFFAF7"
};

const type = {
  fontFamilyBase: '"Outfit", sans-serif',
  fontFamilyMonospace: '"DM Mono", ui-monospace, monospace',
  borderRadiusSmall: "4px",
  borderRadiusMedium: "6px",
  borderRadiusLarge: "8px",
  borderRadiusXLarge: "8px"
};

export const lightTheme: Theme = {
  ...createLightTheme(pulse),
  ...type,
  colorNeutralBackground1: "#f3efe6",
  colorNeutralForeground1: "#12141a",
  colorNeutralStroke1: "rgba(18, 20, 26, 0.12)",
  colorBrandBackground: "#c45c26",
  colorBrandBackgroundHover: "#a64818",
  colorBrandBackgroundPressed: "#7a3714"
};

export const darkTheme: Theme = {
  ...createDarkTheme(pulse),
  ...type,
  colorNeutralBackground1: "#0b0d12",
  colorNeutralForeground1: "#ece7de",
  colorNeutralStroke1: "rgba(236, 231, 222, 0.1)",
  colorBrandBackground: "#e46a2e",
  colorBrandBackgroundHover: "#f07a3f",
  colorBrandBackgroundPressed: "#c45c26"
};
