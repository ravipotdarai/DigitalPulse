import { Button as FluentButton, type ButtonProps } from "@fluentui/react-components";

/** DigitalPulse button. Primary is cyan on void so the label stays readable. */
export function Button({ appearance, className, ...props }: ButtonProps) {
  const tone = appearance === "primary" ? "btn-brand" : "btn-ghost";
  return <FluentButton appearance={appearance} className={className ? `${className} ${tone}` : tone} {...props} />;
}
