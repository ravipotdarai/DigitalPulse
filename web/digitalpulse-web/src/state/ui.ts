import { create } from "zustand";

type UiState = {
  commandOpen: boolean;
  assistantOpen: boolean;
  noticeOpen: boolean;
  navOpen: boolean;
  setCommand: (open: boolean) => void;
  setAssistant: (open: boolean) => void;
  setNotice: (open: boolean) => void;
  setNav: (open: boolean) => void;
};

export const useUi = create<UiState>((set) => ({
  commandOpen: false,
  assistantOpen: false,
  noticeOpen: false,
  navOpen: false,
  setCommand: (commandOpen) => set({ commandOpen }),
  setAssistant: (assistantOpen) => set({ assistantOpen }),
  setNotice: (noticeOpen) => set({ noticeOpen }),
  setNav: (navOpen) => set({ navOpen })
}));
