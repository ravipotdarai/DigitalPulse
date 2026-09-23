import { BrandVariants, createDarkTheme, createLightTheme, Theme } from "@fluentui/react-components";

const copper: BrandVariants = {
  10: "#1A0C07",
  20: "#36180C",
  30: "#52230F",
  40: "#6E2F12",
  50: "#8A3B14",
  60: "#A64818",
  70: "#C45C26",
  80: "#D36F3B",
  90: "#E07A3D",
  100: "#E8925E",
  110: "#EEA97E",
  120: "#F3BF9E",
  130: "#F7D4BE",
  140: "#FBE6D8",
  150: "#FDF3EC",
  160: "#FFFAF7"
};

export const lightTheme: Theme = {
  ...createLightTheme(copper),
  fontFamilyBase: '"Outfit", sans-serif',
  colorNeutralBackground1: "#f3eee4",
  colorNeutralForeground1: "#12151c"
};

export const darkTheme: Theme = {
  ...createDarkTheme(copper),
  fontFamilyBase: '"Outfit", sans-serif',
  colorNeutralBackground1: "#0d1016",
  colorNeutralForeground1: "#f4efe6"
};
