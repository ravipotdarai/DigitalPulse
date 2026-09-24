import { create } from "zustand";

const COLLAPSE_KEY = "dp.nav.collapsed";

type UiState = {
  commandOpen: boolean;
  assistantOpen: boolean;
  noticeOpen: boolean;
  navOpen: boolean;
  navCollapsed: boolean;
  setCommand: (open: boolean) => void;
  setAssistant: (open: boolean) => void;
  setNotice: (open: boolean) => void;
  setNav: (open: boolean) => void;
  setNavCollapsed: (collapsed: boolean) => void;
};

export const useUi = create<UiState>((set) => ({
  commandOpen: false,
  assistantOpen: false,
  noticeOpen: false,
  navOpen: false,
  navCollapsed: localStorage.getItem(COLLAPSE_KEY) === "1",
  setCommand: (commandOpen) => set({ commandOpen }),
  setAssistant: (assistantOpen) => set({ assistantOpen }),
  setNotice: (noticeOpen) => set({ noticeOpen }),
  setNav: (navOpen) => set({ navOpen }),
  setNavCollapsed: (navCollapsed) => {
    localStorage.setItem(COLLAPSE_KEY, navCollapsed ? "1" : "0");
    set({ navCollapsed });
  }
}));
